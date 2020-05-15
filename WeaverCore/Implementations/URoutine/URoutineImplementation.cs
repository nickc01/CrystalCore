﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WeaverCore.Helpers;

namespace WeaverCore.Implementations
{
	public abstract class URoutineImplementation : IImplementation
	{
		public abstract URoutineData Start(IEnumerator<IUAwaiter> function);

		public abstract void Stop(URoutineData routine);

		public abstract float DT { get; }

		public abstract class URoutineData
		{

		}
	}
}
