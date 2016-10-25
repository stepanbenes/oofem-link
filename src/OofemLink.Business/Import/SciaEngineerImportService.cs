using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OofemLink.Data.MeshEntities;
using OofemLink.Data.ModelEntities;

namespace OofemLink.Business.Import
{
	class SciaEngineerImportService : IImportService
	{
		readonly string location;

		public SciaEngineerImportService(string location)
		{
			this.location = location;
		}

		public Model ImportModel()
		{
			Model model = new Model();
			return model;
		}

		public Mesh ImportMesh()
		{
			//string taskName = simulation.TaskName;

			string xyzFilename = Directory.EnumerateFiles(location, "*.xyz").Single(); // Path.Combine(location, $"{taskName}.xyz");
			int dimension = 3;

			long xyzFileLength = new FileInfo(xyzFilename).Length;
			//Console.WriteLine("xyz file length: " + xyzFileLength);
			long xyzSize = dimension * sizeof(double);
			long xyzRecords = xyzFileLength / xyzSize;
			//Console.WriteLine("xyz record count: " + xyzRecords);

			if (xyzRecords * xyzSize != xyzFileLength)
			{
				throw new FormatException("Wrong xyz file format.");
			}

			byte[] xyzByteArray = File.ReadAllBytes(xyzFilename);

			double[] coordinateArray = new double[xyzByteArray.Length / sizeof(double)];
			Buffer.BlockCopy(xyzByteArray, 0, coordinateArray, 0, xyzByteArray.Length);

			// ---------------------------------------------------

			Mesh mesh = new Mesh();

			for (int i = 0; i < xyzRecords; i++)
			{
				Node node = new Node { Id = i + 1 };
				if (dimension > 0)
				{
					node.X = coordinateArray[i * dimension];
					if (dimension > 1)
					{
						node.Y = coordinateArray[i * dimension + 1];
						if (dimension > 2)
						{
							node.Z = coordinateArray[i * dimension + 2];
						}
					}
				}
				mesh.Nodes.Add(node);
			}

			//writer.WriteLine(e1dRecords + e2dRecords);

			//for (int i = 0; i < e1dRecords; i++)
			//{
			//	writer.WriteLine($"{i + 1} 1 {e1dConnectivity[i * 2]} {e1dConnectivity[i * 2 + 1]} 0");
			//}

			//for (int i = 0; i < e2dRecords; i++)
			//{
			//	writer.Write(e1dRecords + i + 1);
			//	if (e2dConnectivity[i * 4 + 2] == e2dConnectivity[i * 4 + 3]) // triangle
			//	{
			//		writer.Write(" 3");
			//		writer.Write(" " + e2dConnectivity[i * 4]);
			//		writer.Write(" " + e2dConnectivity[i * 4 + 1]);
			//		writer.Write(" " + e2dConnectivity[i * 4 + 2]);
			//	}
			//	else // quad
			//	{
			//		writer.Write(" 5 ");
			//		writer.Write(" " + e2dConnectivity[i * 4]);
			//		writer.Write(" " + e2dConnectivity[i * 4 + 1]);
			//		writer.Write(" " + e2dConnectivity[i * 4 + 2]);
			//		writer.Write(" " + e2dConnectivity[i * 4 + 3]);
			//	}
			//	writer.WriteLine(" 0"); // property
			//}

			return mesh;
		}
	}
}
