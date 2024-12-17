using UnityEditor;
using UnityEngine;
using System.Diagnostics;
using System.Collections;
using System.IO;
using System.Collections.Concurrent;
using WeaverCore.Utilities;
using WeaverCore;

[InitializeOnLoad]
public static class CopyQSync
{
    private static string lastClipboardContent = string.Empty;
    private static UnboundCoroutine clipboardSyncCoroutine;
    private static Process copyqProcess;
    private static readonly ConcurrentQueue<string> clipboardQueue = new ConcurrentQueue<string>();

    const string ENDING_STR = "_-_END_-_";

    static CopyQSync()
    {

        if (IsLinux() && IsCopyQInstalled())
        {
            AssemblyReloadEvents.beforeAssemblyReload += OnReload;
            StartCopyQProcess();
            StartClipboardSync();

        }
        else
        {
            UnityEngine.Debug.LogWarning("Clipboard sync not started: Either not on Linux or CopyQ is not installed.");
        }
    }

    static void OnReload()
    {
        StopClipboardSync();
    }

    private static void StartCopyQProcess()
    {
        copyqProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "bash",
                Arguments = $"-c \"while true; do copyq clipboard; echo '{ENDING_STR}'; sleep 0.5; done\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        int processID = 0;

        string clipboardCache = "";

        copyqProcess.OutputDataReceived += (sender, e) =>
        {
            if (e.Data.EndsWith(ENDING_STR))
            {
                clipboardCache += e.Data.Substring(0, e.Data.Length - ENDING_STR.Length);

                //WeaverLog.Log($"{processID} OUTPUT DATA = " + e.Data);
                clipboardQueue.Enqueue(clipboardCache);
                clipboardCache = "";

                /*if (!string.IsNullOrEmpty(e.Data))
                {
                    //WeaverLog.Log($"{processID} CLIPBOARD UPDATE = " + e.Data);
                    clipboardQueue.Enqueue(e.Data);
                }*/
            }
            else
            {
                clipboardCache += e.Data + "\n";
            }
        };

        //WeaverLog.Log("STARTING");
        copyqProcess.Start();
        copyqProcess.BeginOutputReadLine();

        processID = copyqProcess.Id;
    }

    private static void StopCopyQProcess()
    {
        if (copyqProcess != null && !copyqProcess.HasExited)
        {
            copyqProcess.Kill();
            copyqProcess.Dispose();
            copyqProcess = null;
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
            // Process all queued clipboard updates
            while (clipboardQueue.TryDequeue(out string clipboardContent))
            {
                if (!string.IsNullOrEmpty(clipboardContent) && clipboardContent != lastClipboardContent)
                {
                    //EditorGUIUtility.systemCopyBuffer = clipboardContent;
                    lastClipboardContent = clipboardContent;
                    //UnityEngine.Debug.Log($"Clipboard updated: {clipboardContent}");
                }

            }

            // Wait for 500 milliseconds before checking again
            yield return new WaitForSecondsRealtime(0.25f);
        }
    }

    private static bool IsLinux()
    {
        return System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux);
    }

    private static bool IsCopyQInstalled()
    {
        try
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "which";
                process.StartInfo.Arguments = "copyq";
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

        StopCopyQProcess();
        UnityEngine.Debug.Log("Clipboard sync stopped.");
    }
}
