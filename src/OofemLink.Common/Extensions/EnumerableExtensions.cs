using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Common.Extensions
{
	public static class EnumerableExtensions
	{
		public static IEnumerable<string> MergeIfEndsWith(this IEnumerable<string> source, string continuationToken)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			using (var iterator = source.GetEnumerator())
			{
				string acc = "";
				while (iterator.MoveNext())
				{
					string currentTrimmed = iterator.Current.Trim();
					acc += currentTrimmed;
					if (!currentTrimmed.EndsWith(continuationToken))
					{
						yield return acc;
						acc = "";
					}
				}

				if (!string.IsNullOrEmpty(acc))
				{
					yield return acc;
				}
			}
		}
	}
}
