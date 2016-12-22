﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using static System.FormattableString;
using System.Globalization;
using System.Diagnostics;

namespace OofemLink.Services.Export.OOFEM
{
	class InputBuilder : IDisposable,
		INodeRecordBuilder,
		IElementRecordBuilder,
		ICrossSectionBuilder,
		IMaterialBuilder,
		IBoundaryConditionBuilder,
		ITimeFunctionBuilder,
		ISetBuilder
	{
		#region Fields, constructor

		readonly StreamWriter streamWriter;

		public InputBuilder(string fileFullPath)
		{
			var stream = new FileStream(fileFullPath, FileMode.Create, FileAccess.Write, FileShare.None);
			streamWriter = new StreamWriter(stream);
			// Write header comment
			streamWriter.Write($"# OOFEM input file. Generated by {getProgramDescription()} on machine {Environment.MachineName}");
		}

		#endregion

		#region Public methods

		public void AddComment(string comment)
		{
			streamWriter.WriteLine();
			streamWriter.Write("# " + comment);
		}

		public void AddPlainText(string text)
		{
			streamWriter.WriteLine();
			streamWriter.Write(text);
		}

		public void AddEngineeringModel(string engineeringModelName, int numberOfTimeSteps, int numberOfExportModules)
		{
			streamWriter.WriteLine();
			streamWriter.Write($"{engineeringModelName} {Keyword.nsteps} {numberOfTimeSteps} {Keyword.nmodules} {numberOfExportModules}");
		}

		public void AddDomain(string domainType)
		{
			streamWriter.WriteLine();
			streamWriter.Write($"{Keyword.domain} {domainType}");
		}

		public void AddRecordCounts(int dofManagerCount, int elementCount, int crossSectionCount = 0, int materialCount = 0, int boundaryConditionCount = 0, int initialConditionCount = 0, int timeFunctionCount = 0, int setCount = 0)
		{
			streamWriter.WriteLine();
			streamWriter.Write($"{Keyword.ndofman} {dofManagerCount} {Keyword.nelem} {elementCount} {Keyword.ncrosssect} {crossSectionCount} {Keyword.nmat} {materialCount} {Keyword.nbc} {boundaryConditionCount} {Keyword.nic} {initialConditionCount} {Keyword.nltf} {timeFunctionCount} {Keyword.nset} {setCount}");
		}

		public INodeRecordBuilder AddNode(int id)
		{
			streamWriter.WriteLine();
			streamWriter.Write($"{Keyword.node} {id}");
			return this;
		}

		public IElementRecordBuilder AddElement(string type, int id)
		{
			streamWriter.WriteLine();
			streamWriter.Write($"{type} {id}");
			return this;
		}

		public ICrossSectionBuilder AddCrossSection(string name, int id)
		{
			streamWriter.WriteLine();
			streamWriter.Write($"{name} {id}");
			return this;
		}

		public IMaterialBuilder AddMaterial(string name, int id)
		{
			streamWriter.WriteLine();
			streamWriter.Write($"{name} {id}");
			return this;
		}

		public IBoundaryConditionBuilder AddBoundaryCondition(string name, int id)
		{
			streamWriter.WriteLine();
			streamWriter.Write($"{name} {id}");
			return this;
		}

		public ITimeFunctionBuilder AddTimeFunction(string name, int id)
		{
			streamWriter.WriteLine();
			streamWriter.Write($"{name} {id}");
			return this;
		}

		public ISetBuilder AddSet(int id)
		{
			streamWriter.WriteLine();
			streamWriter.Write($"{Keyword.set} {id}");
			return this;
		}

		public void Dispose()
		{
			if (streamWriter != null)
			{
				streamWriter.WriteLine(); // add line break at the end
				streamWriter.Dispose();
			}
		}

		#endregion

		#region Fluent API methods

		INodeRecordBuilder INodeRecordBuilder.WithCoordinates(double x, double y, double z)
		{
			streamWriter.Write(Invariant($" {Keyword.coords} 3 {x} {y} {z}"));
			return this;
		}

		IElementRecordBuilder IElementRecordBuilder.WithNodes(params int[] nodeIds)
		{
			streamWriter.Write($" {Keyword.nodes} {nodeIds.Length} {string.Join(" ", nodeIds)}");
			return this;
		}

		IElementRecordBuilder IElementRecordBuilder.WithZAxis(double[] zAxisDirection)
		{
			Debug.Assert(zAxisDirection.Length == 3);
			streamWriter.Write($" {Keyword.zaxis} {zAxisDirection.Length} {string.Join(" ", zAxisDirection.Select(x => x.ToString(CultureInfo.InvariantCulture)))}");
			return this;
		}

		ICrossSectionBuilder ICrossSectionBuilder.WithParameters(string parameters)
		{
			streamWriter.Write(" " + parameters);
			return this;
		}

		ICrossSectionBuilder ICrossSectionBuilder.HasMaterial(int materialId)
		{
			streamWriter.Write($" {Keyword.material} {materialId}");
			return this;
		}

		ICrossSectionBuilder ICrossSectionBuilder.AppliesToSet(int setId)
		{
			streamWriter.Write($" {Keyword.set} {setId}");
			return this;
		}

		void IMaterialBuilder.WithParameters(string parameters)
		{
			streamWriter.Write(" " + parameters);
		}

		IBoundaryConditionBuilder IBoundaryConditionBuilder.InTime(int timeFunctionId)
		{
			streamWriter.Write($" {Keyword.loadTimeFunction} {timeFunctionId}");
			return this;
		}

		IBoundaryConditionBuilder IBoundaryConditionBuilder.WithParameters(string parameters)
		{
			streamWriter.Write(" " + parameters);
			return this;
		}

		IBoundaryConditionBuilder IBoundaryConditionBuilder.AppliesToSet(int setId)
		{
			streamWriter.Write($" {Keyword.set} {setId}");
			return this;
		}

		ITimeFunctionBuilder ITimeFunctionBuilder.InTime(double time)
		{
			streamWriter.Write(Invariant($" t {time}"));
			return this;
		}

		ITimeFunctionBuilder ITimeFunctionBuilder.WithValue(double value)
		{
			streamWriter.Write(Invariant($" f(t) {value}"));
			return this;
		}

		ITimeFunctionBuilder ITimeFunctionBuilder.WithTimeValuePairs(IReadOnlyList<KeyValuePair<double, double>> timeValuePairs)
		{
			string times = string.Join(" ", timeValuePairs.Select(pair => pair.Key.ToString(CultureInfo.InvariantCulture)));
			string values = string.Join(" ", timeValuePairs.Select(pair => pair.Value.ToString(CultureInfo.InvariantCulture)));
			streamWriter.Write($" {Keyword.nPoints} {timeValuePairs.Count} t {timeValuePairs.Count} {times} f(t) {timeValuePairs.Count} {values}");
			return this;
		}

		ISetBuilder ISetBuilder.WithNodes(IReadOnlyList<int> nodeIds)
		{
			if (nodeIds.Count > 0)
			{
				streamWriter.Write($" {Keyword.nodes} {nodeIds.Count} {string.Join(" ", nodeIds)}");
			}
			return this;
		}

		ISetBuilder ISetBuilder.WithElements(IReadOnlyList<int> elementIds)
		{
			if (elementIds.Count > 0)
			{
				streamWriter.Write($" {Keyword.elements} {elementIds.Count} {string.Join(" ", elementIds)}");
			}
			return this;
		}

		ISetBuilder ISetBuilder.WithElementEdges(IReadOnlyList<KeyValuePair<int, short>> elementEdgeIdPairs)
		{
			if (elementEdgeIdPairs.Count > 0)
			{
				streamWriter.Write($" {Keyword.elementedges} {elementEdgeIdPairs.Count * 2} {string.Join(" ", elementEdgeIdPairs.Select(pair => $"{pair.Key} {pair.Value}"))}");
			}
			return this;
		}

		ISetBuilder ISetBuilder.WithElementSurfaces(IReadOnlyList<KeyValuePair<int, short>> elementSurfaceIdPairs)
		{
			if (elementSurfaceIdPairs.Count > 0)
			{
				streamWriter.Write($" {Keyword.elementboundaries} {elementSurfaceIdPairs.Count * 2} {string.Join(" ", elementSurfaceIdPairs.Select(pair => $"{pair.Key} {pair.Value}"))}");
			}
			return this;
		}

		#endregion

		#region Private methods

		private static string getProgramDescription()
		{
			var assembly = Assembly.GetEntryAssembly();
			var assemblyTitleAttribute = assembly.GetCustomAttribute<AssemblyTitleAttribute>();
			var assemblyFileVersionAttribute = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
			return $"{assemblyTitleAttribute.Title} version {assemblyFileVersionAttribute.Version}";
		}

		#endregion
	}

	interface INodeRecordBuilder
	{
		INodeRecordBuilder WithCoordinates(double x, double y, double z);
	}

	interface IElementRecordBuilder
	{
		IElementRecordBuilder WithNodes(params int[] nodeIds);
		IElementRecordBuilder WithZAxis(double[] zAxisDirection);
	}

	interface ICrossSectionBuilder
	{
		ICrossSectionBuilder WithParameters(string parameters);
		ICrossSectionBuilder HasMaterial(int materialId);
		ICrossSectionBuilder AppliesToSet(int setId);
	}

	interface IMaterialBuilder
	{
		void WithParameters(string parameters);
	}

	interface IBoundaryConditionBuilder
	{
		IBoundaryConditionBuilder InTime(int timeFunctionId);
		IBoundaryConditionBuilder WithParameters(string parameters);
		IBoundaryConditionBuilder AppliesToSet(int setId);
	}

	interface ITimeFunctionBuilder
	{
		ITimeFunctionBuilder InTime(double time);
		ITimeFunctionBuilder WithValue(double value);
		ITimeFunctionBuilder WithTimeValuePairs(IReadOnlyList<KeyValuePair<double, double>> timeValuePairs);
	}

	interface ISetBuilder
	{
		ISetBuilder WithNodes(IReadOnlyList<int> nodeIds);
		ISetBuilder WithElements(IReadOnlyList<int> elementIds);
		ISetBuilder WithElementEdges(IReadOnlyList<KeyValuePair<int, short>> elementEdgeIdPairs);
		ISetBuilder WithElementSurfaces(IReadOnlyList<KeyValuePair<int, short>> elementSurfaceIdPairs);
	}
}
