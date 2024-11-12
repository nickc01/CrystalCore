
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using WeaverCore.Settings;

namespace WeaverCore.Game
{
	public static class SettingStorage
	{
		public static DirectoryInfo SettingsFolder { get; private set; }


		static SettingStorage()
		{
			var weaverCoreFile = new FileInfo(typeof(Initialization).Assembly.Location);
			SettingsFolder = new DirectoryInfo(weaverCoreFile.Directory.FullName + $"{Path.DirectorySeparatorChar}WeaverCore{Path.DirectorySeparatorChar}Settings");

			SettingsFolder.Create();
		}

		public static void Load<T>(T settings) where T : GlobalSettings
		{
			Load(typeof(T), settings);
		}

		public static void Load(Type type, GlobalSettings settings)
		{
			if (!typeof(GlobalSettings).IsAssignableFrom(type))
			{
				throw new Exception("The type " + type.FullName + " does not inherit from WeaverSettings");
			}
			var file = new FileInfo(SettingsFolder.FullName + Path.DirectorySeparatorChar + type.FullName + ".cfg");

			if (file.Exists)
			{
				try
				{
					JsonUtility.FromJsonOverwrite(File.ReadAllText(file.FullName), settings);
				}
				catch (Exception)
				{
					
				}
			}
		}

		public static void Save(GlobalSettings settings)
		{
			var type = settings.GetType();
			var file = new FileInfo(SettingsFolder.FullName + Path.DirectorySeparatorChar + type.FullName + ".cfg");
			File.WriteAllText(file.FullName, JsonUtility.ToJson(settings));
		}

	}
}
