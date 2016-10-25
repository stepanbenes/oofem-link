using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Business.Import;
using OofemLink.Data;

namespace OofemLink.Business
{
    public class ModelBuilder
    {
		int? modelId;

		public ModelBuilder(/*ILogger logger*/)
		{

		}

		public void ImportModel(IModelImportService importService)
		{
			Debug.Assert(!modelId.HasValue);
			var model = importService.ImportModel();
			using (var context = new DataContext())
			{
				context.Models.Add(model);
				context.SaveChanges();
				this.modelId = model.Id;
			}
		}

		public void ImportMesh(IMeshImportService importService)
		{
			Debug.Assert(modelId.HasValue);
			var mesh = importService.ImportMesh();
			mesh.ModelId = this.modelId.Value;
			using (var context = new DataContext())
			{
				context.Meshes.Add(mesh);
				context.SaveChanges();
			}
		}

		//public void AddMacro(Macro newMacro) { }
	}
}
