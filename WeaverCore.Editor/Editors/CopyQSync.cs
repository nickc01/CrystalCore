using UnityEditor;
using UnityEngine;
using System.Diagnostics;
using System.Collections;
using System.IO;
using System.Collections.Concurrent;
using WeaverCore.Utilities;
using WeaverCore;
using System.Linq;
using System.Runtime.InteropServices;

[InitializeOnLoad]
public static class CopyQSync
{
    private static string lastClipboardContent = string.Empty;
    private static UnboundCoroutine clipboardSyncCoroutine;
    private static Process clipboardProcess;
    private static readonly ConcurrentQueue<string> clipboardQueue = new ConcurrentQueue<string>();

    const string ENDING_STR = "_-_END_-_";

    private static bool useWlPaste = false;
    private static bool useCopyQ = false;

    // PID file per Unity Editor instance
    private static readonly int editorPid = Process.GetCurrentProcess().Id;
    private static readonly string pidFilePath = Path.Combine(Path.GetTempPath(), $"copyqsync_{editorPid}.pid");

    /*static CopyQSync()
    {
        if (IsLinux())
        {
            KillOldProcessIfExists();

            if (IsWlPasteInstalled())
            {
                useWlPaste = true;
                StartClipboardProcessWithWatch();
                StartClipboardSync();
            }
            else if (IsCopyQInstalled())
            {
                useCopyQ = true;
                StartClipboardProcess("copyq clipboard");
                StartClipboardSync();
            }
            else
            {
                UnityEngine.Debug.LogWarning("Clipboard sync not started: Neither wl-paste nor copyq is installed.");
            }
        }
        else
        {
            UnityEngine.Debug.LogWarning("Clipboard sync not started: Not running on Linux.");
        }

        AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        EditorApplication.quitting += OnEditorQuitting;
    }*/

    private static void OnBeforeAssemblyReload()
    {
        StopClipboardSync();
    }

    private static void OnEditorQuitting()
    {
        StopClipboardSync();
    }

    private static void KillOldProcessIfExists()
    {
        if (File.Exists(pidFilePath))
        {
            try
            {
                string oldPidStr = File.ReadAllText(pidFilePath).Trim();
                if (int.TryParse(oldPidStr, out int oldPid))
                {
                    try
                    {
                        Process oldProcess = Process.GetProcessById(oldPid);
                        if (!oldProcess.HasExited)
                        {
                            oldProcess.Kill();
                            oldProcess.WaitForExit();
                        }
                    }
                    catch
                    {
                        // If process doesn't exist or can't be killed, just ignore
                    }
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogWarning("Error reading old PID file: " + ex.Message);
            }
            finally
            {
                File.Delete(pidFilePath);
            }
        }
    }

    /// <summary>
    /// Starts the wl-paste process using its --watch feature.
    /// </summary>
    private static void StartClipboardProcessWithWatch()
    {
        string command = "wl-paste --watch sh -c 'cat; echo " + ENDING_STR + "'";

        clipboardProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "bash",
                Arguments = "-c \"" + command + "\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        string clipboardCache = "";

        clipboardProcess.OutputDataReceived += (sender, e) =>
        {
            if (string.IsNullOrEmpty(e.Data))
                return;

            // Check if the line ends with ENDING_STR
            if (e.Data.EndsWith(ENDING_STR))
            {
                clipboardCache += e.Data.Substring(0, e.Data.Length - ENDING_STR.Length);
                clipboardQueue.Enqueue(clipboardCache);
                clipboardCache = "";
            }
            else
            {
                clipboardCache += e.Data + "\n";
            }
        };

        clipboardProcess.Start();
        clipboardProcess.BeginOutputReadLine();

        File.WriteAllText(pidFilePath, clipboardProcess.Id.ToString());
    }

    /// <summary>
    /// Starts a fallback clipboard polling process using copyq if wl-paste not available.
    /// </summary>
    private static void StartClipboardProcess(string command)
    {
        string bashArgs = $"-c \"while true; do {command}; echo '{ENDING_STR}'; sleep 0.5; done\"";

        clipboardProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "bash",
                Arguments = bashArgs,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        string clipboardCache = "";

        clipboardProcess.OutputDataReceived += (sender, e) =>
        {
            if (string.IsNullOrEmpty(e.Data))
                return;

            if (e.Data.EndsWith(ENDING_STR))
            {
                clipboardCache += e.Data.Substring(0, e.Data.Length - ENDING_STR.Length);
                clipboardQueue.Enqueue(clipboardCache);
                clipboardCache = "";
            }
            else
            {
                clipboardCache += e.Data + "\n";
            }
        };

        clipboardProcess.Start();
        clipboardProcess.BeginOutputReadLine();

        File.WriteAllText(pidFilePath, clipboardProcess.Id.ToString());
    }

    private static void StopClipboardProcess()
    {
        if (clipboardProcess != null && !clipboardProcess.HasExited)
        {
            try
            {
                clipboardProcess.Kill();
                clipboardProcess.WaitForExit();
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError("Failed to kill clipboard process: " + ex.Message);
            }
            clipboardProcess.Dispose();
            clipboardProcess = null;
        }

        if (File.Exists(pidFilePath))
        {
            File.Delete(pidFilePath);
        }
    }

    private static void StartClipboardSync()
    {
        clipboardSyncCoroutine = UnboundCoroutine.Start(ClipboardSyncCoroutine());
    }

    private static IEnumerator ClipboardSyncCoroutine()
    {
        while (true)
        {
            // Process all queued clipboard updates from system to Unity
            while (clipboardQueue.TryDequeue(out string clipboardContent))
            {
                if (!string.IsNullOrEmpty(clipboardContent) && clipboardContent != lastClipboardContent)
                {
                    lastClipboardContent = clipboardContent;
                    EditorGUIUtility.systemCopyBuffer = clipboardContent;
                    //UnityEngine.Debug.Log($"Clipboard updated from system: {clipboardContent}");
                }
            }

            // Now check if Unity's clipboard is different than the last known content
            // If it is, we need to push this change back to the system clipboard
            var currentUnityClipboard = EditorGUIUtility.systemCopyBuffer;
            if (currentUnityClipboard != lastClipboardContent)
            {
                lastClipboardContent = currentUnityClipboard;
                // Push update to system clipboard
                UpdateSystemClipboard(currentUnityClipboard);
            }

            yield return new WaitForSecondsRealtime(0.1f);
        }
    }

    private static void UpdateSystemClipboard(string content)
    {
        try
        {
            if (useWlPaste)
            {
                // Use wl-copy
                using (Process wlCopyProcess = new Process())
                {
                    wlCopyProcess.StartInfo.FileName = "bash";
                    // Quote the content to handle special chars
                    wlCopyProcess.StartInfo.Arguments = $"-c \"echo -n {EscapeForShell(content)} | wl-copy\"";
                    wlCopyProcess.StartInfo.UseShellExecute = false;
                    wlCopyProcess.StartInfo.RedirectStandardOutput = true;
                    wlCopyProcess.StartInfo.RedirectStandardError = true;
                    wlCopyProcess.StartInfo.CreateNoWindow = true;
                    wlCopyProcess.Start();
                    wlCopyProcess.WaitForExit();
                }
            }
            else if (useCopyQ)
            {
                // Use copyq
                using (Process copyqProcess = new Process())
                {
                    copyqProcess.StartInfo.FileName = "bash";
                    copyqProcess.StartInfo.Arguments = $"-c \"echo -n {EscapeForShell(content)} | copyq copy -\"";
                    copyqProcess.StartInfo.UseShellExecute = false;
                    copyqProcess.StartInfo.RedirectStandardOutput = true;
                    copyqProcess.StartInfo.RedirectStandardError = true;
                    copyqProcess.StartInfo.CreateNoWindow = true;
                    copyqProcess.Start();
                    copyqProcess.WaitForExit();
                }
            }
            else
            {
                // No known clipboard tool available; can't sync back
                UnityEngine.Debug.LogWarning("No system clipboard tool (wl-copy/copyq) available to sync back.");
            }
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError("Failed to update system clipboard: " + ex.Message);
        }
    }

    private static string EscapeForShell(string input)
    {
        // A simple shell escape by wrapping in single quotes and replacing existing single quotes.
        if (input == null)
            return "";
        return "'" + input.Replace("'", "'\"'\"'") + "'";
    }

    private static bool IsLinux()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    }

    private static bool IsWlPasteInstalled()
    {
        return IsCommandAvailable("wl-paste") && IsCommandAvailable("wl-copy");
    }

    private static bool IsCopyQInstalled()
    {
        return IsCommandAvailable("copyq");
    }

    private static bool IsCommandAvailable(string commandName)
    {
        try
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "which";
                process.StartInfo.Arguments = commandName;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                string result = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return !string.IsNullOrEmpty(result.Trim());
            }
        }
        catch
        {
            return false;
        }
    }

    [MenuItem("Tools/Stop Clipboard Sync")]
    private static void StopClipboardSync()
    {
        if (clipboardSyncCoroutine != null)
        {
            clipboardSyncCoroutine.Stop();
            clipboardSyncCoroutine = null;
        }

        StopClipboardProcess();
        //UnityEngine.Debug.Log("Clipboard sync stopped.");
    }
}
