using System;
using System.Threading;
using System.Threading.Tasks;

namespace TreeImport
{
	/// <summary>
	/// Responsible for thread consumption limitation and all processings finishing -waits for it
	/// </summary>
	public class TreeParallelProcessor
	{
		public static void Process<TNodeType, TKey>(TreeIterator<TNodeType, TKey> iterator,
			Action<TNodeType> process,
			int threadsCount)
		{
			var finishedEvent = new ManualResetEvent(false);
			int toProcessCount = iterator.Count;
			var semaphore = new Semaphore(threadsCount, threadsCount);

			foreach (var node in iterator.GetNodesToSync())
			{
				semaphore.WaitOne();

				var nodeToSync = node;
				Task.Factory.StartNew(() =>
				{
					try
					{
						process(nodeToSync);
					}
					finally
					{
						semaphore.Release();
						iterator.OnNodeProcessingCompleted(nodeToSync);

						if (Interlocked.Decrement(ref toProcessCount) == 0)
							finishedEvent.Set();
					}
				}, TaskCreationOptions.LongRunning);
			}
			finishedEvent.WaitOne();
		}
	}
}