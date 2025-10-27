namespace Engine.Collision;

public static class IntersectionTests
{
    private static Real TransformToAxis(
        CollisionBox box,
        Vector3 axis
    )
    {
        return
            box.HalfSize.X * (Real)Math.Abs(axis * box.GetAxis(0)) +
            box.HalfSize.Y * (Real)Math.Abs(axis * box.GetAxis(1)) +
            box.HalfSize.Z * (Real)Math.Abs(axis * box.GetAxis(2));
    }

    private static bool OverlapOnAxis(
        CollisionBox one,
        CollisionBox two,
        Vector3 axis,
        Vector3 toCentre
    )
    {
        // Project the half-size of one onto the axis
        Real oneProject = TransformToAxis(one, axis);
        Real twoProject = TransformToAxis(two, axis);

        // Project this onto the axis
        Real distance = (Real)Math.Abs(toCentre * axis);

        // Check for overlap
        return (distance < oneProject + twoProject);
    }

    public static bool BoxAndBox(
        CollisionBox one,
        CollisionBox two)
    {
        // Find the vector between the two centres
        Vector3 toCentre = two.GetAxis(3) - one.GetAxis(3);

        bool TestOverlap(Vector3 axis)
        {
            if(axis.SquareMagnitude() < (Real)0.0001) return true;
            return OverlapOnAxis(one, two, axis, toCentre);
        }

        return (
            // Check on box one's axes first
            TestOverlap(one.GetAxis(0)) &&
            TestOverlap(one.GetAxis(1)) &&
            TestOverlap(one.GetAxis(2)) &&

            // And on two's
            TestOverlap(two.GetAxis(0)) &&
            TestOverlap(two.GetAxis(1)) &&
            TestOverlap(two.GetAxis(2)) &&

            // Now on the cross-products
            TestOverlap(one.GetAxis(0) % two.GetAxis(0)) &&
            TestOverlap(one.GetAxis(0) % two.GetAxis(1)) &&
            TestOverlap(one.GetAxis(0) % two.GetAxis(2)) &&
            TestOverlap(one.GetAxis(1) % two.GetAxis(0)) &&
            TestOverlap(one.GetAxis(1) % two.GetAxis(1)) &&
            TestOverlap(one.GetAxis(1) % two.GetAxis(2)) &&
            TestOverlap(one.GetAxis(2) % two.GetAxis(0)) &&
            TestOverlap(one.GetAxis(2) % two.GetAxis(1)) &&
            TestOverlap(one.GetAxis(2) % two.GetAxis(2))
        );
    }

    // Needs some unit tests, wrote it without too much thought
    public static bool AABBOverlap(Engine.Collision.Bounding_Volume_Hierarchy.BoundingBox a, Engine.Collision.Bounding_Volume_Hierarchy.BoundingBox b)
    {
        if(a == null || b == null) return false;

        var distX = Math.Abs(a.center.X - b.center.X);
        var distY = Math.Abs(a.center.Y - b.center.Y);
        var distZ = Math.Abs(a.center.Z - b.center.Z);

        return (
            distX < a.halfSize.X + b.halfSize.X ||
            distY < a.halfSize.Y + b.halfSize.Y ||
            distZ < a.halfSize.Z + b.halfSize.Z);
    }
    
    /// <summary>
    /// Does an intersection test on an arbitrarily aligned box and a
    /// half-space.
    ///
    /// The box is given as a transform matrix, including
    /// position, and a vector of half-sizes for the extent of the
    /// box along each local axis.
    ///
    /// The half-space is given as a direction (i.e. unit) vector and the
    /// offset of the limiting plane from the origin, along the given
    /// direction.
    /// </summary>
    public static bool BoxAndHalfSpace(
        CollisionBox box,
        CollisionPlane plane)
    {
        // Work out the projected radius of the box onto the plane direction
        Real projectedRadius = TransformToAxis(box, plane.Direction);

        // Work out how far the box is from the origin
        Real boxDistance =
            plane.Direction *
            box.GetAxis(3) -
            projectedRadius;

        // Check for the intersection
        return boxDistance <= plane.Offset;
    }
};