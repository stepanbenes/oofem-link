using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Business.Import;
using OofemLink.Data;

namespace OofemLink.Business
{
    public class ProjectManager
    {
		public static ProjectManager GetOrCreateNew(string projectName)
		{
			using (var db = new DataContext())
			{
				var project = db.Projects.SingleOrDefault(p => p.Name == projectName);
				if (project == null)
				{
					// create and save new project to db
					project = new Project { Name = projectName };
					db.Projects.Add(project);
					db.SaveChanges();
				}
				return new ProjectManager(project.Id);
			}
		}

		readonly int projectId;

		public ProjectManager(int projectId)
		{
			this.projectId = projectId;
		}

		public void ImportSimulation(IImportService importService)
		{
			using (var context = new DataContext())
			{
				var simulation = importService.ImportSimulation();
				simulation.ProjectId = projectId;
				context.Simulations.Add(simulation);
				context.SaveChanges();

				var model = importService.ImportModel();
				model.Id = simulation.Id;
				simulation.Model = model;
				context.Models.Add(model);
				context.SaveChanges();

				var mesh = importService.ImportMesh();
				mesh.ModelId = model.Id;
				context.Meshes.Add(mesh);
				context.SaveChanges();
			}
		}
	}
}
