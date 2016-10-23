using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace TreeImport
{
	internal class TreeImporter
	{
		private List<Asset> _results;
		public IReadOnlyList<Asset> Process (IReadOnlyList<Asset> inputData)
		{
			_results = new List<Asset>();
			IReadOnlyDictionary<int, Asset> sortedInput = inputData.ToDictionary(a => a.Id);
			var alreadyProcessed = new ConcurrentDictionary<int, byte>();

			var answer = sortedInput.AsParallel().AsOrdered()
				.SelectMany(pair => ProcessSubTree(alreadyProcessed, pair, sortedInput))
				.ToList();

			return answer;
		}

		private IEnumerable<Asset> ProcessSubTree (ConcurrentDictionary<int, byte> alreadyProcessed,
													KeyValuePair<int, Asset> pair,
													IReadOnlyDictionary<int, Asset> sortedInput)
		{
			if (alreadyProcessed.ContainsKey(pair.Key)) yield break;

			var path = new Stack<Asset>();
			path.Push(pair.Value);

			var currentParent = pair.Value.ParentId;
			while (currentParent != 0 && !alreadyProcessed.ContainsKey(currentParent))
			{
				if (!sortedInput.ContainsKey(currentParent))
					break;
				if (!alreadyProcessed.TryAdd(currentParent, 1))
					break;
				path.Push(sortedInput[currentParent]);
				currentParent = path.Peek().ParentId;
			}

			while (path.Count > 0)
			{
				yield return path.Pop();
			}
		}
	}
}