using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Services.Execution
{
    public class ExecutionOptions
    {
		public string OofemExecutableFilePath { get; set; }
		public string DefaultInputLocation { get; set; }
		public string DefaultOutputLocation { get; set; }
	}
}
