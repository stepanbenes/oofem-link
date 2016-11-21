using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Services.Import
{
	abstract class FormatParserBase
	{
		#region Protected methods

		protected static bool TryParseInt32(string text, out int result)
		{
			return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
		}

		protected static int ParseInt32(string text)
		{
			int result;
			if (!TryParseInt32(text, out result))
			{
				throw new FormatException($"32bit integer expected instead of '{text}'");
			}
			return result;
		}

		protected static byte ParseUInt8(string text)
		{
			byte result;
			if (!byte.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
			{
				throw new FormatException($"Unsigned 8bit integer expected instead of '{text}'");
			}
			return result;
		}

		protected static double ParseFloat64(string text)
		{
			double result;
			if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
			{
				throw new FormatException($"Floating-point number expected instead of '{text}'");
			}
			return result;
		}

		protected static float ParseFloat32(string text)
		{
			float result;
			if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
			{
				throw new FormatException($"Floating-point number expected instead of '{text}'");
			}
			return result;
		}

		#endregion
	}
}
