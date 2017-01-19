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
		readonly List<CrossSectionRecord> crossSectionRecords;
		readonly List<MaterialRecord> materialRecords;
		readonly List<BoundaryConditionRecord> boundaryConditionRecords;
		readonly List<TimeFunctionRecord> timeFunctionRecords;
		readonly List<SetRecord> setRecords;

		readonly Dictionary<int, DofManagerRecord> dofManagerRecords;
		readonly Dictionary<int, ElementRecord> elementRecords;

		int maxDofManagerId, maxElementId, maxCrossSectionId, maxMaterialId, maxBoundaryConditionId, maxTimeFunctionId, maxSetId;

		public InputBuilder()
		{
			headerRecords = new List<HeaderRecord>();
			crossSectionRecords = new List<CrossSectionRecord>();
			materialRecords = new List<MaterialRecord>();
			boundaryConditionRecords = new List<BoundaryConditionRecord>();
			timeFunctionRecords = new List<TimeFunctionRecord>();
			setRecords = new List<SetRecord>();

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
				foreach (var record in crossSectionRecords)
					streamWriter.WriteLine(record.ToString());

				streamWriter.WriteLine($"# MATERIALS");
				foreach (var record in materialRecords)
					streamWriter.WriteLine(record.ToString());

				streamWriter.WriteLine($"# BOUNDARY CONDITIONS");
				foreach (var record in boundaryConditionRecords)
					streamWriter.WriteLine(record.ToString());

				streamWriter.WriteLine($"# TIME FUNCTIONS");
				foreach (var record in timeFunctionRecords)
					streamWriter.WriteLine(record.ToString());

				streamWriter.WriteLine($"# SETS");
				foreach (var record in setRecords)
					streamWriter.WriteLine(record.ToString());
			}
		}

		public void AddHeaderRecord(HeaderRecord record)
		{
			headerRecords.Add(record);
		}

		public void AddOrUpdateDofManagerRecord(DofManagerRecord record)
		{
			dofManagerRecords[record.Id] = record;
			maxDofManagerId = Math.Max(maxDofManagerId, record.Id);
		}

		public void AddOrUpdateElementRecord(ElementRecord record)
		{
			elementRecords[record.Id] = record;
			maxElementId = Math.Max(maxElementId, record.Id);
		}

		public void AddCrossSectionRecord(CrossSectionRecord record)
		{
			crossSectionRecords.Add(record);
			maxCrossSectionId = Math.Max(maxCrossSectionId, record.Id);
		}

		public void AddMaterialRecord(MaterialRecord record)
		{
			materialRecords.Add(record);
			maxMaterialId = Math.Max(maxMaterialId, record.Id);
		}

		public void AddBoundaryConditionRecord(BoundaryConditionRecord record)
		{
			boundaryConditionRecords.Add(record);
			maxBoundaryConditionId = Math.Max(maxBoundaryConditionId, record.Id);
		}

		public void AddTimeFunctionRecord(TimeFunctionRecord record)
		{
			timeFunctionRecords.Add(record);
			maxTimeFunctionId = Math.Max(maxTimeFunctionId, record.Id);
		}

		public void AddSetRecord(SetRecord record)
		{
			setRecords.Add(record);
			maxSetId = Math.Max(maxSetId, record.Id);
		}

		public IReadOnlyDictionary<int, DofManagerRecord> DofManagerRecords => dofManagerRecords;

		public IReadOnlyDictionary<int, ElementRecord> ElementRecords => elementRecords;

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
