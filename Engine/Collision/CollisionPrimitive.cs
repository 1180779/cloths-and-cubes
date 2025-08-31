using System.Diagnostics;
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
    
    public void CalculateInternals() {
        Transform = Body.TransformMatrix * Offset;
    }
    
    public Vector3 GetAxis(int index)
    {
        CalculateInternals();
        return Transform.GetAxisVector(index);
    }
};