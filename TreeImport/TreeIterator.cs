using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace TreeImport
{
	/// <summary>
	/// Returns Tree nodes and their children after 
	/// parent processing(when OnNodeProcessingCompleted - thread safe method)
	/// </summary>
	/// <typeparam name="Asset"></typeparam>
	public class TreeIterator
	{
		private readonly IReadOnlyDictionary<int, Asset> _nodesDictionary;
		private ConcurrentBag<Asset> _nodesAvailableForProcessing;
		private readonly ManualResetEvent _nodeProcessedEvent = new ManualResetEvent(false);

		public int Count { get; set; }

		public TreeIterator(IEnumerable<Asset> nodes,
							Func<Asset, int> idGenerator,
							Func<Asset, int> parentIdGenerator)
		{
			_nodesDictionary = nodes.ToDictionary(idGenerator);

			FillInChildren();

			Count = _nodesDictionary.Count;
		}

		public IEnumerable<Asset> GetNodesToSync()
		{
			int processed = 0;

			_nodesAvailableForProcessing = new ConcurrentBag<Asset>();
			var roots = GetRoots();
			foreach (var node in roots)
			{
				yield return node;
				processed++;
			}

			while (processed != Count)
			{
				Asset nodeToSync;
				if (!_nodesAvailableForProcessing.TryTake(out nodeToSync))
				{
					_nodeProcessedEvent.WaitOne();
					_nodeProcessedEvent.Reset();
					continue;
				}
				processed++;
				yield return nodeToSync;
			}
		}

		public void OnNodeProcessingCompleted(Asset entity)
		{
			foreach (var childNode in entity.ChildAssets)
			{
				_nodesAvailableForProcessing.Add(childNode);
			}
			_nodeProcessedEvent.Set();
		}

		private IEnumerable<Asset> GetRoots()
		{
			return _nodesDictionary.Values.Where(node => !_nodesDictionary.ContainsKey(node.ParentId));
		}

		private void FillInChildren()
		{
			foreach (var node in _nodesDictionary.Values)
			{
				if (_nodesDictionary.ContainsKey(node.ParentId))
					_nodesDictionary[node.ParentId].ChildAssets.Add(node);
			}
		}
	}
}