﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Common.Enumerations
{
    public enum SimulationState : byte
    {
		New = 0,
		ModelReady = 1,
		Finished = 2,
		ReadyToPostprocess = 3
	}
}
