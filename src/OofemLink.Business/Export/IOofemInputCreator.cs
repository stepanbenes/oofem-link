using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Business.Export
{
    interface IOofemInputCreator<TService>
    {
		TService AddInputRecord(OofemInputRecord inputRecord);
	}
}
