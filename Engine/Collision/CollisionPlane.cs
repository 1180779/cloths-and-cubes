namespace Engine.Collision;

public class CollisionPlane
{
    private Vector3 direction = new();

    /// <summary>
    /// The plane normal. Returned value is normalized.
    /// When setting this value, it will be normalized.
    /// Does not modify the caller's instance.
    /// </summary>
    public Vector3 Direction
    {
        get => direction;
        set
        {
            direction = new Vector3(value);
            direction.Normalize();
        }
    }

    /// <summary>
    /// The distance of the plane from the origin.
    /// </summary>
    public float Offset;

    public CollisionPlane()
    {
    }

    public CollisionPlane(Vector3 direction)
    {
        Direction = direction;
    }
};