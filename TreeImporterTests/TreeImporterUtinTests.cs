using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using TreeImport;

namespace TreeImporterTests
{
	[TestClass]
	public class TreeImporterUtinTests
	{
		[TestMethod]
		public void Process_PredefinedTree_ShouldGenerateProperOutput()
		{
			//acquire
			IReadOnlyList<Asset> inputData = new[]
			{
				new Asset {Id = 1, ParentId = 2},
				new Asset {Id = 2, ParentId = 3},
				new Asset {Id = 3, ParentId = 4},
				new Asset {Id = 4, ParentId = 5},
				new Asset {Id = 5, ParentId = 6},
				new Asset {Id = 6, ParentId = 0},
				new Asset {Id = 7, ParentId = 6},
				new Asset {Id = 8, ParentId = 5},
				new Asset {Id = 9, ParentId = 4},
				new Asset {Id = 10, ParentId = 3},
				new Asset {Id = 11, ParentId = 3},
			};
			var importer = new TreeImporter();
			//act
			var outputData = importer.Process(inputData);
			//assert
			StandardCheck(outputData, inputData);
		}

		[TestMethod]
		public void Process_PredefinedTree_Parallel_Child_Processing()
		{
			IReadOnlyList<Asset> inputData = new[]
			 {
				new Asset {Id = 1, ParentId = 0},
				new Asset {Id = 2, ParentId = 1},
				new Asset {Id = 3, ParentId = 1},
				new Asset {Id = 4, ParentId = 1},
				new Asset {Id = 5, ParentId = 1},
				new Asset {Id = 6, ParentId = 1},
				new Asset {Id = 7, ParentId = 1},
				new Asset {Id = 8, ParentId = 1},
				new Asset {Id = 9, ParentId = 1},
				new Asset {Id = 10, ParentId = 1},
				new Asset {Id = 11, ParentId = 1},
			};

			var importer = new TreeImporter();
			//act
			var outputData = importer.Process(inputData);
			//assert
			StandardCheck(outputData, inputData);
		}
		[TestMethod]
		public void Process_PredefinedTree1_ShouldGenerateProperOutput()
		{
			//acquire
			IReadOnlyList<Asset> inputData = new[]
			{
				new Asset {Id = 1, ParentId = 0},
				new Asset {Id = 2, ParentId = 1},
				new Asset {Id = 3, ParentId = 4},
				new Asset {Id = 4, ParentId = 11},
				new Asset {Id = 5, ParentId = 10},
				new Asset {Id = 6, ParentId = 9},
				new Asset {Id = 7, ParentId = 8},
				new Asset {Id = 8, ParentId = 0},
				new Asset {Id = 9, ParentId = 10},
				new Asset {Id = 10, ParentId = 0},
				new Asset {Id = 11, ParentId = 10},
			};
			var importer = new TreeImporter();
			//act
			var outputData = importer.Process(inputData);
			//assert
			StandardCheck(outputData, inputData);
		}
		[TestMethod]
		public void Process_PredefinedTree2_ShouldGenerateProperOutput()
		{
			//acquire
			IReadOnlyList<Asset> inputData = new[]
			{
				new Asset {Id = 1, ParentId = 2},
				new Asset {Id = 2, ParentId = 3},
				new Asset {Id = 3, ParentId = 0},
				new Asset {Id = 4, ParentId = 5},
				new Asset {Id = 5, ParentId = 6},
				new Asset {Id = 6, ParentId = 0},
				new Asset {Id = 7, ParentId = 6},
				new Asset {Id = 8, ParentId = 5},
				new Asset {Id = 9, ParentId = 1},
				new Asset {Id = 10, ParentId = 3},
				new Asset {Id = 11, ParentId = 3},
			};
			var importer = new TreeImporter();
			//act
			var outputData = importer.Process(inputData);
			//assert
			StandardCheck(outputData, inputData);
		}
		[TestMethod]
		public void Process_RandomTreeOf10_ShouldGenerateProperOutput()
		{
			//acquire
			IReadOnlyList<Asset> inputData = GenerateData(10);
			var importer = new TreeImporter();
			//act
			var outputData = importer.Process(inputData);
			//assert
			StandardCheck(outputData, inputData);
		}
		[TestMethod]
		public void Process_RandomTreeOf1000_ShouldGenerateProperOutput()
		{
			//acquire
			IReadOnlyList<Asset> inputData = GenerateData(10000);
			var importer = new TreeImporter();
			//act
			var outputData = importer.Process(inputData);
			//assert
			StandardCheck(outputData, inputData);
		}

		private static void StandardCheck(List<Asset> outputData, IReadOnlyList<Asset> inputData)
		{
			outputData.Count.Should().Be(inputData.Count, "all elements should be synchronized");
			//check that there is no parent elements after current one
			for (int i = 0; i < outputData.Count; i++)
			{
				var parentIdx = outputData.FindIndex(a => a.Id == outputData[i].ParentId);
				parentIdx.Should().BeLessThan(i
					, "output sequence should contain all parent nodes in front of its child ones");
			}
		}

		private static IReadOnlyList<Asset> GenerateData(int amount)
		{
			var random = new Random();
			var tree = Enumerable.Range(1, amount).Select(n => new Asset
			{
				Id = n,
				ParentId = random.Next(amount)

			}).Pipe(a => { if (a.Id == a.ParentId) a.ParentId--; })
			.ToList();

			var checkedItems = new HashSet<int>();
			foreach (var asset in tree)
			{
				if (checkedItems.Contains(asset.Id)) continue;

				checkedItems.Add(asset.Id);
				var parent = tree.FirstOrDefault(a => a.Id == asset.ParentId);
				while (parent != null)
				{
					checkedItems.Add(parent.Id);
					if (checkedItems.Contains(parent.ParentId))
					{
						parent.ParentId = 0;
						break;
					}
					parent = tree.FirstOrDefault(a => a.Id == parent.ParentId);
				}
			}
			return tree;
		}
	}

}
