using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using OofemLink.Business.Dto;
using OofemLink.Business.Import;
using OofemLink.Common.Encoding;
using OofemLink.Data;

namespace OofemLink.Business.Services
{
	public class ProjectService : DataService, IProjectService
	{
		public ProjectService(DataContext context)
			: base(context)
		{ }

		public void ImportSimulation(IImportService importService)
		{
			var simulation = importService.ImportSimulation();
			if (simulation.Project == null || string.IsNullOrEmpty(simulation.Project.Name))
			{
				string projectName = new ZBaseEncoder().Encode(Guid.NewGuid().ToByteArray()); // generate unique project name
				simulation.Project = new Project { Name = projectName };
			}
			
			Project existingProject = Context.Projects.Where(p => p.Name == simulation.Project.Name).SingleOrDefault();
			if (existingProject != null)
			{
				simulation.Project = existingProject;
			}
			else
			{
				Context.Projects.Add(simulation.Project);
			}

			Context.Simulations.Add(simulation);
			Context.SaveChanges();
		}

		public async Task<IReadOnlyList<ProjectDto>> GetAllAsync(Func<IQueryable<ProjectDto>, IQueryable<ProjectDto>> filter = null)
		{
			return await GetQuery<Project, ProjectDto>(filter).ToListAsync();
		}

		public async Task<ProjectDto> GetOneAsync(int primaryKey)
		{
			return await Context.Projects.Where(p => p.Id == primaryKey).ProjectTo<ProjectDto>().SingleOrDefaultAsync();
		}

		public async Task CreateAsync(ProjectDto dto)
		{
			Context.Projects.Add(Mapper.Map<Project>(dto));
			await Context.SaveChangesAsync();
		}

		public async Task UpdateAsync(int primaryKey, ProjectDto dto)
		{
			var entityToUpdate = Mapper.Map<Project>(dto);
			entityToUpdate.Id = primaryKey;
			Context.Projects.Update(entityToUpdate);
			await Context.SaveChangesAsync();
		}

		public async Task DeleteAsync(int primaryKey)
		{
			var entityToDelete = new Project { Id = primaryKey };
			Context.Projects.Remove(entityToDelete);
			await Context.SaveChangesAsync();
		}
	}
}
