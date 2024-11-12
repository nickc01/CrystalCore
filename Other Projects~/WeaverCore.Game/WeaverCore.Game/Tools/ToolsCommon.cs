using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeaverCore.Utilities;
using UnityEngine;

namespace WeaverCore.Game.Tools
{
	public static class ToolsCommon
	{
		/// <summary>
		/// The location where all the sprites and animations should be dumped to
		/// </summary>
		public static DirectoryInfo DumpLocation
		{
			get
			{
				var location = new FileInfo(typeof(Initialization).Assembly.Location).Directory.CreateSubdirectory("WeaverCore").CreateSubdirectory("Dumps");
				UnityEngine.Debug.Log("Dump Location = " + location.FullName);
				return location;
			}
		}
	}
}
