using Engine.Collision;
using Engine.Collision.Bounding_Volume_Hierarchy;

namespace Engine.RigidBodies;

public sealed class Cone : CollisionCone, IBoxable
{
    public BoundingBox GetBoundingBox()
    {
        // The bounding box of a cone is contained within the bounding box of a cylinder
        // with the same radius and height.
        // So we can use the exact same logic as the Cylinder.
        return Cylinder.GetGetBoundingBox(this, Radius, Height);
    }
}