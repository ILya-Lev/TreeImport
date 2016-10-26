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
	/// <typeparam name="TNodeType"></typeparam>
	/// <typeparam name="TKey"></typeparam>
	public class TreeIterator<TNodeType, TKey>
	{
		private readonly Func<TNodeType, TKey> _getId;
		private readonly Func<TNodeType, TKey> _getParentId;
		private readonly IReadOnlyDictionary<TKey, TNodeType> _nodesDictionary;

		private ConcurrentBag<TNodeType> _nodesAvailableForProcessing;
		private readonly ManualResetEvent _nodeProcessedEvent = new ManualResetEvent(false);


		public TreeIterator(Func<TNodeType, TKey> getId,
			Func<TNodeType, TKey> getParentId,
			IEnumerable<TNodeType> nodes)
		{
			_getId = getId;
			_getParentId = getParentId;
			_nodesDictionary = nodes.ToDictionary(getId);
			Count = _nodesDictionary.Count;
		}

		public int Count { get; set; }

		public IEnumerable<TNodeType> GetNodesToSync()
		{
			int processed = 0;

			_nodesAvailableForProcessing = new ConcurrentBag<TNodeType>();
			var roots = GetRoots();
			foreach (var node in roots)
			{
				yield return node;
				processed++;
			}

			while (processed != Count)
			{
				TNodeType nodeToSync;
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

		public void OnNodeProcessingCompleted(TNodeType node)
		{
			var parentId = _getId(node);
			var childNodes = _nodesDictionary.Values.Where(n => _getParentId(n).Equals(parentId));
			foreach (var childNode in childNodes)
			{
				_nodesAvailableForProcessing.Add(childNode);
			}
			_nodeProcessedEvent.Set();
		}
		private IEnumerable<TNodeType> GetRoots()
		{
			foreach (var node in _nodesDictionary.Values)
			{
				if (!_nodesDictionary.ContainsKey(_getParentId(node)))
				{
					yield return node;
				}
			}
		}
	}
}