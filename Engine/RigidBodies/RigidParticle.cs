using Engine.Collision;
using Engine.Collision.Bounding_Volume_Hierarchy;

namespace Engine.RigidBodies
{
    public class ClothRigidParticle : RigidParticle
    {
        public required int ClothParticleX { get; init; }
        public required int ClothParticleY { get; init; }
        public required Cloth AttachedToCloth { get; init; }
    }

    public class RigidParticle : CollisionParticle, IBoxable
    {
        public const float BoundingBoxHalfSize = 0.04f;
        public const float BoxScale = 2 * BoundingBoxHalfSize;

        public virtual BoundingBox GetBoundingBox()
        {
            return new BoundingBox(
                center: this.Body.Position,
                halfSize: new Vector3(BoundingBoxHalfSize, BoundingBoxHalfSize,
                    BoundingBoxHalfSize) // small AABB for particle
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

        public void RefreshPhysicsState()
        {
            Matrix3 tensor = new();
            Body.SetInertiaTensor(tensor);

            Body.LinearDamping = 0.95f;
            Body.AngularDamping = 0.8f;

            Body.SetAwake();
            Body.CalculateDerivedData();
            CalculateInternals();
        }
    }
}