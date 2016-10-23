using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TreeImport
{
	class Program
	{
		static void Main (string[] args)
		{
			var inputData = GenerateData(10);
			PrintSequence(inputData);

			var importer = new TreeImporter();
			var outputData = importer.Process(inputData);
			PrintSequence(outputData);
		}

		private static void PrintSequence (IReadOnlyList<Asset> sequence)
		{
			sequence.ForEach(a => Console.Write(a.Id + " "));
			Console.WriteLine();
			sequence.ForEach(a => Console.Write(a.ParentId + " "));
			Console.WriteLine("\n\n");
		}

		private static IReadOnlyList<Asset> GenerateData (int amount)
		{
			var random = new Random();
			return Enumerable.Range(1, amount).Select(n => new Asset
			{
				Id = n,
				ParentId = random.Next(amount)

			}).Pipe(a => { if (a.Id == a.ParentId) a.ParentId--; })
			.ToList();
		}
	}
}
