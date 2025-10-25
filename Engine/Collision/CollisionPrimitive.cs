using Engine.RigidBodies;

namespace Engine.Collision;

public class CollisionPrimitive
{
    public RigidBody Body = new();
    public Matrix4 Offset = new();

    /// <summary>
    /// The resultant transform of the primitive. This is
    /// calculated by combining the offset of the primitive
    /// with the transform of the rigid body.
    /// </summary>

    public Matrix4 Transform { get; private set; } = new();

    public void CalculateInternals()
    {
        Transform = Body.TransformMatrix * Offset;
    }

    /// <summary>
    /// Retrieves one of the three basis vectors or the position vector from the object's transformation matrix.
    /// The transformation matrix is updated before retrieval to ensure the returned vector is current.
    /// The vectors are returned in world coordinates.
    /// </summary>
    /// <param name="index">The index of the vector to retrieve. 0 for the x-axis, 1 for the y-axis, 2 for the z-axis, and 3 for the position.</param>
    /// <returns>The requested axis or position vector.</returns>
    public Vector3 GetAxis(int index)
    {
        CalculateInternals();
        return Transform.GetAxisVector(index);
    }
};