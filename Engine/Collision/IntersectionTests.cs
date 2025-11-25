namespace Engine.Collision;

/// <summary>
/// Provides a static set of methods for performing exact, fine-phase intersection
/// tests between various collision primitives.
/// </summary>
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

	/// <summary>
	/// Checks if two oriented bounding boxes are intersecting.
	/// </summary>
	/// <param name="one">The first collision box.</param>
	/// <param name="two">The second collision box.</param>
	/// <returns>True if the boxes intersect, false otherwise.</returns>
	public static bool BoxAndBox(
		CollisionBox one,
		CollisionBox two)
	{
		// Find the vector between the two centres
		var toCentre = two.GetAxis(3) - one.GetAxis(3);

		if (!TestOverlap(one.GetAxis(0))) return false;
		if (!TestOverlap(one.GetAxis(1))) return false;
		if (!TestOverlap(one.GetAxis(2))) return false;

		if (!TestOverlap(two.GetAxis(0))) return false;
		if (!TestOverlap(two.GetAxis(1))) return false;
		if (!TestOverlap(two.GetAxis(2))) return false;

		if (!TestOverlap(one.GetAxis(0) % two.GetAxis(0))) return false;
		if (!TestOverlap(one.GetAxis(0) % two.GetAxis(1))) return false;
		if (!TestOverlap(one.GetAxis(0) % two.GetAxis(2))) return false;
		if (!TestOverlap(one.GetAxis(1) % two.GetAxis(0))) return false;
		if (!TestOverlap(one.GetAxis(1) % two.GetAxis(1))) return false;
		if (!TestOverlap(one.GetAxis(1) % two.GetAxis(2))) return false;
		if (!TestOverlap(one.GetAxis(2) % two.GetAxis(0))) return false;
		if (!TestOverlap(one.GetAxis(2) % two.GetAxis(1))) return false;
		if (!TestOverlap(one.GetAxis(2) % two.GetAxis(2))) return false;

		return true;

		bool TestOverlap(Vector3 axis)
		{
			if (axis.SquareMagnitude() < (Real)0.0001) return true;
			return OverlapOnAxis(one, two, axis, toCentre);
		}
	}


	// Needs some unit tests, wrote it without too much thought
	public static bool AABBOverlap(Bounding_Volume_Hierarchy.BoundingBox a, Bounding_Volume_Hierarchy.BoundingBox b)
	{
		if (a == null || b == null) return false;

		var distX = Math.Abs(a.center.X - b.center.X);
		var distY = Math.Abs(a.center.Y - b.center.Y);
		var distZ = Math.Abs(a.center.Z - b.center.Z);

		return distX < a.halfSize.X + b.halfSize.X &&
			distY < a.halfSize.Y + b.halfSize.Y &&
			distZ < a.halfSize.Z + b.halfSize.Z;
	}

	/// <summary>
	/// Checks for an intersection between an oriented bounding box and a half-space.
	/// The half-space is defined by a plane; any point on the negative side of the
	/// plane's normal is considered inside the half-space.
	/// </summary>
	/// <param name="box">The collision box to test.</param>
	/// <param name="plane">The collision plane defining the half-space.</param>
	/// <returns>
	/// True if the box intersects with the half-space (i.e., any part of the box
	/// is on the negative side of the plane). False otherwise.
	/// </returns>
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

	/// <summary>
	/// Checks for an intersection between a sphere and a half-space.
	/// The half-space is defined by a plane; any point on the negative side of the
	/// plane's normal is considered inside the half-space.
	/// </summary>
	/// <param name="sphere">The collision sphere to test.</param>
	/// <param name="plane">The collision plane defining the half-space.</param>
	/// <returns>
	/// True if the sphere intersects with the half-space (i.e., any part of the sphere
	/// is on the negative side of the plane). False otherwise.
	/// </returns>
	public static bool SphereAndHalfSpace(CollisionSphere sphere, CollisionPlane plane)
	{
		var ballDistance = plane.Direction * sphere.GetAxis(3) - sphere.Radius;
		return ballDistance <= plane.Offset;
	}

	/// <summary>
	/// Checks if two spheres are intersecting.
	/// </summary>
	/// <param name="one">The first collision sphere.</param>
	/// <param name="two">The second collision sphere.</param>
	/// <returns>True if the spheres intersect, false otherwise.</returns>
	public static bool SphereAndSphere(CollisionSphere one, CollisionSphere two)
	{
		var midline = one.GetAxis(3) - two.GetAxis(3);
		var radiusSum = one.Radius + two.Radius;
		return midline.SquareMagnitude() < radiusSum * radiusSum;
	}
};