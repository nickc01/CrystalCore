﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WeaverCore.Helpers;

namespace WeaverCore.GameStatus
{
	public static class GameStatus
	{
		public static RunningState GameState => ImplFinder.State;
	}
}
