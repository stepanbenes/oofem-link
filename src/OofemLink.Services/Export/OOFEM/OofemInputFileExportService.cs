using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OofemLink.Common.Enumerations;
using OofemLink.Data;
using OofemLink.Data.Entities;

namespace OofemLink.Services.Export.OOFEM
{
	class OofemInputFileExportService : IExportService
	{
		#region Fields, constructor

		readonly string fileFullPath;
		readonly DataContext dataContext;

		public OofemInputFileExportService(DataContext dataContext, string fileFullPath)
		{
			this.dataContext = dataContext;
			if (!string.IsNullOrEmpty(fileFullPath))
				this.fileFullPath = fileFullPath;
			else
				this.fileFullPath = "oofem.in";
		}

		#endregion

		#region Public methods

		public void ExportSimulation(int simulationId)
		{
			using (var input = new InputBuilder(fileFullPath))
			{
				createOofemInput(input, simulationId);
			}
		}

		#endregion

		#region Private methods

		private void createOofemInput(InputBuilder input, int simulationId)
		{
			var simulation = dataContext.Simulations
								.Include(s => s.Project)
								.Include(s => s.TimeSteps)
								.Include(s => s.Model)
								.ThenInclude(m => m.Meshes)
								.FirstOrDefault(s => s.Id == simulationId);

			if (simulation == null)
				throw new KeyNotFoundException($"Simulation with id {simulationId} was not found.");
			if (simulation.Model == null)
				throw new InvalidDataException($"Simulation {simulationId} does not contain any model.");

			var model = simulation.Model;

			if (model.Meshes.Count == 0)
				throw new InvalidDataException($"No mesh found for model {model.Id}.");
			if (model.Meshes.Count > 1)
				throw new NotSupportedException($"Multiple meshes for single model are not yet supported (model {model.Id}).");

			var mesh = model.Meshes.Single();

			var nodesQuery = from node in dataContext.Nodes
							 where node.MeshId == mesh.Id
							 select node;
			var elementsQuery = from element in dataContext.Elements.Include(e => e.ElementNodes)
								where element.MeshId == mesh.Id
								select element;
			var crossSectionsQuery = from attribute in dataContext.Attributes.Include(a => a.ChildAttributes)
									 where attribute.ModelId == model.Id
									 where attribute.Type == AttributeType.CrossSection
									 select attribute;
			var materialsQuery = from attribute in dataContext.Attributes
								 where attribute.ModelId == model.Id
								 where attribute.Type == AttributeType.Material
								 select attribute;
			// =========================================================================================

			// Output file name
			input.AddPlainText((simulation.Project?.Name ?? "output") + ".out"); // TODO: make valid filename
			// Description
			input.AddPlainText($"Project: {simulation.Project?.Name}, Task: {simulation.TaskName}");

			// Type of so-called engineering model, willbe the same for now, for non-linear problems we will need switch to nonlinear static. The nlstatic can have several keywords specifying solver parameters, convergence criteria and so on, nmodules = number of export modules
			input.AddEngineeringModel(
					engineeringModelName: "LinearStatic", // TODO: take this from analysis parameters in Simulation object
					numberOfTimeSteps: simulation.TimeSteps.Count,
					numberOfExportModules: 1 /**/
				);

			// the export module is vtk
			// TODO: this is hard-coded now, enable this to be configurable
			input.AddPlainText("vtkxml tstep_all domain_all primvars 1 1");

			// domain specify degrees of freedom, but it is not used anymore and will be removed in near future, it remains here just for backward compatibility
			// TODO: avoid hard-coded string
			input.AddDomain("3dshell");

			// default outputmanager giving outfile, in this case beam3d.out, only specific elements or time steps can be exported, here we export all of them
			// TODO: avoid hard-coded string
			input.AddPlainText("OutputManager tstep_all dofman_all element_all");

			// number of dofmanagers(generalization of nodes), number of elements, n of corssections, n of materials, boundary conditions, initial conditions, load time functions, and sets
			input.AddRecordCounts(
					dofManagerCount: nodesQuery.Count(),
					elementCount: elementsQuery.Count(),
					crossSectionCount: crossSectionsQuery.Count(),
					materialCount: materialsQuery.Count()
					// TODO: complete record counts
				);

			addDebugComment(input, "NODES");
			
			foreach (var node in nodesQuery)
			{
				input.AddNode(node.Id).WithCoordinates(node.X, node.Y, node.Z);
			}

			addDebugComment(input, "ELEMENTS");
			
			foreach (var element in elementsQuery)
			{
				var nodeIds = (from elementNode in element.ElementNodes
							   orderby elementNode.Rank
							   select elementNode.NodeId).ToArray();

				switch (element.Type)
				{
					case CellType.LineLinear:
						Debug.Assert(nodeIds.Length == 2);
						input.AddElement("beam3d", element.Id).HavingNodes(nodeIds);
						break;
					case CellType.TriangleLinear:
						Debug.Assert(nodeIds.Length == 3);
						input.AddElement("mitc4shell", element.Id).HavingNodes(nodeIds[0], nodeIds[1], nodeIds[2], nodeIds[2]); // last node is doubled
						break;
					case CellType.QuadLinear:
						Debug.Assert(nodeIds.Length == 4);
						input.AddElement("mitc4shell", element.Id).HavingNodes(nodeIds);
						break;
					default:
						throw new NotSupportedException($"Element type {element.Type} is not supported.");
				}
			}

			var sets = new List<Set>();

			addDebugComment(input, "CROSS-SECTIONS");
			var crossSections = crossSectionsQuery.ToArray();
			foreach (var crossSection in crossSections)
			{
				input.AddCrossSection(crossSection.Name, crossSection.LocalNumber)
					 .WithParameters(crossSection.Parameters)
					 .HasMaterial(materialId: crossSection.ChildAttributes.Single().ChildAttribute.LocalNumber) // TODO: handle cases with non-single referenced materials
					 .AppliesToSet(getOrCreateSetForAttribute(crossSection, sets).Id);
			}

			addDebugComment(input, "MATERIALS");
			foreach (var material in materialsQuery)
			{
				input.AddMaterial(material.Name, material.LocalNumber).WithParameters(material.Parameters);
			}

			addDebugComment(input, "SETS");
			foreach (var set in sets)
			{
				input.AddSet(set.Id).ContainingNodes(set.Nodes).ContainingElements(set.Elements);
			}
		}

		private Set getOrCreateSetForAttribute(ModelAttribute attribute, List<Set> sets)
		{
			var q = from a in dataContext.Attributes
					where a.ModelId == attribute.ModelId
					where a.Id == attribute.Id
					from curveAttribute in a.CurveAttributes
					from curveElement in curveAttribute.Curve.CurveElements
					orderby curveElement.ElementId
					select curveElement.ElementId;
			var set = new Set(id: sets.Count + 1).WithElements(q.ToArray());
			sets.Add(set); // TODO: try to look in cache if it contains the same set
			return set;
		}

		[Conditional("DEBUG")]
		private static void addDebugComment(InputBuilder input, string comment)
		{
			input.AddComment(comment);
		}

		#endregion
	}
}
