using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Engine.RigidBodies;

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
            Engine.Vector3 minV = new(float.MaxValue, float.MaxValue, float.MaxValue);
            Engine.Vector3 maxV = new(float.MinValue, float.MinValue, float.MinValue);

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

        public static BVHNode GenerateHierarchy(List<ulong> sortedMortonCodes, List<BVHLeaf> sortedLeaves, uint first, uint last)
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
            }
            while (step > 1);
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

        public static void TraverseRecursive(ref List<(int, int)> potentialContacts, ref BVH bvh, BoundingBox query, int queryObjectId, BVHNode node)
        {
            if (node == null) return;
            if(IntersectionTests.AABBOverlap(node.bounds, query))
            {
                if (node.isLeaf)
                    potentialContacts.Add((((BVHLeaf)node).objectId, queryObjectId));

                else
                {
                    TraverseRecursive(ref potentialContacts, ref bvh, query, queryObjectId, ((BVHInternal)node).left);
                    TraverseRecursive(ref potentialContacts, ref bvh, query, queryObjectId, ((BVHInternal)node).right);
                }
            }
        }
    }

    //public class BVHNodeB
    //{
    //    public (BVHNode? left, BVHNode? right) Children;
    //    public BoundingVolume Volume;
    //    public RigidBody? Body;

    //    public bool isLeaf => Body != null;

    //    public BVHNodeB(BoundingVolume volume, RigidBody? body = null)
    //    {
    //        Volume = volume;
    //        Body = body;
    //        Children = (null, null);
    //    }

    //    public static BVHNode GenerateHierarchyBoxes(List<BoundingBox> bodies, List<RigidBody> rigids)
    //    {
    //        if (bodies.Count == 0)
    //            throw new ArgumentException("Cannot generate BVH with no bodies");
    //        if (bodies.Count == 1)
    //        {
    //            var body = bodies[0];
    //            return new BVHNode(body, rigids[0]);
    //        }
    //        // Compute the bounding volume that contains all bodies
    //        var combinedVolume = BoundingBox.JoinAABBs(bodies);

    //        List<(BoundingBox box, RigidBody rigid, ulong MortonCode)> mortonCodes = new();
    //        Vector3 sceneMin = combinedVolume.center - combinedVolume.halfSize;
    //        Vector3 sceneMax = combinedVolume.center + combinedVolume.halfSize;
    //        for (int i = 0; i < bodies.Count; i++)
    //        {
    //            var code = MortonCodes.Encode(bodies[i].center, sceneMin, sceneMax);
    //            mortonCodes.Add((bodies[i], rigids[i], code));
    //        }

    //        mortonCodes.Sort((a, b) => a.MortonCode.CompareTo(b.MortonCode));
    //        int first = 0;
    //        int last = mortonCodes.Count - 1;

    //        return GenerateHierarchyRecursive(mortonCodes, first, last);
    //    }

    //    public static BVHNode GenerateHierarchyRecursive(List<(BoundingBox, RigidBody, ulong)> sorted, int first, int last)
    //    {
    //        if (first == last)
    //        {
    //            var (box, rigid, _) = sorted[first];
    //            return new BVHNode(box, rigid);
    //        }
    //        // Determine the split point
    //        int split = FindSplit(sorted, first, last); // Find the appropriate split index based on Morton codes
    //        var left = GenerateHierarchyRecursive(sorted, first, split);
    //        var right = GenerateHierarchyRecursive(sorted, split + 1, last);
    //        var combinedVolume = BoundingBox.JoinAABBs(new List<BoundingBox> { left.Volume as BoundingBox, right.Volume as BoundingBox });
    //        var node = new BVHNode(combinedVolume);
    //        node.Children = (left, right);
    //        return node;
    //    }

    //    static int countZeros(uint x)
    //    {
    //        // Keep shifting x by one until
    //        // leftmost bit does not become 1.
    //        int total_bits = sizeof(uint) * 8;
    //        int res = 0;
    //        while ((x & (1 << (total_bits - 1))) == 0)
    //        {
    //            x = (x << 1);
    //            res++;
    //        }
    //        return res;
    //    }

    //    public static int FindSplit(List<(BoundingBox box, RigidBody rigid, ulong MortonCode)> sorted, int first, int last)
    //    {
    //        uint firstCode = (uint)sorted[first].MortonCode;
    //        uint lastCode = (uint)sorted[last].MortonCode;
    //        if (firstCode == lastCode)
    //            return (first + last) / 2; // All Morton codes are the same, split in the middle

    //        int commonPrefix = countZeros((uint)(firstCode ^ lastCode));
    //        int split = first; // initial guess
    //        int step = last - first;

    //        do
    //        {
    //            step = (step + 1) >> 1; // exponential decrease
    //            int newSplit = split + step; // proposed new position

    //            if (newSplit < last)
    //            {
    //                ulong splitCode = sorted[newSplit].MortonCode;
    //                int splitPrefix = countZeros((uint)(firstCode ^ splitCode));
    //                if (splitPrefix > commonPrefix)
    //                    split = newSplit; // accept proposal
    //            }
    //        }
    //        while (step > 1);

    //        return split;

    //    }
    //}
}
