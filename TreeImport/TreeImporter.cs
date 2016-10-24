using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TreeImport
{
	internal class TreeImporter
	{
		// asset - level map
		private Dictionary<int, int> _results;

		public void Process(IReadOnlyList<Asset> inputData)
		{
			IReadOnlyDictionary<int, Asset> sortedInput = inputData.ToDictionary(a => a.Id);
			var leveledItems = SortByLevel(sortedInput);

			foreach (IGrouping<int, Asset> level in leveledItems)
			{
				//level.AsParallel().Pipe(a => Console.Write(level.Key + " " + a.Id + " " + a.ParentId))
				//	.ToList();
				Parallel.ForEach(level, a => Console.WriteLine($"{level.Key} {a.Id} {a.ParentId}"));
			}
		}

		private List<IGrouping<int, Asset>> SortByLevel(IReadOnlyDictionary<int, Asset> sortedInput)
		{
			_results = new Dictionary<int, int>();

			var alreadyProcessed = new HashSet<int>();

			sortedInput.Pipe(pair => ProcessSubTree(alreadyProcessed, pair, sortedInput)).ToList();

			return _results.Select(pair => new
			{
				Level = pair.Value,
				Asset = sortedInput[pair.Key]
			})
							.GroupBy(item => item.Level, item => item.Asset)
							.ToList();
		}

		private void ProcessSubTree(HashSet<int> alreadyProcessed,
									KeyValuePair<int, Asset> pair,
									IReadOnlyDictionary<int, Asset> sortedInput)
		{
			if (alreadyProcessed.Contains(pair.Key)) return;
			alreadyProcessed.Add(pair.Key);

			var path = new Stack<Asset>();
			path.Push(pair.Value);

			var currentParent = pair.Value.ParentId;
			while (currentParent != 0 && !alreadyProcessed.Contains(currentParent))
			{
				if (!sortedInput.ContainsKey(currentParent))
					break;
				alreadyProcessed.Add(currentParent);

				var parentAsset = sortedInput[currentParent];
				path.Push(parentAsset);

				currentParent = parentAsset.ParentId;
			}

			var level = 0;
			while (path.Count > 0)
			{
				var asset = path.Pop();
				if (_results.ContainsKey(asset.ParentId))
					level = _results[asset.ParentId] + 1;

				//if (!_results.ContainsKey(asset.Id))
				_results.Add(asset.Id, level++);
			}
		}
	}
}