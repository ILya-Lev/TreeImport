using System.Collections.Generic;
using System.Threading;

namespace TreeImport
{
	public class TreeImporter
	{
		public readonly List<Asset> _output = new List<Asset>();
		public List<Asset> Process(IReadOnlyList<Asset> inputData)
		{
			ThreadPool.SetMinThreads(200, 200);
			var iterator = new TreeIterator<Asset, int>(
											asset => asset.Id,
											asset => asset.ParentId,
											inputData);

			TreeParallelProcessor.Process(iterator, SynchronizeNode, 15);

			return _output;
		}

		private void SynchronizeNode(Asset nodeToSync)
		{
			Thread.Sleep(1000);
			lock (_output)
			{
				_output.Add(nodeToSync);
			}
		}
	}
}