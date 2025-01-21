using System;
using System.IO;
using System.Reflection;
using System.Text;
using WeaverCore.Attributes;

namespace WeaverCore.Editor.Patches
{
    static class VisualStudioCodeEditorPatches
	{
		[OnHarmonyPatch]
		static void OnHarmonyPatch(HarmonyPatcher patcher)
		{
			try
			{
				foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
				{
					if (assembly.GetName().Name == "Unity.VisualStudio.Editor")
					{
						var installType = assembly.GetType("Microsoft.Unity.VisualStudio.Editor.VisualStudioCodeInstallation");
						
						if (installType != null)
						{
							/*{
								//TryDiscoverInstallation
								var orig = installType.GetMethod("TryDiscoverInstallation", BindingFlags.Public | BindingFlags.Static);
								var postfix = typeof(TestPatches).GetMethod(nameof(TryDiscoverInstallationPostfix), BindingFlags.NonPublic | BindingFlags.Static);
								patcher.Patch(orig, null, postfix);
							}*/

							{
								//ProcessStartInfoFor
								var orig = installType.GetMethod("ProcessStartInfoFor", BindingFlags.NonPublic | BindingFlags.Static);
								var prefix = typeof(VisualStudioCodeEditorPatches).GetMethod(nameof(ProcessStartInfoForPrefix), BindingFlags.NonPublic | BindingFlags.Static);
								patcher.Patch(orig, prefix, null);
							}
						}

						break;
					}
				}
			}
			catch (Exception e)
			{
				WeaverLog.Log("Failed to patch vscode custom stuff. Continuing anyway " + e);
			}
		}

		static bool IsWaylandSessionRunning()
		{
			try
			{
				var env = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");
				return !string.IsNullOrEmpty(env) && env.Contains("wayland");
			}
			catch (Exception e)
			{
				WeaverLog.Log("ENV Check Failed. Continuing anyway " + e);
				return false;
			}
		}

		static bool ProcessStartInfoForPrefix(object __instance, ref string application, ref string arguments)
		{
			try
			{
				if (IsWaylandSessionRunning() && arguments != null && !arguments.StartsWith("--ozone-platform-hint"))
				{
					arguments = "--ozone-platform-hint=wayland --enable-wayland-ime --use-gl=egl " + arguments;
				}
				return true;
			}
			catch (Exception e)
			{
				WeaverLog.Log("Custom process start for vscode failed. Continuing anyway " + e);
				return true;
			}
		}

		static void TryDiscoverInstallationPostfix(object __instance, ref object installation)
		{
			StringBuilder output = new StringBuilder("Installation = ");

			foreach (var field in installation.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public))
			{
				output.AppendLine($" ---- {field.Name} = {field.GetValue(installation)}");
			}

			foreach (var field in installation.GetType().BaseType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public))
			{
				output.AppendLine($" ---- {field.Name} = {field.GetValue(installation)}");
			}

			WeaverLog.Log(output.ToString());
			//WeaverLog.Log("INSTALLATION = " + installation.ToString());
		}

	}
}
