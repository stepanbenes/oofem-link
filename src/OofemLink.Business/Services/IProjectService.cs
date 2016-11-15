using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Business.Dto;
using OofemLink.Business.Import;

namespace OofemLink.Business.Services
{
    public interface IProjectService : IQuery<ProjectDto, int>, ICommand<ProjectDto, int>
    {
		void ImportSimulation(IImportService importService);
	}
}
