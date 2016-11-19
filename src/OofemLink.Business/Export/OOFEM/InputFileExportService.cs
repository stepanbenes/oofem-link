using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OofemLink.Common.Enumerations;
using OofemLink.Data;

namespace OofemLink.Business.Export.OOFEM
{
	class InputFileExportService : IExportService
	{
		#region Fields, constructor

		readonly string fileFullPath;
		readonly DataContext dataContext;

		public InputFileExportService(DataContext dataContext, string fileFullPath)
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
								.Include(s => s.Models)
								.ThenInclude(m => m.Meshes)
								.FirstOrDefault(s => s.Id == simulationId);

			if (simulation == null)
				throw new KeyNotFoundException($"Simulation with id {simulationId} was not found.");
			if (simulation.Models.Count == 0)
				throw new InvalidDataException($"Simulation {simulationId} does not contain any model.");
			if (simulation.Models.Count > 1)
				throw new InvalidDataException($"Simulation {simulationId} contains more then one model.");

			var model = simulation.Models.Single();

			if (model.Meshes.Count == 0)
				throw new InvalidDataException($"No mesh found for model {model.Id}.");
			if (model.Meshes.Count > 1)
				throw new NotSupportedException($"Multiple meshes for single model are not yet supported (model {model.Id}).");

			var mesh = model.Meshes.Single();

			// first line: Output file name
			input.AddPlainString("oofem.out");
			// second line: Description
			input.AddPlainString($"Project: {simulation.Project?.Name}, Task: {simulation.TaskName}");

			// TODO: complete header

			var nodesQuery = from node in dataContext.Nodes
							 where node.MeshId == mesh.Id
							 select node;

			foreach (var node in nodesQuery)
			{
				input.AddNode(node.Id).WithCoordinates(node.X, node.Y, node.Z);
			}

			var elementQuery = from element in dataContext.Elements.Include(e => e.ElementNodes)
							   where element.MeshId == mesh.Id
							   select element;

			foreach (var element in elementQuery)
			{
				string elementName;
				switch (element.Type)
				{
					case CellType.LineLinear:
						elementName = "beam3d";
						break;
					case CellType.TriangleLinear:
						elementName = "triangle";
						break;
					case CellType.QuadLinear:
						elementName = "quad";
						break;
					default:
						throw new NotSupportedException($"Element type {element.Type} is not supported.");
				}

				input.AddElement(elementName, element.Id).HavingNodes((from elementNode in element.ElementNodes
																	   orderby elementNode.Rank
																	   select elementNode.NodeId).ToArray());
			}
		}

		#endregion
	}
}
