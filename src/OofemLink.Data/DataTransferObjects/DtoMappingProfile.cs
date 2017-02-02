using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using OofemLink.Data.DbEntities;

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
				.ForMember(c => c.VertexIds, options => options.MapFrom(c => c.CurveVertices.OrderBy(v => v.Rank).Select(v => v.VertexId).ToList()));
			CreateMap<Mesh, MeshDto>();
			CreateMap<Node, NodeDto>();
			CreateMap<Element, ElementDto>()
				.ForMember(e => e.NodeIds, options => options.MapFrom(e => e.ElementNodes.OrderBy(en => en.Rank).Select(en => en.NodeId).ToList()));
			CreateMap<TimeStep, TimeStepDto>();
			CreateMap<ModelAttribute, AttributeDto>()
				.ForMember(a => a.ChildAttributeIds, options => options.MapFrom(a => a.ChildAttributes.Select(ca => ca.ChildAttributeId).ToList()));
			// DTO -> ENTITY
			CreateMap<ProjectDto, Project>();
			CreateMap<EditSimulationDto, Simulation>();
			CreateMap<VertexDto, Vertex>();
		}
    }
}
