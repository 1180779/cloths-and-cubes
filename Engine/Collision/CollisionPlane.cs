namespace Engine.Collision;

public class CollisionPlane
{
    /**
     * The plane normal
     */
    public Vector3 Direction = new();

    /**
     * The distance of the plane from the origin.
     */
    public float Offset;

    public CollisionPlane()
    {
    }

    public CollisionPlane(Vector3 direction)
    {
        this.Direction = direction;
    }
};