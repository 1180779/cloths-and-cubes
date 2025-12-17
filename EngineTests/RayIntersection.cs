using System.Collections;

using Engine;
using Engine.Collision;
using Engine.Collision.Bounding_Volume_Hierarchy;
using Engine.Rays;

namespace EngineTests;

public sealed class RayIntersection
{
    [Test]
    [TestCaseSource(nameof(AABBIntersectionTestCases))]
    public void AABB_AxisAligned_Test(Ray ray, BoundingBox box, bool expectedHit, float expectedDistance)
    {
        var actualHit = Engine.Rays.RayIntersection.IntersectRayAABB(ray, box, out var actualDistance);

        Assert.That(actualHit, Is.EqualTo(expectedHit));
        if (expectedHit)
        {
            Assert.That(actualDistance, Is.EqualTo(expectedDistance).Within(Core.Epsilon));
        }
    }

    private static IEnumerable AABBIntersectionTestCases()
    {
        // Standard Box for most tests:
        // Center(0,0,0)
        // HalfSize(1,1,1)
        //
        // Min(-1,-1,-1), Max(1,1,1)
        var standardBox = new BoundingBox(Vector3.Zero, new Vector3(1, 1, 1));

        // Case 1: Ray starting inside the box
        yield return new TestCaseData(
            new Ray(Vector3.Zero, new Vector3(0, 0, 1)),
            standardBox,
            true,
            0.0f
        ).SetName("AABB_Inside_PointingOut");

        // Case 2: Ray starting outside and pointing towards the box
        yield return new TestCaseData(
            new Ray(new Vector3(0, 0, -5), new Vector3(0, 0, 1)),
            standardBox,
            true,
            4.0f
        ).SetName("AABB_Outside_PointingTowards");

        // Case 3: Ray starting outside and pointing away from the box
        yield return new TestCaseData(
            new Ray(new Vector3(0, 0, -5), new Vector3(0, 0, -1)),
            standardBox,
            false,
            0.0f
        ).SetName("AABB_Outside_PointingAway");

        // Case 4: Ray grazes the edge of the box (touches X = 1, Y = 1 boundary)
        yield return new TestCaseData(
            new Ray(new Vector3(1, 1, -5), new Vector3(0, 0, 1)),
            standardBox,
            true,
            4.0f
        ).SetName("AABB_Graze_Edge");

        // Case 6: Ray is parallel to X-axis.
        // It is inside the Y-slab (Y = 0 is in [-1, 1]), but outside the Z-slab (Z = -5 is not in [-1, 1]).
        yield return new TestCaseData(
            new Ray(new Vector3(0, 0, -5), new Vector3(1, 0, 0)),
            new BoundingBox(new Vector3(5, 0, 0), new Vector3(1, 1, 1)),
            false,
            0.0f
        ).SetName("AABB_Parallel_X_InsideY_OutsideZ_Miss");

        // Case 7: Ray is parallel to X-axis.
        // It is outside the Y-slab (Y = 2 is not in [-1, 1]).
        yield return new TestCaseData(
            new Ray(new Vector3(0, 2, -5), new Vector3(1, 0, 0)),
            new BoundingBox(new Vector3(5, 0, 0), new Vector3(1, 1, 1)),
            false,
            0.0f
        ).SetName("AABB_Parallel_X_OutsideY_Miss");

        // Case 7b: Ray is parallel to Y-axis.
        // It is outside the X-slab (X = 0 is not in [4, 6]).
        yield return new TestCaseData(
            new Ray(new Vector3(0, 0, 0), new Vector3(0, 1, 0)),
            new BoundingBox(new Vector3(5, 0, 0), new Vector3(1, 1, 1)),
            false,
            0.0f
        ).SetName("AABB_Parallel_Y_OutsideX_Miss");

        // Case 8: Ray aimed exactly at a corner vertex (1, 1, 1)
        // Distance equal to unit cubes diagonal i.e. sqrt(3)
        var dirToCorner = new Vector3(-1, -1, -1);
        dirToCorner.Normalise();
        yield return new TestCaseData(
            new Ray(new Vector3(2, 2, 2), dirToCorner),
            standardBox,
            true,
            MathF.Sqrt(3)
        ).SetName("AABB_Corner_Hit_ExactVertex");

        // Case 8: Ray starting inside and passing through a corner vertex (1, 1, 1)
        var dirFromCenter = new Vector3(1, 1, 1);
        dirFromCenter.Normalise();
        yield return new TestCaseData(
            new Ray(Vector3.Zero, dirFromCenter),
            standardBox,
            true,
            0.0f
        ).SetName("AABB_Inside_ThroughCorner");

        // Case 9: Ray grazing a face (Parallel to face)
        // Ray at X = 1 (boundary), pointing in Z.
        // It touches the face X=1.
        yield return new TestCaseData(
            new Ray(new Vector3(1, 0, -5), new Vector3(0, 0, 1)),
            standardBox,
            true,
            4.0f
        ).SetName("AABB_Graze_Face_Parallel");

        // Case 10: Ray just slightly missing the corner at (1,1,1). 
        // Ray runs along the Z = 1.00f plane, but with X = Y = 1.01f at the closest point near the corner (1, 1, 1)
        var zPlaneDirection = new Vector3(1.0f, -1.0f, 0.0f);
        yield return new TestCaseData(
            new Ray(new Vector3(1.01f, 1.01f, 1.00f) - zPlaneDirection, zPlaneDirection),
            standardBox,
            false,
            0.0f
        ).SetName("AABB_NearMiss_Corner");
    }

    [Test]
    [TestCaseSource(nameof(PlaneIntersectionTestCases))]
    public void PlaneIntersection_Test(Ray ray, CollisionPlane plane, bool expectedHit, float expectedDistance)
    {
        var actualHit = Engine.Rays.RayIntersection.IntersectRayPlane(ray, plane, out var actualDistance);

        Assert.That(actualHit, Is.EqualTo(expectedHit));
        if (expectedHit)
        {
            Assert.That(actualDistance, Is.EqualTo(expectedDistance).Within(Core.Epsilon));
        }
    }

    private static IEnumerable PlaneIntersectionTestCases()
    {
        // Plane with normal (0,1,0) passing through the origin
        var yPlane = new CollisionPlane { Direction = new Vector3(0, 1, 0), Offset = 0 };

        // Case 1: Ray pointing towards the plane
        yield return new TestCaseData(
            new Ray(new Vector3(0, 5, 0), new Vector3(0, -1, 0)),
            yPlane,
            true,
            5.0f
        ).SetName("Plane_Hit");

        // Case 2: Ray pointing away from the plane
        yield return new TestCaseData(
            new Ray(new Vector3(0, 5, 0), new Vector3(0, 1, 0)),
            yPlane,
            false,
            0.0f
        ).SetName("Plane_Miss_PointingAway");

        // Case 3: Ray parallel to the plane
        yield return new TestCaseData(
            new Ray(new Vector3(1, 1, 0), new Vector3(1, 0, 0)),
            yPlane,
            false,
            0.0f
        ).SetName("Plane_Parallel");

        // Case 4: Ray starting on the plane
        yield return new TestCaseData(
            new Ray(new Vector3(0, 0, 0), new Vector3(0, -1, 0)),
            yPlane,
            true,
            0.0f
        ).SetName("Plane_StartOnPlane");

        // Case 5: Ray behind the plane and pointing away
        yield return new TestCaseData(
            new Ray(new Vector3(0, -5, 0), new Vector3(0, -1, 0)),
            yPlane,
            false,
            0.0f
        ).SetName("Plane_Behind_PointingAway");
    }

    [Test]
    [TestCaseSource(nameof(SphereIntersectionTestCases))]
    public void SphereIntersection_Test(Ray ray, CollisionSphere sphere, bool expectedHit, float expectedDistance)
    {
        var actualHit = Engine.Rays.RayIntersection.IntersectRaySphere(ray, sphere, out var actualDistance);

        Assert.That(actualHit, Is.EqualTo(expectedHit));
        if (expectedHit)
        {
            Assert.That(actualDistance, Is.EqualTo(expectedDistance).Within(Core.Epsilon));
        }
    }

    private static IEnumerable SphereIntersectionTestCases()
    {
        // Sphere at origin with radius 1
        var sphere = new CollisionSphere { Radius = 1.0f, Body = { Position = Vector3.Zero } };

        // Case 1: Ray pointing towards the sphere center
        yield return new TestCaseData(
            new Ray(new Vector3(0, 0, -5), new Vector3(0, 0, 1)),
            sphere,
            true,
            4.0f
        ).SetName("Sphere_CenterHit");

        // Case 2: Ray misses the sphere
        yield return new TestCaseData(
            new Ray(new Vector3(0, 2, -5), new Vector3(0, 0, 1)),
            sphere,
            false,
            0.0f
        ).SetName("Sphere_Miss");

        // Case 3: Ray grazes the sphere
        yield return new TestCaseData(
            new Ray(new Vector3(0, 1, -5), new Vector3(0, 0, 1)),
            sphere,
            true,
            5.0f
        ).SetName("Sphere_Graze");

        // Case 4: Ray starts inside the sphere
        yield return new TestCaseData(
            new Ray(new Vector3(0, 0, 0), new Vector3(0, 0, 1)),
            sphere,
            true,
            -1.0f
        ).SetName("Sphere_Inside");
    }
    
    [Test]
    [TestCaseSource(nameof(TriangleIntersectionTestCases))]
    public void TriangleIntersection_Test(Ray ray, Triangle triangle, bool expectedHit, float expectedDistance)
    {
        var actualHit = Engine.Rays.RayIntersection.IntersectRayTriangle(ray, triangle, out var actualDistance);

        Assert.That(actualHit, Is.EqualTo(expectedHit));
        if (expectedHit)
        {
            Assert.That(actualDistance, Is.EqualTo(expectedDistance).Within(Core.Epsilon));
        }
    }

    private static IEnumerable TriangleIntersectionTestCases()
    {
        // Triangle on the XY plane
        var triangle = new Triangle(new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0));

        // Case 1: Ray hits the triangle
        yield return new TestCaseData(
            new Ray(new Vector3(0.25f, 0.25f, -1), new Vector3(0, 0, 1)),
            triangle,
            true,
            1.0f
        ).SetName("Triangle_Hit");

        // Case 2: Ray misses the triangle
        yield return new TestCaseData(
            new Ray(new Vector3(1, 1, -1), new Vector3(0, 0, 1)),
            triangle,
            false,
            0.0f
        ).SetName("Triangle_Miss");

        // Case 3: Ray hits a vertex
        yield return new TestCaseData(
            new Ray(new Vector3(0, 0, -1), new Vector3(0, 0, 1)),
            triangle,
            true,
            1.0f
        ).SetName("Triangle_VertexHit");

        // Case 4: Ray hits an edge
        yield return new TestCaseData(
            new Ray(new Vector3(0.5f, 0, -1), new Vector3(0, 0, 1)),
            triangle,
            true,
            1.0f
        ).SetName("Triangle_EdgeHit");
        
        // Case 5: Ray near misses an edge
        yield return new TestCaseData(
            new Ray(new Vector3(0.5f, -0.01f, -1), new Vector3(0, 0, 1)),
            triangle,
            false,
            0.0f
        ).SetName("Triangle_NearMiss_Edge");
        
        // Case 6: Ray near misses a vertex
        yield return new TestCaseData(
            new Ray(new Vector3(-0.01f, -0.01f, -1), new Vector3(0, 0, 1)),
            triangle,
            false,
            0.0f
        ).SetName("Triangle_NearMiss_Vertex");
    }
}