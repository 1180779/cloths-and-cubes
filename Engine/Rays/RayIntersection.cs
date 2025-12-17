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
        // Solves the intersection of a ray
        // P(t) = O + t*D
        // with a sphere
        // (X - C)^2 = R^2.
        //
        // This yields a quadratic equation in t:
        // (D*D)t^2 + 2(D*(O - C))t + (O - C)*(O - C) - R^2 = 0
        //
        // Let m = O - C. Assuming the ray direction is normalized (D*D = 1), this simplifies to:
        // t^2 + 2(D*m)t + m*m - R^2 = 0
        //
        // Let b = D*m and c = m*m - R^2.
        // The equation is
        // t^2 + 2bt + c = 0.
        // The solutions are t = -b +- sqrt(b^2 - c).
        distance = 0;

        Vector3 sphereCenter = sphere.GetAxis(3);
        Vector3 m = ray.Origin - sphereCenter;

        Real b = m * ray.Direction;
        Real c = m * m - sphere.Radius * sphere.Radius;

        // Exit if the ray's origin is outside the sphere (c > 0) and the ray is pointing away from the sphere (b > 0).
        if (c > 0.0f && b > 0.0f)
            return false;

        Real discr = b * b - c;

        // A negative discriminant corresponds to the ray missing the sphere.
        if (discr < 0.0f)
            return false;

        // Ray intersects the sphere. We want the smallest non-negative root, which is the nearest intersection point.
        // The smaller root is t0 = -b - sqrt(discr).

        Real t;
        Real sqrtDiscr = (Real)Math.Sqrt(discr);
        if (b < 0)
        {
            // When b is negative, compute the larger root t1 = -b + sqrt(discr) first,
            // and then find t0 using Vieta's formulas: t0 * t1 = c => t0 = c / t1.
            t = c / (-b + sqrtDiscr);
        }
        else
        {
            // When b is non-negative, use the standard formula for t0
            t = -b - sqrtDiscr;
        }

        distance = t;
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
        Real tMin = 0.0f;
        Real tMax = Real.MaxValue;

        Vector3 boxCenter = box.GetAxis(3);
        Vector3 localOrigin = ray.Origin - boxCenter;

        // X-axis slab test
        Vector3 axisX = box.GetAxis(0);
        Real dotOriginX = localOrigin * axisX;
        Real dotDirX = ray.Direction * axisX;
        if (!Slab(dotOriginX, dotDirX, 1.0f / dotDirX, -box.HalfSize.X, box.HalfSize.X, ref tMin, ref tMax))
            return false;

        // Y-axis slab test
        Vector3 axisY = box.GetAxis(1);
        Real dotOriginY = localOrigin * axisY;
        Real dotDirY = ray.Direction * axisY;
        if (!Slab(dotOriginY, dotDirY, 1.0f / dotDirY, -box.HalfSize.Y, box.HalfSize.Y, ref tMin, ref tMax))
            return false;

        // Z-axis slab test
        Vector3 axisZ = box.GetAxis(2);
        Real dotOriginZ = localOrigin * axisZ;
        Real dotDirZ = ray.Direction * axisZ;
        if (!Slab(dotOriginZ, dotDirZ, 1.0f / dotDirZ, -box.HalfSize.Z, box.HalfSize.Z, ref tMin, ref tMax))
            return false;

        distance = tMin;
        return true;
    }

    public static bool IntersectRayPlane(Ray ray, CollisionPlane plane, out Real distance)
    {
        distance = 0;
        Real denominator = plane.Direction * ray.Direction;

        if (denominator > -1e-6 && denominator < 1e-6)
        {
            return false;
        }

        Real t = (plane.Offset - (plane.Direction * ray.Origin)) / denominator;

        if (t < 0)
        {
            return false;
        }

        distance = t;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Slab(Real origin, Real dir, Real invDir, Real minVal, Real maxVal, ref Real tMin, ref Real tMax)
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

    /// <summary>
    /// Traverses a BVH to find all potential ray intersections.
    /// Returns the IDs of objects that potentially intersect with the ray.
    /// </summary>
    /// <param name="ray">The ray to test.</param>
    /// <param name="node">The current BVH node.</param>
    /// <param name="potentialHits">List to store IDs of potentially intersecting objects.</param>
    public static void TraverseBVHForRay(Ray ray, BVHNode? node, ref List<int> potentialHits)
    {
        if (node == null)
            return;

        var stack = new Stack<BVHNode?>();
        stack.Push(node);

        while (stack.Count > 0)
        {
            var currentNode = stack.Pop();

            if (currentNode == null)
                continue;

            if (!IntersectRayAABB(ray, currentNode.bounds, out _))
                continue;

            if (currentNode.isLeaf)
            {
                var leaf = (BVHLeaf)currentNode;
                potentialHits.Add(leaf.objectId);
            }
            else
            {
                var inter = (BVHInternal)currentNode;
                stack.Push(inter.left);
                stack.Push(inter.right);
            }
        }
    }

    /// <summary>
    /// Tests if a ray intersects with a triangle in 3D space.
    /// Uses the Möller–Trumbore intersection algorithm.
    /// </summary>
    /// <param name="ray">The ray to test against the triangle.</param>
    /// <param name="triangle">The triangle to test for intersection.</param>
    /// <param name="distance">The distance from the ray origin to the intersection point, if an intersection occurs.</param>
    /// <returns>True if the ray intersects the triangle, false otherwise.</returns>
    public static bool IntersectRayTriangle(Ray ray, Triangle triangle, out Real distance)
    {
        Vector3 v0 = triangle.Vertex1;
        Vector3 v1 = triangle.Vertex2;
        Vector3 v2 = triangle.Vertex3;
        distance = 0;

        // Find vectors for two edges sharing v0
        Vector3 edge1 = v1 - v0;
        Vector3 edge2 = v2 - v0;

        // Begin calculating determinant - also used to calculate u parameter
        Vector3 crossRayDirEdge2 = Vector3.CrossProduct(ray.Direction, edge2);
        Real det = edge1 * crossRayDirEdge2; 

        // if the determinant is near zero, ray lies in plane of triangle
        Real eps = Core.Epsilon;
        if (det > -eps && det < eps)
            return false;
        
        Real invDet = 1.0f / det;
        
        // Calculate distances from v0 to ray origin
        Vector3 originMinusV0 = ray.Origin - v0;
        
        // Calculate u parameter and test bounds
        Real baryU = invDet * (originMinusV0 * crossRayDirEdge2);
        if (baryU < 0.0f || baryU > 1.0f)
            return false;

        // Prepare to test V parameter
        Vector3 crossOrigMinusV0 = Vector3.CrossProduct(originMinusV0, edge1);
        
        // Calculate V parameter and test bounds
        Real baryV = (ray.Direction * crossOrigMinusV0) * invDet;
        if (baryV < 0.0f || baryU + baryV > 1.0f)
            return false;

        // Calculate V parameter and test bounds
        Real rayT = (edge2 * crossOrigMinusV0) * invDet;
        if (rayT >= 0)
        {
            distance = rayT;
            return true;
        }

        // Line intersection but not a ray intersection
        return false;
    }

    public static bool IntersectRayCloth(Ray ray, Triangle[] triangles, out Real distance)
    {
        distance = Real.MaxValue;
        bool hit = false;

        foreach (var triangle in triangles)
        {
            if (IntersectRayTriangle(ray, triangle, out Real triDistance))
            {
                if (triDistance < distance)
                {
                    distance = triDistance;
                    hit = true;
                }
            }
        }

        return hit;
    }
}