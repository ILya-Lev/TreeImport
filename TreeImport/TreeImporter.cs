using MoreLinq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TreeImport
{
	public class TreeImporter
	{
		// asset - level map
		private Dictionary<int, int> _results;
		public List<Asset> _output;

		public List<Asset> Process(IReadOnlyList<Asset> inputData)
		{
			_output = new List<Asset>();
			IReadOnlyDictionary<int, Asset> sortedInput = inputData.ToDictionary(a => a.Id);

			foreach (var pair in sortedInput)
			{
				if (sortedInput.ContainsKey(pair.Value.ParentId))
					sortedInput[pair.Value.ParentId].ChildAssets.Add(pair.Value);
			}

			var roots = FindRoots(sortedInput).ToList();
			ImportTrees(roots, sortedInput);

			return _output;
		}

		private static IEnumerable<Asset> FindRoots(IReadOnlyDictionary<int, Asset> sortedInput)
		{
			foreach (var pair in sortedInput)
			{
				if (!sortedInput.ContainsKey(pair.Value.ParentId))
					yield return pair.Value;
			}
		}

		/// <summary>
		/// Sergey's solution with some small changes from my side
		/// </summary>
		/// <param name="roots"></param>
		/// <param name="sortedInput"></param>
		private void ImportTrees(IReadOnlyList<Asset> roots, IReadOnlyDictionary<int, Asset> sortedInput)
		{
			Semaphore semaphore = new Semaphore(15, 15);
			var parentSynchronizedEvent = new ManualResetEvent(false);

			var availableNodesToSync = new ConcurrentBag<Asset>(roots);
			List<Task> threads = new List<Task>();

			for (int processedNodes = 0; processedNodes < sortedInput.Count;)
			{
				Asset nodeToSync = null;
				if (!availableNodesToSync.TryTake(out nodeToSync))
				{
					parentSynchronizedEvent.WaitOne();
					parentSynchronizedEvent.Reset();
				}

				if (nodeToSync == null)
				{
					continue;
				}

				semaphore.WaitOne();
				var aTask = Task.Factory.StartNew(() =>
				{
					try
					{
						SynchronizeNode(nodeToSync);
					}
					finally
					{
						//add synchronized asset children to asset processing
						nodeToSync.ChildAssets.ForEach(c => availableNodesToSync.Add(c));
						semaphore.Release();
						//setting event to release waiting asset
						parentSynchronizedEvent.Set();
					}
				});
				processedNodes++;
				threads.Add(aTask);
			}

			Task.WaitAll(threads.ToArray());
		}

		private void SynchronizeNode(Asset nodeToSync)
		{
			lock (_output)
			{
				Thread.Sleep(1000);
				_output.Add(nodeToSync);
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