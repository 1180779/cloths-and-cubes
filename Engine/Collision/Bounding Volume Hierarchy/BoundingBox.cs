using Engine.RigidBodies;

namespace Engine.Collision.Bounding_Volume_Hierarchy
{
    public abstract class BoundingVolume
    {
    }

    public class BoundingBox : BoundingVolume
    {
        public Vector3 Center;
        public Vector3 HalfSize;

        public BoundingBox(Vector3 center, Vector3 halfSize)
        {
            this.Center = center;
            this.HalfSize = halfSize;
        }

        public static BoundingBox CreateFastAABBFromBox(Box box)
        {
            return new BoundingBox(
                center: box.Body.Position,
                halfSize: new Vector3(box.HalfSize.Magnitude, box.HalfSize.Magnitude, box.HalfSize.Magnitude)
            );
        }

        public static BoundingBox CreateFastAABBFromParticle(Particle particle)
        {
            return new BoundingBox(
                center: particle.position,
                halfSize: new Vector3(0.1f, 0.1f, 0.1f) // small AABB for particle
            );
        }

        public static BoundingBox JoinAABBs(BoundingBox a, BoundingBox b)
        {
            var min = new Vector3(
                Math.Min(a.Center.X - a.HalfSize.X, b.Center.X - b.HalfSize.X),
                Math.Min(a.Center.Y - a.HalfSize.Y, b.Center.Y - b.HalfSize.Y),
                Math.Min(a.Center.Z - a.HalfSize.Z, b.Center.Z - b.HalfSize.Z)
            );
            var max = new Vector3(
                Math.Max(a.Center.X + a.HalfSize.X, b.Center.X + b.HalfSize.X),
                Math.Max(a.Center.Y + a.HalfSize.Y, b.Center.Y + b.HalfSize.Y),
                Math.Max(a.Center.Z + a.HalfSize.Z, b.Center.Z + b.HalfSize.Z)
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

    public interface IBoxable
    {
        public BoundingBox GetBoundingBox();
    }
}