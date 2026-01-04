using System.Collections.Concurrent;

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
        public BVHNode? root;

        public BVH(BVHNode? root)
        {
            this.root = root;
        }

        public static BVH Build(Dictionary<int, IBoxable> bodies)
        {
            if (bodies.Count == 0)
            {
                return new BVH(null);
            }


            Vector3 minV = new(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 maxV = new(float.MinValue, float.MinValue, float.MinValue);

            ConcurrentDictionary<int, BoundingBox> concurrentBodyDict = new ConcurrentDictionary<int, BoundingBox>();
            Parallel.ForEach(bodies, body =>
            {
                concurrentBodyDict[body.Key] = body.Value.GetBoundingBox();
            });

            int partitionCount = Environment.ProcessorCount;
            Vector3[] minVpartitions = new Vector3[partitionCount];
            Vector3[] maxVpartitions = new Vector3[partitionCount];
            Parallel.For(0, partitionCount, partitionIndex =>
            {
                Vector3 localMinV = new(float.MaxValue, float.MaxValue, float.MaxValue);
                Vector3 localMaxV = new(float.MinValue, float.MinValue, float.MinValue);
                int count = 0;
                foreach (var body in concurrentBodyDict)
                {
                    if (count % partitionCount == partitionIndex)
                    {
                        var box = body.Value;
                        var boxMin = box.Center - box.HalfSize;
                        var boxMax = box.Center + box.HalfSize;
                        localMinV.X = Math.Min(localMinV.X, boxMin.X);
                        localMinV.Y = Math.Min(localMinV.Y, boxMin.Y);
                        localMinV.Z = Math.Min(localMinV.Z, boxMin.Z);
                        localMaxV.X = Math.Max(localMaxV.X, boxMax.X);
                        localMaxV.Y = Math.Max(localMaxV.Y, boxMax.Y);
                        localMaxV.Z = Math.Max(localMaxV.Z, boxMax.Z);
                    }

                    count++;
                }

                minVpartitions[partitionIndex] = localMinV;
                maxVpartitions[partitionIndex] = localMaxV;
            });
            for (int i = 0; i < partitionCount; i++)
            {
                minV.X = Math.Min(minV.X, minVpartitions[i].X);
                minV.Y = Math.Min(minV.Y, minVpartitions[i].Y);
                minV.Z = Math.Min(minV.Z, minVpartitions[i].Z);
                maxV.X = Math.Max(maxV.X, maxVpartitions[i].X);
                maxV.Y = Math.Max(maxV.Y, maxVpartitions[i].Y);
                maxV.Z = Math.Max(maxV.Z, maxVpartitions[i].Z);
            }


            // TODO: check if the morton codes are allowed to duplicate
            //  seems to work for now
            // Handle Morton code collisions by grouping objects with the same code
            var leafList = new List<(ulong code, BVHLeaf leaf)>();
            var leafListLock = new object();

            Parallel.ForEach(concurrentBodyDict, kvp =>
            {
                var code = MortonCodes.Encode(kvp.Value.Center, minV, maxV);
                var leaf = new BVHLeaf(kvp.Value, kvp.Key);
                lock (leafListLock)
                {
                    leafList.Add((code, leaf));
                }
            });

            var sortedPairs = leafList.OrderBy(x => x.code).ToList();
            var sortedLeaves = sortedPairs.Select(x => x.leaf).ToList();
            var sortedMortonCodes = sortedPairs.Select(x => x.code).ToList();
            BVHNode root = GenerateHierarchy(sortedMortonCodes, sortedLeaves, 0, (uint)(sortedLeaves.Count - 1));
            return new BVH(root);
        }

        public static BVH BuildSynchronous(Dictionary<int, IBoxable> bodies)
        {
            if (bodies.Count == 0) return new BVH(null);

            Vector3 minV = new(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 maxV = new(float.MinValue, float.MinValue, float.MinValue);

            Dictionary<int, BoundingBox> bodyDict = new Dictionary<int, BoundingBox>();
            foreach (var body in bodies)
            {
                bodyDict[body.Key] = body.Value.GetBoundingBox();
            }

            foreach (var body in bodyDict)
            {
                var box = body.Value;
                var boxMin = box.Center - box.HalfSize;
                var boxMax = box.Center + box.HalfSize;
                minV.X = Math.Min(minV.X, boxMin.X);
                minV.Y = Math.Min(minV.Y, boxMin.Y);
                minV.Z = Math.Min(minV.Z, boxMin.Z);
                maxV.X = Math.Max(maxV.X, boxMax.X);
                maxV.Y = Math.Max(maxV.Y, boxMax.Y);
                maxV.Z = Math.Max(maxV.Z, boxMax.Z);
            }

            // Handle Morton code collisions by preserving all objects
            var leafList = new List<(ulong code, BVHLeaf leaf)>();
            foreach (var kvp in bodyDict)
            {
                var code = MortonCodes.Encode(kvp.Value.Center, minV, maxV);
                var leaf = new BVHLeaf(kvp.Value, kvp.Key);
                leafList.Add((code, leaf));
            }

            // Sort by Morton code, but preserve all objects even with duplicate codes
            var sortedPairs = leafList.OrderBy(x => x.code).ToList();
            var sortedLeaves = sortedPairs.Select(x => x.leaf).ToList();
            var sortedMortonCodes = sortedPairs.Select(x => x.code).ToList();
            BVHNode root = GenerateHierarchy(sortedMortonCodes, sortedLeaves, 0, (uint)(sortedLeaves.Count - 1));
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
            var combinedVolume = BoundingBox.JoinAABBs([left.bounds, right.bounds]);
            return new BVHInternal(combinedVolume, left, right);
        }

        public static int FindSplit(List<ulong> sortedMortonCodes, uint first, uint last)
        {
            if (sortedMortonCodes.Count < first)
                throw new ArgumentOutOfRangeException(nameof(first));
            if (sortedMortonCodes.Count < last)
                throw new ArgumentOutOfRangeException(nameof(last));

            uint firstCode = (uint)sortedMortonCodes[(int)first];
            uint lastCode = (uint)sortedMortonCodes[(int)last];
            if (firstCode == lastCode)
                return (int)((first + last) / 2); // All Morton codes are the same, split in the middle

            int commonPrefix = CountZeros((uint)(firstCode ^ lastCode));
            int split = (int)first; // initial guess
            int step = (int)(last - first);
            do
            {
                step = (step + 1) >> 1; // exponential decrease
                int newSplit = split + step; // proposed new position
                if (newSplit < last)
                {
                    ulong splitCode = sortedMortonCodes[newSplit];
                    int splitPrefix = CountZeros((uint)(firstCode ^ splitCode));
                    if (splitPrefix > commonPrefix)
                        split = newSplit; // accept proposal
                }
            } while (step > 1);

            return split;
        }

        private static int CountZeros(uint x)
        {
            if (x == 0) return 32;
            // Keep shifting x by one until
            // the leftmost bit does not become 1.
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

            Stack<BVHNode> nodeStack = new Stack<BVHNode>();
            Stack<(BVHNode, BVHNode)> overlapStack = new Stack<(BVHNode, BVHNode)>();

            nodeStack.Push(node);

            // First pass: traverse tree and collect overlap pairs
            while (nodeStack.Count > 0)
            {
                var current = nodeStack.Pop();

                if (current.isLeaf)
                {
                    continue;
                }

                var internalNode = (BVHInternal)current;

                // Push children for further processing
                nodeStack.Push(internalNode.left);
                nodeStack.Push(internalNode.right);

                // Push this pair for overlap testing
                overlapStack.Push((internalNode.left, internalNode.right));
            }

            // Second pass: process all overlap tests
            while (overlapStack.Count > 0)
            {
                var (node1, node2) = overlapStack.Pop();
                TestOverlap(ref potentialContacts, ref overlapStack, node1, node2);
            }
        }

        private static void TestOverlap(
            ref List<(int, int)> potentialContacts,
            ref Stack<(BVHNode, BVHNode)> overlapStack,
            BVHNode? node1,
            BVHNode? node2)
        {
            if (node1 == null || node2 == null) return;

            if (!IntersectionTests.AABBOverlap(node1.bounds, node2.bounds))
            {
                return;
            }

            // Both are leaf nodes, potential contact
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

            // node1 is a leaf, and node2 is not, check node1 with children of node2
            if (node1.isLeaf)
            {
                var internalNode2 = (BVHInternal)node2;
                overlapStack.Push((node1, internalNode2.left));
                overlapStack.Push((node1, internalNode2.right));
            }
            // node1 is not a leaf, and node1 is a left, check children of node1 with node2
            else if (node2.isLeaf)
            {
                var internalNode1 = (BVHInternal)node1;
                overlapStack.Push((internalNode1.left, node2));
                overlapStack.Push((internalNode1.right, node2));
            }
            // both nodes are internal, check 4 pairs of children
            else
            {
                var internal1 = (BVHInternal)node1;
                var internal2 = (BVHInternal)node2;
                overlapStack.Push((internal1.left, internal2.left));
                overlapStack.Push((internal1.left, internal2.right));
                overlapStack.Push((internal1.right, internal2.left));
                overlapStack.Push((internal1.right, internal2.right));
            }
        }
    }
}