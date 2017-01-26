﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Globalization;
using System.Diagnostics;

namespace OofemLink.Services.Export.OOFEM
{
	class InputBuilder
	{
		#region Fields, constructor

		readonly List<HeaderRecord> headerRecords;

		readonly Dictionary<int, BoundaryConditionRecord> boundaryConditionRecords;
		readonly Dictionary<int, TimeFunctionRecord> timeFunctionRecords;
		
		readonly Dictionary<int, DofManagerRecord> dofManagerRecords;
		readonly Dictionary<int, ElementRecord> elementRecords;

		readonly Dictionary<int, CrossSectionRecord> crossSectionRecords;
		readonly Dictionary<int, MaterialRecord> materialRecords;
		readonly Dictionary<int, SetRecord> setRecords;

		int maxDofManagerId, maxElementId, maxCrossSectionId, maxMaterialId, maxBoundaryConditionId, maxTimeFunctionId, maxSetId;

		public InputBuilder()
		{
			headerRecords = new List<HeaderRecord>();
			crossSectionRecords = new Dictionary<int, CrossSectionRecord>();
			materialRecords = new Dictionary<int, MaterialRecord>();
			boundaryConditionRecords = new Dictionary<int, BoundaryConditionRecord>();
			timeFunctionRecords = new Dictionary<int, TimeFunctionRecord>();
			setRecords = new Dictionary<int, SetRecord>();

			dofManagerRecords = new Dictionary<int, DofManagerRecord>();
			elementRecords = new Dictionary<int, ElementRecord>();
		}

		public int MaxDofManagerId => maxDofManagerId;
		public int MaxElementId => maxElementId;
		public int MaxCrossSectionId => maxCrossSectionId;
		public int MaxMaterialId => maxMaterialId;
		public int MaxBoundaryConditionId => maxBoundaryConditionId;
		public int MaxTimeFunctionId => maxTimeFunctionId;
		public int MaxSetId => maxSetId;

		#endregion

		#region Public methods

		public void WriteToFile(string fileFullPath)
		{
			using (var stream = new FileStream(fileFullPath, FileMode.Create, FileAccess.Write, FileShare.None))
			using (var streamWriter = new StreamWriter(stream))
			{
				// Write header comment
				streamWriter.WriteLine($"# OOFEM input file. Generated by {getProgramDescription()} on machine {Environment.MachineName}");
				foreach (var record in headerRecords)
					streamWriter.WriteLine(record.ToString());

				// calculate and print numbers of records
				streamWriter.WriteLine(buildRecordCountsString());

				streamWriter.WriteLine($"# DOF-MANAGERS");
				foreach (var record in dofManagerRecords.Values.OrderBy(r => r.Id))
					streamWriter.WriteLine(record.ToString());

				streamWriter.WriteLine($"# ELEMENTS");
				foreach (var record in elementRecords.Values.OrderBy(r => r.Id))
					streamWriter.WriteLine(record.ToString());

				streamWriter.WriteLine($"# CROSS-SECTIONS");
				foreach (var record in crossSectionRecords.Values.OrderBy(r => r.Id))
					streamWriter.WriteLine(record.ToString());

				streamWriter.WriteLine($"# MATERIALS");
				foreach (var record in materialRecords.Values.OrderBy(r => r.Id))
					streamWriter.WriteLine(record.ToString());

				streamWriter.WriteLine($"# BOUNDARY CONDITIONS");
				foreach (var record in boundaryConditionRecords.Values.OrderBy(r => r.Id))
					streamWriter.WriteLine(record.ToString());

				streamWriter.WriteLine($"# TIME FUNCTIONS");
				foreach (var record in timeFunctionRecords.Values.OrderBy(r => r.Id))
					streamWriter.WriteLine(record.ToString());

				streamWriter.WriteLine($"# SETS");
				foreach (var record in setRecords.Values.OrderBy(r => r.Id))
					streamWriter.WriteLine(record.ToString());
			}
		}

		public void AddHeaderRecord(HeaderRecord record)
		{
			headerRecords.Add(record);
		}

		public void AddDofManagerRecord(DofManagerRecord record)
		{
			dofManagerRecords.Add(record.Id, record);
			maxDofManagerId = Math.Max(maxDofManagerId, record.Id);
		}

		public void UpdateDofManagerRecord(DofManagerRecord record)
		{
			if (!dofManagerRecords.ContainsKey(record.Id))
				throw new KeyNotFoundException($"DofManager record with id {record.Id} was not found");
			dofManagerRecords[record.Id] = record;
			maxDofManagerId = Math.Max(maxDofManagerId, record.Id);
		}

		public void AddElementRecord(ElementRecord record)
		{
			elementRecords[record.Id] = record;
			maxElementId = Math.Max(maxElementId, record.Id);
		}

		public void UpdateElementRecord(ElementRecord record)
		{
			if (!elementRecords.ContainsKey(record.Id))
				throw new KeyNotFoundException($"Element record with id {record.Id} was not found");
			elementRecords.Add(record.Id, record);
			maxElementId = Math.Max(maxElementId, record.Id);
		}

		public void AddCrossSectionRecord(CrossSectionRecord record)
		{
			crossSectionRecords.Add(record.Id, record);
			maxCrossSectionId = Math.Max(maxCrossSectionId, record.Id);
		}

		public void UpdateCrossSectionRecord(CrossSectionRecord record)
		{
			if (!crossSectionRecords.ContainsKey(record.Id))
				throw new KeyNotFoundException($"Cross-section record with id {record.Id} was not found");
			crossSectionRecords[record.Id] = record;
			maxCrossSectionId = Math.Max(maxCrossSectionId, record.Id);
		}

		public void AddMaterialRecord(MaterialRecord record)
		{
			materialRecords.Add(record.Id, record);
			maxMaterialId = Math.Max(maxMaterialId, record.Id);
		}

		public void UpdateMaterialRecord(MaterialRecord record)
		{
			if (!materialRecords.ContainsKey(record.Id))
				throw new KeyNotFoundException($"Material record with id {record.Id} was not found");
			materialRecords[record.Id] = record;
			maxMaterialId = Math.Max(maxMaterialId, record.Id);
		}

		public void AddBoundaryConditionRecord(BoundaryConditionRecord record)
		{
			boundaryConditionRecords.Add(record.Id, record);
			maxBoundaryConditionId = Math.Max(maxBoundaryConditionId, record.Id);
		}

		public void UpdateBoundaryConditionRecord(BoundaryConditionRecord record)
		{
			if (!boundaryConditionRecords.ContainsKey(record.Id))
				throw new KeyNotFoundException($"Boundary condition record with id {record.Id} was not found");
			boundaryConditionRecords[record.Id] = record;
			maxBoundaryConditionId = Math.Max(maxBoundaryConditionId, record.Id);
		}

		public void AddTimeFunctionRecord(TimeFunctionRecord record)
		{
			timeFunctionRecords.Add(record.Id, record);
			maxTimeFunctionId = Math.Max(maxTimeFunctionId, record.Id);
		}

		public void UpdateTimeFunctionRecord(TimeFunctionRecord record)
		{
			if (!timeFunctionRecords.ContainsKey(record.Id))
				throw new KeyNotFoundException($"Time function record with id {record.Id} was not found");
			timeFunctionRecords[record.Id] = record;
			maxTimeFunctionId = Math.Max(maxTimeFunctionId, record.Id);
		}

		public void AddSetRecord(SetRecord record)
		{
			setRecords.Add(record.Id, record);
			maxSetId = Math.Max(maxSetId, record.Id);
		}

		public void UpdateSetRecord(SetRecord record)
		{
			if (!setRecords.ContainsKey(record.Id))
				throw new KeyNotFoundException($"Set record with id {record.Id} was not found");
			setRecords[record.Id] = record;
			maxSetId = Math.Max(maxSetId, record.Id);
		}

		public IReadOnlyDictionary<int, DofManagerRecord> DofManagerRecords => dofManagerRecords;
		public IReadOnlyDictionary<int, ElementRecord> ElementRecords => elementRecords;
		public IReadOnlyDictionary<int, CrossSectionRecord> CrossSectionRecords => crossSectionRecords;
		public IReadOnlyDictionary<int, MaterialRecord> MaterialRecords => materialRecords;
		public IReadOnlyDictionary<int, SetRecord> SetRecords => setRecords;

		public IReadOnlyDictionary<int, BoundaryConditionRecord> BoundaryConditionRecords => boundaryConditionRecords;
		public IReadOnlyDictionary<int, TimeFunctionRecord> TimeFunctionRecords => timeFunctionRecords;

		#endregion

		#region Private methods

		private static string getProgramDescription()
		{
			var assembly = Assembly.GetEntryAssembly();
			var assemblyTitleAttribute = assembly.GetCustomAttribute<AssemblyTitleAttribute>();
			var assemblyFileVersionAttribute = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
			return $"{assemblyTitleAttribute.Title} version {assemblyFileVersionAttribute.Version}";
		}

		private string buildRecordCountsString()
		{
			int dofManagerCount = dofManagerRecords.Count;
			int elementCount = elementRecords.Count;
			int crossSectionCount = crossSectionRecords.Count;
			int materialCount = materialRecords.Count;
			int boundaryConditionCount = boundaryConditionRecords.Count;
			int initialConditionCount = 0; /*no initial conditions for time-independent analysis (statics)*/
			int timeFunctionCount = timeFunctionRecords.Count;
			int setCount = setRecords.Count;

			return $"{Keyword.ndofman} {dofManagerCount} {Keyword.nelem} {elementCount} {Keyword.ncrosssect} {crossSectionCount} {Keyword.nmat} {materialCount} {Keyword.nbc} {boundaryConditionCount} {Keyword.nic} {initialConditionCount} {Keyword.nltf} {timeFunctionCount} {Keyword.nset} {setCount}";
		}

		#endregion
	}
}
