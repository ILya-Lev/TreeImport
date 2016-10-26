using System.Collections.Generic;

namespace TreeImport
{
	public class Asset
	{
		public int Id { get; set; }
		public int ParentId { get; set; }
		public List<Asset> ChildAssets { get; } = new List<Asset>();

		public override string ToString() => $"id={Id} pid={ParentId} child={ChildAssets.Count}";
	}
}
