namespace Engine.Collision
{
    public class CollisionParticle : CollisionPrimitive
    {
        public Vector3 Radius { get; set; } = new();

        public override float Friction => 0.3f;
    }
}