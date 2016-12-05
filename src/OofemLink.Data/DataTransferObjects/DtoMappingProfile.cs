using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using OofemLink.Data.Entities;

namespace OofemLink.Data.DataTransferObjects
{
    public class DtoMappingProfile : Profile
    {
		public DtoMappingProfile()
		{
			// ENTITY -> DTO
			CreateMap<Project, ProjectDto>();
			CreateMap<Simulation, ViewSimulationDto>()
				.ForMember(s => s.ProjectName, options => options.MapFrom(s => s.Project.Name));
			CreateMap<Vertex, VertexDto>();
			CreateMap<Curve, CurveDto>()
				.ForMember(c => c.VertexIds, options => options.MapFrom(c => c.CurveVertices.OrderBy(v => v.Rank).Select(v => v.VertexId)));
			// DTO -> ENTITY
			CreateMap<ProjectDto, Project>();
			CreateMap<EditSimulationDto, Simulation>();
			CreateMap<VertexDto, Vertex>();
		}
    }
}
