namespace Engine.Collision.Bounding_Volume_Hierarchy
{
	public class BVHNode
	{
		public BoundingBox bounds;
		public bool isLeaf { get; set; }
	}

	public class BVHInternal : BVHNode
	{
		public BVHNode left;
		public BVHNode right;

		public BVHInternal(BoundingBox bounds, BVHNode left, BVHNode right)
		{
			this.bounds = bounds;
			this.left = left;
			this.right = right;
			this.isLeaf = false;
		}
	}

	public class BVHLeaf : BVHNode
	{
		public int objectId;

		public BVHLeaf(BoundingBox bounds, int id)
		{
			this.bounds = bounds;
			this.objectId = id;
			this.isLeaf = true;
		}
	}

	public class BVH
	{
		public BVHNode root;

		public BVH(BVHNode root)
		{
			this.root = root;
		}

		public static BVH Build(Dictionary<int, IBoxable> bodies)
		{
			Vector3 minV = new(float.MaxValue, float.MaxValue, float.MaxValue);
			Vector3 maxV = new(float.MinValue, float.MinValue, float.MinValue);

			Dictionary<int, BoundingBox> bodyDict = new Dictionary<int, BoundingBox>();
			foreach (var b in bodies)
			{
				bodyDict[b.Key] = b.Value.GetBoundingBox();
			}

			foreach (var box in bodyDict.Values)
			{
				var boxMin = box.center - box.halfSize;
				var boxMax = box.center + box.halfSize;
				minV.X = Math.Min(minV.X, boxMin.X);
				minV.Y = Math.Min(minV.Y, boxMin.Y);
				minV.Z = Math.Min(minV.Z, boxMin.Z);
				maxV.X = Math.Max(maxV.X, boxMax.X);
				maxV.Y = Math.Max(maxV.Y, boxMax.Y);
				maxV.Z = Math.Max(maxV.Z, boxMax.Z);
			}

			List<BVHLeaf> leaves = new();
			List<ulong> mortonCodes = new();

			foreach (var kvp in bodyDict)
			{
				var code = MortonCodes.Encode(kvp.Value.center, minV, maxV);
				mortonCodes.Add(code);
				leaves.Add(new BVHLeaf(kvp.Value, kvp.Key));
			}

			var sortedLeaves = leaves.Zip(mortonCodes, (leaf, code) => (leaf, code))
				.OrderBy(x => x.code)
				.Select(x => x.leaf)
				.ToList();
			mortonCodes.Sort();

			BVHNode root = GenerateHierarchy(mortonCodes, sortedLeaves, 0, (uint)(sortedLeaves.Count - 1));
			return new BVH(root);
		}

		public static BVHNode GenerateHierarchy(
			List<ulong> sortedMortonCodes,
			List<BVHLeaf> sortedLeaves,
			uint first,
			uint last)
		{
			if (first == last)
			{
				return sortedLeaves[(int)first];
			}

			int split = FindSplit(sortedMortonCodes, first, last);
			BVHNode left = GenerateHierarchy(sortedMortonCodes, sortedLeaves, first, (uint)split);
			BVHNode right = GenerateHierarchy(sortedMortonCodes, sortedLeaves, (uint)(split + 1), last);
			var combinedVolume = BoundingBox.JoinAABBs(new List<BoundingBox> { left.bounds, right.bounds });
			return new BVHInternal(combinedVolume, left, right);
		}

		public static int FindSplit(List<ulong> sortedMortonCodes, uint first, uint last)
		{
			uint firstCode = (uint)sortedMortonCodes[(int)first];
			uint lastCode = (uint)sortedMortonCodes[(int)last];
			if (firstCode == lastCode)
				return (int)((first + last) / 2); // All Morton codes are the same, split in the middle

			int commonPrefix = countZeros((uint)(firstCode ^ lastCode));
			int split = (int)first; // initial guess
			int step = (int)(last - first);
			do
			{
				step = (step + 1) >> 1; // exponential decrease
				int newSplit = split + step; // proposed new position
				if (newSplit < last)
				{
					ulong splitCode = sortedMortonCodes[newSplit];
					int splitPrefix = countZeros((uint)(firstCode ^ splitCode));
					if (splitPrefix > commonPrefix)
						split = newSplit; // accept proposal
				}
			} while (step > 1);

			return split;
		}

		static int countZeros(uint x)
		{
			// Keep shifting x by one until
			// leftmost bit does not become 1.
			int total_bits = sizeof(uint) * 8;
			int res = 0;
			while ((x & (1 << (total_bits - 1))) == 0)
			{
				x = (x << 1);
				res++;
			}

			return res;
		}

		public static void GetPotentialContacts(ref List<(int, int)> potentialContacts, BVHNode? node)
		{
			if (node == null || node.isLeaf)
			{
				return;
			}

			var internalNode = (BVHInternal)node;

			GetPotentialContacts(ref potentialContacts, internalNode.left);
			GetPotentialContacts(ref potentialContacts, internalNode.right);

			TestOverlap(ref potentialContacts, internalNode.left, internalNode.right);
		}

		private static void TestOverlap(ref List<(int, int)> potentialContacts, BVHNode? node1, BVHNode? node2)
		{
			if (node1 == null || node2 == null) return;

			if (!IntersectionTests.AABBOverlap(node1.bounds, node2.bounds))
			{
				return;
			}

			if (node1.isLeaf && node2.isLeaf)
			{
				var leaf1 = (BVHLeaf)node1;
				var leaf2 = (BVHLeaf)node2;
				if (leaf1.objectId != leaf2.objectId)
				{
					var pair = leaf1.objectId < leaf2.objectId
						? (leaf1.objectId, leaf2.objectId)
						: (leaf2.objectId, leaf1.objectId);
					potentialContacts.Add(pair);
				}

				return;
			}

			if (node1.isLeaf) // and node2 is not
			{
				var internalNode2 = (BVHInternal)node2;
				TestOverlap(ref potentialContacts, node1, internalNode2.left);
				TestOverlap(ref potentialContacts, node1, internalNode2.right);
			}
			else if (node2.isLeaf) // and node1 is not
			{
				var internalNode1 = (BVHInternal)node1;
				TestOverlap(ref potentialContacts, internalNode1.left, node2);
				TestOverlap(ref potentialContacts, internalNode1.right, node2);
			}
			else // both are internal
			{
				var internal1 = (BVHInternal)node1;
				var internal2 = (BVHInternal)node2;
				TestOverlap(ref potentialContacts, internal1.left, internal2.left);
				TestOverlap(ref potentialContacts, internal1.left, internal2.right);
				TestOverlap(ref potentialContacts, internal1.right, internal2.left);
				TestOverlap(ref potentialContacts, internal1.right, internal2.right);
			}
		}
	}
}