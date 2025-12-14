using System.Runtime.CompilerServices;

using Engine.Collision;
using Engine.Collision.Bounding_Volume_Hierarchy;

namespace Engine.Rays;

/// <summary>
/// Provides ray intersection tests for various collision primitives and bounding volumes.
/// </summary>
public static class RayIntersection
{
    /// <summary>
    /// Tests if a ray intersects with an axis-aligned bounding box.
    /// Uses the slab method for efficient intersection testing.
    /// </summary>
    /// <param name="ray">The ray to test.</param>
    /// <param name="box">The bounding box to test against.</param>
    /// <param name="distance">The distance to the intersection point (if any).</param>
    /// <returns>True if the ray intersects the box, false otherwise.</returns>
    public static bool IntersectRayAABB(Ray ray, BoundingBox box, out Real distance)
    {
        distance = 0;

        Vector3 min = box.Center - box.HalfSize;
        Vector3 max = box.Center + box.HalfSize;

        Real tMin = 0.0f;
        Real tMax = Real.MaxValue;

        // Use cached inverse direction
        Vector3 inv = ray.InvDirection;

        if (!Slab(ray.Origin.X, ray.Direction.X, inv.X, min.X, max.X, ref tMin, ref tMax)) return false;
        if (!Slab(ray.Origin.Y, ray.Direction.Y, inv.Y, min.Y, max.Y, ref tMin, ref tMax)) return false;
        if (!Slab(ray.Origin.Z, ray.Direction.Z, inv.Z, min.Z, max.Z, ref tMin, ref tMax)) return false;

        distance = tMin;
        return true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool Slab(Real origin, Real dir, Real invDir, Real minVal, Real maxVal, ref Real tMin, ref Real tMax)
        {
            Real eps = Core.Epsilon;

            // Parallel case would produce Nans (0 * infinity)
            if (dir > -eps && dir < eps)
                return origin >= minVal && origin <= maxVal;

            Real t1 = (minVal - origin) * invDir;
            Real t2 = (maxVal - origin) * invDir;

            if (t1 > t2)
                (t1, t2) = (t2, t1);

            if (t1 > tMin) tMin = t1;
            if (t2 < tMax) tMax = t2;

            return tMin <= tMax;
        }
    }

    /// <summary>
    /// Tests if a ray intersects with a sphere.
    /// </summary>
    /// <param name="ray">The ray to test.</param>
    /// <param name="sphere">The collision sphere to test against.</param>
    /// <param name="distance">The distance to the nearest intersection point (if any).</param>
    /// <returns>True if the ray intersects the sphere, false otherwise.</returns>
    public static bool IntersectRaySphere(Ray ray, CollisionSphere sphere, out Real distance)
    {
        // P(t) = O + t*D                       C - sphere center
        // |X - C|^2 = R^2                      X - point on the sphere surface
        // |P(t) - C|^2 = R^2                   R - sphere radius
        // |O + t*D - C|^2 = R^2                D - ray direction
        // |O + t*D - C|^2 - R^2 = 0
        //
        // |m + t*D|^2 - R^2 = 0                m = O - C
        // m*m + 2*m*t*D + t^2*D*D - R^2 = 0
        // (D*D) t^2 + 2 (m*D) t + (m*m − R^2) = 0
        distance = 0;

        Vector3 sphereCenter = sphere.GetAxis(3);
        Vector3 m = ray.Origin - sphereCenter;

        Real b = m * ray.Direction;
        Real c = m * m - sphere.Radius * sphere.Radius;

        // Exit if the ray's origin is outside the sphere (c > 0) and the ray is pointing away from the sphere (b > 0)
        if (c > 0.0f && b > 0.0f)
            return false;

        Real discr = b * b - c;

        // A negative discriminant corresponds to the ray missing the sphere
        if (discr < 0.0f)
            return false;

        // Ray now found to intersect the sphere, compute smallest t value of intersection
        distance = -b - (Real)Math.Sqrt(discr);

        // If t is negative, the ray started inside the sphere so clamp t to zero
        if (distance < 0.0f)
            distance = 0.0f;

        return true;
    }

     /// <summary>
    /// Tests if a ray intersects with an oriented bounding box.
    /// This is more expensive than AABB intersection but handles rotated boxes.
    /// </summary>
    /// <param name="ray">The ray to test.</param>
    /// <param name="box">The collision box to test against.</param>
    /// <param name="distance">The distance to the intersection point (if any).</param>
    /// <returns>True if the ray intersects the box, false otherwise.</returns>
    public static bool IntersectRayOBB(Ray ray, CollisionBox box, out Real distance)
    {
        distance = 0;

        // Transform ray into box's local space
        Vector3 boxCenter = box.GetAxis(3);
        Vector3 localOrigin = ray.Origin - boxCenter;

        // Get the three axes of the box
        Vector3 axisX = box.GetAxis(0);
        Vector3 axisY = box.GetAxis(1);
        Vector3 axisZ = box.GetAxis(2);

        // Transform ray to local space by projecting onto box axes
        Vector3 localRayOrigin = new(
            localOrigin * axisX,
            localOrigin * axisY,
            localOrigin * axisZ
        );

        Vector3 localRayDir = new(
            ray.Direction * axisX,
            ray.Direction * axisY,
            ray.Direction * axisZ
        );

        // Now perform AABB test in local space
        Vector3 min = new(-box.HalfSize.X, -box.HalfSize.Y, -box.HalfSize.Z);
        Vector3 max = new(box.HalfSize.X, box.HalfSize.Y, box.HalfSize.Z);

        Real tMin = 0.0f;
        Real tMax = Real.MaxValue;

        for (int i = 0; i < 3; i++)
        {
            Real origin = i == 0 ? localRayOrigin.X : (i == 1 ? localRayOrigin.Y : localRayOrigin.Z);
            Real dir = i == 0 ? localRayDir.X : (i == 1 ? localRayDir.Y : localRayDir.Z);
            Real minVal = i == 0 ? min.X : (i == 1 ? min.Y : min.Z);
            Real maxVal = i == 0 ? max.X : (i == 1 ? max.Y : max.Z);

            if (Math.Abs(dir) < Core.Epsilon)
            {
                if (origin < minVal || origin > maxVal)
                    return false;
            }
            else
            {
                Real ood = 1.0f / dir;
                Real t1 = (minVal - origin) * ood;
                Real t2 = (maxVal - origin) * ood;

                if (t1 > t2)
                {
                    (t1, t2) = (t2, t1);
                }

                tMin = Math.Max(tMin, t1);
                tMax = Math.Min(tMax, t2);

                if (tMin > tMax)
                    return false;
            }
        }

        distance = tMin;
        return true;
    }
    
    /// <summary>
    /// Traverses a BVH to find all potential ray intersections.
    /// Returns the IDs of objects that potentially intersect with the ray.
    /// </summary>
    /// <param name="ray">The ray to test.</param>
    /// <param name="node">The current BVH node.</param>
    /// <param name="potentialHits">List to store IDs of potentially intersecting objects.</param>
    public static void TraverseBVHForRay(Ray ray, BVHNode? node, List<int> potentialHits)
    {
        if (node == null)
            return;

        // Test against bounding box
        if (!IntersectRayAABB(ray, node.bounds, out _))
            return;

        if (node.isLeaf)
        {
            // Leaf node - add object ID to potential hits
            BVHLeaf leaf = (BVHLeaf)node;
            potentialHits.Add(leaf.objectId);
        }
        else
        {
            // Internal node - recurse on children
            BVHInternal inter = (BVHInternal)node;
            TraverseBVHForRay(ray, inter.left, potentialHits);
            TraverseBVHForRay(ray, inter.right, potentialHits);
        }
    }
}