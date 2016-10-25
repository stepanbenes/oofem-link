using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Data.MeshEntities;
using OofemLink.Data.ModelEntities;

namespace OofemLink.Business.Import
{
	public interface IModelImportService
	{
		Model ImportModel();
	}

	public interface IMeshImportService
    {
		Mesh ImportMesh();
    }

	public interface IImportService : IModelImportService, IMeshImportService
	{ }
}
