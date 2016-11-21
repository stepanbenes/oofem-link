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
				.ForMember(s => s.ProjectName, options => options.MapFrom(s => s.Project.Name))
				.ForMember(s => s.ModelId, options => options.MapFrom(s => s.Models.Select(m => (int?)m.Id).SingleOrDefault()));
			CreateMap<Vertex, VertexDto>();
			CreateMap<Curve, CurveDto>()
				.ForMember(c => c.VertexIds, options => options.MapFrom(c => c.Vertices.OrderBy(v => v.Rank).Select(v => v.VertexId)));
			// DTO -> ENTITY
			CreateMap<ProjectDto, Project>();
			CreateMap<EditSimulationDto, Simulation>();
			CreateMap<VertexDto, Vertex>();
		}
    }
}
