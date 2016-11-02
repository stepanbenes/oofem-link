using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using OofemLink.Data;

namespace OofemLink.Business.Dto
{
    public class DtoMappingProfile : Profile
    {
		public DtoMappingProfile()
		{
			// ENTITY -> DTO
			CreateMap<Project, ProjectDto>();
			CreateMap<Simulation, ViewSimulationDto>()
				.ForMember(s => s.ProjectName, options => options.MapFrom(s => s.Project.Name));
			// DTO -> ENTITY
			CreateMap<ProjectDto, Project>();
			CreateMap<EditSimulationDto, Simulation>();
		}
    }
}
