using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Data.DbEntities;

namespace OofemLink.Services.Import
{
	//public interface IModelImportService
	//{
	//	Model ImportModel();
	//}

	//public interface IMeshImportService
	//{
	//	Mesh ImportMesh();
	//}

	public interface IImportService
	{
		Simulation ImportSimulation();
	}
}
