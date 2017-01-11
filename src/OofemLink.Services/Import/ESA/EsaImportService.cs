using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OofemLink.Common.Enumerations;
using OofemLink.Common.OofemNames;
using OofemLink.Data.Entities;

namespace OofemLink.Services.Import.ESA
{
	class EsaImportService : IImportService
	{
		#region Fields, constructor

		readonly string location, taskName;
		readonly ILoggerFactory loggerFactory;
		readonly ILogger logger;
		readonly bool importModelOnly;

		public EsaImportService(string location, string taskName, ILoggerFactory loggerFactory, bool importModelOnly = false)
		{
			this.location = location;
			this.taskName = taskName;
			this.importModelOnly = importModelOnly;
			this.loggerFactory = loggerFactory;
			this.logger = loggerFactory.CreateLogger<EsaImportService>();
		}

		#endregion

		#region Public methods

		public Simulation ImportSimulation()
		{
			logger.LogInformation("Starting import...");

			var simulation = new ProFileParser(location, taskName, loggerFactory).Parse(loadCasesToIgnore: new[] { 9998, 9999 });

			ModelDimensions dimensions;
			var model = new GeoFileParser(location, taskName, loggerFactory).Parse(out dimensions);
			var mesh = importMesh(dimensions);
			model.Meshes.Add(mesh);

			linkModelAndMeshTogether(model, mesh);

			if (!importModelOnly)
			{
				importAttributesToModel(model, simulation.TimeSteps); // import model attributes
			}

			simulation.DimensionFlags = dimensions;
			simulation.Model = model;

			simulation.State = SimulationState.ModelReady;

			logger.LogInformation("Import finished.");
			return simulation;
		}

		#endregion

		#region Private methods

		private Mesh importMesh(ModelDimensions dimensions)
		{
			Mesh mesh = new Mesh();
			// NODES
			foreach (var node in new XyzFileParser(location, taskName, loggerFactory).Parse(dimensions))
			{
				mesh.Nodes.Add(node);
			}
			// 1D ELEMENTS
			foreach (var element in new E1dFileParser(location, taskName, loggerFactory).Parse(startElementId: 1))
			{
				mesh.Elements.Add(element);
			}
			// 2D ELEMENTS
			foreach (var element in new E2dFileParser(location, taskName, loggerFactory).Parse(startElementId: mesh.Elements.Count + 1))
			{
				mesh.Elements.Add(element);
			}
			return mesh;
		}

		/// <summary>
		/// link model and mesh entities together (using e.g. file MTO and LIN)
		/// </summary>
		private void linkModelAndMeshTogether(Model model, Mesh mesh)
		{
			var modelMeshMapper = new ModelMeshMapper(model, mesh);

			string mtoFileFullPath = Path.Combine(location, $"{taskName}.MTO");

			// parse MTO file (Macro - Elements links)
			var mtoFileParser = new MtoFileParser(location, taskName, loggerFactory);
			mtoFileParser.Parse(modelMeshMapper);

			// parse LIN file (Curve - 2D Elements links)
			var linFileParser = new LinFileParser(location, taskName, loggerFactory);
			linFileParser.Parse(modelMeshMapper);

			// add Vertex-Node mappings
			foreach (var vertex in model.Vertices)
			{
				modelMeshMapper.MapVertexToNode(vertex.Id);
			}
		}

		private void importAttributesToModel(Model model, IEnumerable<TimeStep> timeSteps)
		{
			var attributeMapper = new AttributeMapper(model);
			var coordinateTransformService = new CoordinateTransformService(model);

			int timeFunctionId = 1;
			var constantTimeFunction = new ConstantFunction
			{
				Id = timeFunctionId++,
				ConstantValue = 1
			};
			model.TimeFunctions.Add(constantTimeFunction);

			// IST file parsing
			var istParser = new IstFileParser(attributeMapper, coordinateTransformService, location, taskName, loggerFactory);
			var cs_mat_bc_attributes = istParser.Parse().ToList();
			foreach (var attribute in cs_mat_bc_attributes)
			{
				assignTimeFunctionToAttributeAndItsChildren(attribute, constantTimeFunction);
			}
			int attributesTotal = addAttributesToModel(cs_mat_bc_attributes, model, startAttributeId: 1);

			// Ixxxx files parsing
			foreach (var timeStep in timeSteps)
			{
				var timeFunctionForCurrentStep = new PiecewiseLinFunction
				{
					Id = timeFunctionId++,
					Values = { new TimeFunctionValue { TimeStep = timeStep, Value = 1.0 } }
				};
				model.TimeFunctions.Add(timeFunctionForCurrentStep);
				int loadCaseNumber = timeStep.Number;
				var ixxxxFileParser = new IxxxxFileParser(loadCaseNumber, attributeMapper, location, taskName, loggerFactory);
				var lc_attributes = ixxxxFileParser.Parse().ToList();
				foreach (var loadCase in lc_attributes)
				{
					assignTimeFunctionToAttributeAndItsChildren(loadCase, timeFunctionForCurrentStep);
				}
				attributesTotal += addAttributesToModel(lc_attributes, model, startAttributeId: attributesTotal + 1);
			}
		}

		private int addAttributesToModel(IEnumerable<ModelAttribute> attributes, Model model, int startAttributeId)
		{
			int id = startAttributeId;
			foreach (var attribute in attributes)
			{
				attribute.Id = id;
				model.Attributes.Add(attribute);
				id += 1;
				int addedCount = addAttributesToModel(attribute.ChildAttributes.Select(c => c.ChildAttribute), model, id);
				id += addedCount;
			}
			return id - startAttributeId; // returns count of attributes that were added
		}

		private void assignTimeFunctionToAttributeAndItsChildren(ModelAttribute attribute, TimeFunction timeFunction)
		{
			attribute.TimeFunction = timeFunction;
			foreach (var attributeComposition in attribute.ChildAttributes)
			{
				assignTimeFunctionToAttributeAndItsChildren(attributeComposition.ChildAttribute, timeFunction);
			}
		}

		#endregion
	}
}
