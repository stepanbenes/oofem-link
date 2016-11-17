﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using OofemLink.Data;
using OofemLink.Data.Entities;
using System.Reflection;
using static System.FormattableString;

namespace OofemLink.Business.Export.OOFEM
{
	class InputBuilder : IDisposable, INodeRecordBuilder, IElementRecordBuilder
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

		public void AddPlainString(string text)
		{
			streamWriter.WriteLine();
			streamWriter.Write(text);
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

		public void Dispose()
		{
			streamWriter.Dispose();
		}

		#endregion

		#region Fluent API methods

		INodeRecordBuilder INodeRecordBuilder.WithCoordinates(double x, double y, double z)
		{
			streamWriter.Write(Invariant($" {Keyword.coords} 3 {x} {y} {z}"));
			return this;
		}

		IElementRecordBuilder IElementRecordBuilder.HavingNodes(params int[] nodeIds)
		{
			streamWriter.Write($" {Keyword.nodes} {nodeIds.Length} {string.Join(" ", nodeIds)}");
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
		IElementRecordBuilder HavingNodes(params int[] nodeIds);
	}
}
