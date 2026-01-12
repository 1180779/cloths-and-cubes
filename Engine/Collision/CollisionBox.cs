namespace Engine.Collision;

public class CollisionBox : CollisionPrimitive
{
    public Vector3 HalfSize = new();

    public override float Friction => 0.95f;
};