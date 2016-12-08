﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Common.Enumerations
{
    public enum SimulationState : byte
    {
		New = 0,
		ModelReady,
		ReadyToRun,
		Finished,
		ReadyToPostprocess
	}
}
