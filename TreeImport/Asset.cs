﻿using System.Collections.Generic;

namespace TreeImport
{
	class Asset
	{
		public int Id { get; set; }
		public int ParentId { get; set; }
		public List<Asset> ChildAssets { get; } = new List<Asset>();
	}
}
