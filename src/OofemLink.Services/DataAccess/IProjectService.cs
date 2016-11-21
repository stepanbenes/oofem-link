using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Data.DataTransferObjects;
using OofemLink.Services.Import;

namespace OofemLink.Services.DataAccess
{
    public interface IProjectService : IQuery<ProjectDto, int>, ICommand<ProjectDto, int>
    {
		void ImportSimulation(IImportService importService);
	}
}
