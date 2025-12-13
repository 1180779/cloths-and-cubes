using Engine.Collision;
using Engine.Collision.Bounding_Volume_Hierarchy;

namespace Engine.RigidBodies
{
    public class RigidParticle : CollisionParticle, IBoxable
    {
        public Collision.Bounding_Volume_Hierarchy.BoundingBox GetBoundingBox()
        {
            return new Collision.Bounding_Volume_Hierarchy.BoundingBox(
                center: this.Body.Position,
                halfSize: new Vector3(0.001f, 0.001f, 0.001f) // small AABB for particle
            );
        }

        public void SetState(
            Vector3 position,
            float extents,
            Vector3 velocity,
            float mass = 0.1f)
        {
            Body.Position = position;
            Body.Velocity = velocity;
            Body.Rotation = new();
            Body.Mass = mass;

            Matrix3 tensor = new();
            Body.SetInertiaTensor(tensor);

            Body.LinearDamping = 0.95f;
            Body.AngularDamping = 0.8f;
            Body.ClearAccumulators();
            Body.Acceleration = new(0, -10f, 0);

            Body.SetAwake();
            Body.CalculateDerivedData();
        }
    }
}