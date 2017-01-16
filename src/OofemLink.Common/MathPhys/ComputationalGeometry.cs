using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Common.MathPhys
{
    public static class ComputationalGeometry
    {
		/// <summary>
		/// Rotate vector <paramref name="v"/> around axis <paramref name="axis"/> by angle <paramref name="angle"/>
		/// </summary>
		/// <param name="v">Arbitrary vector that should be rotated</param>
		/// <param name="angle">Angle in radians</param>
		/// <param name="axis">Unit vector around which the vector <paramref name="v"/> will be rotated</param>
		/// <returns>Rotated vector with the same length as the input vector <paramref name="v"/></returns>
		public static Vector3d RotateVector(Vector3d v, double angle, Vector3d axis)
		{
			double cosTheta = Math.Cos(angle);
			double sinTheta = Math.Sin(angle);

			Vector3d rotated;
			rotated.X = (cosTheta + (1 - cosTheta) * axis.X * axis.X) * v.X;
			rotated.X += ((1 - cosTheta) * axis.X * axis.Y - axis.Z * sinTheta) * v.Y;
			rotated.X += ((1 - cosTheta) * axis.X * axis.Z + axis.Y * sinTheta) * v.Z;

			rotated.Y = ((1 - cosTheta) * axis.X * axis.Y + axis.Z * sinTheta) * v.X;
			rotated.Y += (cosTheta + (1 - cosTheta) * axis.Y * axis.Y) * v.Y;
			rotated.Y += ((1 - cosTheta) * axis.Y * axis.Z - axis.X * sinTheta) * v.Z;

			rotated.Z = ((1 - cosTheta) * axis.X * axis.Z - axis.Y * sinTheta) * v.X;
			rotated.Z += ((1 - cosTheta) * axis.Y * axis.Z + axis.X * sinTheta) * v.Y;
			rotated.Z += (cosTheta + (1 - cosTheta) * axis.Z * axis.Z) * v.Z;

			return rotated;
		}

		/// <summary>
		/// Transform angle in Degrees to Radians
		/// </summary>
		public static double Deg2Rad(double angleInDeg) => angleInDeg * Math.PI / 180.0;

		/// <summary>
		/// Transform angle in Radians to Degrees
		/// </summary>
		public static double Rad2Deg(double angleInRad) => angleInRad * 180.0 / Math.PI;
	}
}
