using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Engine.RigidBodies;

namespace Engine.Collision.Bounding_Volume_Hierarchy
{
    public class BVHNode
    {
        public (BVHNode? left, BVHNode? right) Children;
        public BoundingVolume Volume;
        public RigidBody? Body;

        public bool isLeaf => Body != null;

        public BVHNode(BoundingVolume volume, RigidBody? body = null)
        {
            Volume = volume;
            Body = body;
            Children = (null, null);
        }

        public static BVHNode GenerateHierarchyBoxes(List<BoundingBox> bodies, List<RigidBody> rigids)
        {
            if (bodies.Count == 0)
                throw new ArgumentException("Cannot generate BVH with no bodies");
            if (bodies.Count == 1)
            {
                var body = bodies[0];
                return new BVHNode(body, rigids[0]);
            }
            // Compute the bounding volume that contains all bodies
            var combinedVolume = BoundingBox.JoinAABBs(bodies);

            List<(BoundingBox box, RigidBody rigid, ulong MortonCode)> mortonCodes = new();
            Vector3 sceneMin = combinedVolume.center - combinedVolume.halfSize;
            Vector3 sceneMax = combinedVolume.center + combinedVolume.halfSize;
            for (int i = 0; i < bodies.Count; i++)
            {
                var code = MortonCodes.Encode(bodies[i].center, sceneMin, sceneMax);
                mortonCodes.Add((bodies[i], rigids[i], code));
            }

            mortonCodes.Sort((a, b) => a.MortonCode.CompareTo(b.MortonCode));
            int first = 0;
            int last = mortonCodes.Count - 1;

            return GenerateHierarchyRecursive(mortonCodes, first, last);
        }

        public static BVHNode GenerateHierarchyRecursive(List<(BoundingBox, RigidBody, ulong)> sorted, int first, int last)
        {
            if (first == last)
            {
                var (box, rigid, _) = sorted[first];
                return new BVHNode(box, rigid);
            }
            // Determine the split point
            int split = FindSplit(sorted, first, last); // Find the appropriate split index based on Morton codes
            var left = GenerateHierarchyRecursive(sorted, first, split);
            var right = GenerateHierarchyRecursive(sorted, split + 1, last);
            var combinedVolume = BoundingBox.JoinAABBs(new List<BoundingBox> { left.Volume as BoundingBox, right.Volume as BoundingBox });
            var node = new BVHNode(combinedVolume);
            node.Children = (left, right);
            return node;
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

        public static int FindSplit(List<(BoundingBox box, RigidBody rigid, ulong MortonCode)> sorted, int first, int last)
        {
            uint firstCode = (uint)sorted[first].MortonCode;
            uint lastCode = (uint)sorted[last].MortonCode;
            if (firstCode == lastCode)
                return (first + last) / 2; // All Morton codes are the same, split in the middle

            int commonPrefix = countZeros((uint)(firstCode ^ lastCode));
            int split = first; // initial guess
            int step = last - first;

            do
            {
                step = (step + 1) >> 1; // exponential decrease
                int newSplit = split + step; // proposed new position

                if (newSplit < last)
                {
                    ulong splitCode = sorted[newSplit].MortonCode;
                    int splitPrefix = countZeros((uint)(firstCode ^ splitCode));
                    if (splitPrefix > commonPrefix)
                        split = newSplit; // accept proposal
                }
            }
            while (step > 1);

            return split;

        }
    }
}
