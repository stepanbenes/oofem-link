using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using OofemLink.Business.Dto;
using OofemLink.Business.Import;
using OofemLink.Data;

namespace OofemLink.Business.Services
{
	public class ProjectService : DataService<Project>, IService<ProjectDto, int>
	{
		public ProjectService(DataContext context)
			: base(context)
		{ }

		public void ImportSimulation(string projectNameOrId, IImportService importService)
		{
			var simulation = importService.ImportSimulation();
			int projectId;
			if (int.TryParse(projectNameOrId, out projectId)) // find existing project or create new
			{
				simulation.ProjectId = projectId;
			}
			else
			{
				Project project = Entities.Where(p => p.Name == projectNameOrId).SingleOrDefault();
				if (project == null)
				{
					project = new Project { Name = projectNameOrId };
					Entities.Add(project);
					simulation.Project = project;
				}
				else
				{
					simulation.ProjectId = project.Id;
				}
			}
			Context.Simulations.Add(simulation);
			Context.SaveChanges();
		}

		public async Task<IReadOnlyList<ProjectDto>> GetAllAsync(Func<IQueryable<ProjectDto>, IQueryable<ProjectDto>> filter = null)
		{
			return await GetQuery(filter).ToListAsync();
		}

		public async Task<ProjectDto> GetOneAsync(int primaryKey)
		{
			return await Entities.Where(p => p.Id == primaryKey).ProjectTo<ProjectDto>().SingleOrDefaultAsync();
		}

		public async Task CreateAsync(ProjectDto dto)
		{
			Entities.Add(Mapper.Map<Project>(dto));
			await Context.SaveChangesAsync();
		}

		public async Task UpdateAsync(int primaryKey, ProjectDto dto)
		{
			var entityToUpdate = Mapper.Map<Project>(dto);
			entityToUpdate.Id = primaryKey;
			Entities.Update(entityToUpdate);
			await Context.SaveChangesAsync();
		}

		public async Task DeleteAsync(int primaryKey)
		{
			var entityToDelete = new Project { Id = primaryKey };
			Entities.Remove(entityToDelete);
			await Context.SaveChangesAsync();
		}
	}
}
