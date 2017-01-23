using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OofemLink.Common.Extensions
{
	public static class EnumerableExtensions
	{
		/// <summary>Adds a single element to the end of an IEnumerable.</summary>
		/// <typeparam name="T">Type of enumerable to return.</typeparam>
		/// <returns>IEnumerable containing all the input elements, followed by the specified additional element.</returns>
		public static IEnumerable<T> AppendItem<T>(this IEnumerable<T> source, T item)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));
			return concatIterator(item, source, false);
		}

		/// <summary>Adds a single element to the start of an IEnumerable.</summary>
		/// <typeparam name="T">Type of enumerable to return.</typeparam>
		/// <returns>IEnumerable containing the specified additional element, followed by all the input elements.</returns>
		public static IEnumerable<T> PrependItem<T>(this IEnumerable<T> tail, T head)
		{
			if (tail == null)
				throw new ArgumentNullException(nameof(tail));
			return concatIterator(head, tail, true);
		}

		public static IEnumerable<string> MergeIfEndsWith(this IEnumerable<string> source, string continuationToken)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			using (var iterator = source.GetEnumerator())
			{
				string acc = "";
				while (iterator.MoveNext())
				{
					string current = iterator.Current;
					acc += current;
					if (!current.EndsWith(continuationToken))
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

		public static IEnumerable<string> MergeIfStartsWith(this IEnumerable<string> source, string continuationToken)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			using (var iterator = source.GetEnumerator())
			{
				string acc = "";
				while (iterator.MoveNext())
				{
					string current = iterator.Current;
					if (!current.StartsWith(continuationToken) && !string.IsNullOrEmpty(acc))
					{
						yield return acc;
						acc = "";
					}
					acc += current;
				}

				if (!string.IsNullOrEmpty(acc))
				{
					yield return acc;
				}
			}
		}

		#region Private methods

		private static IEnumerable<T> concatIterator<T>(T extraElement, IEnumerable<T> source, bool insertAtStart)
		{
			if (insertAtStart)
				yield return extraElement;
			foreach (var e in source)
				yield return e;
			if (!insertAtStart)
				yield return extraElement;
		}

		#endregion
	}
}
