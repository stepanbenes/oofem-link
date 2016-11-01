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
				var model = importService.ImportModel();
				var mesh = importService.ImportMesh();

				simulation.ProjectId = projectId;
				context.Simulations.Add(simulation);
				context.SaveChanges(); // simulation id is retrieved
				
				simulation.Model = model;
				model.Simulation = simulation;
				
				model.Meshes.Add(mesh);

				context.SaveChanges();
			}
		}
	}
}
