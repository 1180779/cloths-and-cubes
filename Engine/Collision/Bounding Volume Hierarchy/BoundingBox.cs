using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Engine.RigidBodies;

namespace Engine.Collision.Bounding_Volume_Hierarchy
{
    public abstract class BoundingVolume
    {
    }
    public class BoundingBox : BoundingVolume
    {
        public Engine.Vector3 center;
        public Engine.Vector3 halfSize;

        public BoundingBox(Vector3 center, Vector3 halfSize)
        {
            this.center = center;
            this.halfSize = halfSize;
        }

        public static BoundingBox CreateFastAABBFromBox(Box box)
        {
            return new BoundingBox(
                center: box.Body.Position,
                halfSize: new Vector3(box.HalfSize.Magnitude, box.HalfSize.Magnitude, box.HalfSize.Magnitude)
            );
        }

        public static BoundingBox JoinAABBs(BoundingBox a, BoundingBox b)
        {
            var min = new Vector3(
                Math.Min(a.center.X - a.halfSize.X, b.center.X - b.halfSize.X),
                Math.Min(a.center.Y - a.halfSize.Y, b.center.Y - b.halfSize.Y),
                Math.Min(a.center.Z - a.halfSize.Z, b.center.Z - b.halfSize.Z)
            );
            var max = new Vector3(
                Math.Max(a.center.X + a.halfSize.X, b.center.X + b.halfSize.X),
                Math.Max(a.center.Y + a.halfSize.Y, b.center.Y + b.halfSize.Y),
                Math.Max(a.center.Z + a.halfSize.Z, b.center.Z + b.halfSize.Z)
            );
            var center = new Vector3(
                (min.X + max.X) / 2,
                (min.Y + max.Y) / 2,
                (min.Z + max.Z) / 2
            );
            var halfSize = new Vector3(
                (max.X - min.X) / 2,
                (max.Y - min.Y) / 2,
                (max.Z - min.Z) / 2
            );
            return new BoundingBox(center, halfSize);
        }

        public static BoundingBox JoinAABBs(List<BoundingBox> bodies)
        {
            BoundingBox box = bodies[0];
            for (int i = 1; i < bodies.Count; i++)
            {
                box = JoinAABBs(box, bodies[i]);
            }
            return box;
        }
    }
}
