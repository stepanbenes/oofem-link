using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Common.Extensions
{
    public static class StringExtensions
    {
		public static IEnumerable<string> Split(this string source, int chunkSize)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));
			if (chunkSize <= 0)
				throw new ArgumentOutOfRangeException(nameof(chunkSize));
			if (source.Length <= chunkSize)
				return Enumerable.Repeat(source, 1);

			return Enumerable.Range(0, source.Length / chunkSize)
				.Select(i => source.Substring(i * chunkSize, chunkSize));
		}
	}
}
