using System.Diagnostics;

namespace Engine.Collision;

/// <summary>
/// A static utility class that provides a suite of narrow-phase collision detection
/// algorithms for various primitive shapes. It is responsible for determining if
/// two primitives are overlapping and, if so, generating detailed contact
/// information (contact point, normal, and penetration depth).
/// </summary>
public static class CollisionDetector
{
    /// <summary>
    /// A small epsilon value to handle floating-point comparisons and avoid divisions by zero.
    /// </summary>
    private const Real Epsilon = (Real)1e-5;

    public static Real TransformToAxis(CollisionBox box, Vector3 axis)
    {
        return
            box.HalfSize.X * Math.Abs(axis * box.GetAxis(0)) +
            box.HalfSize.Y * Math.Abs(axis * box.GetAxis(1)) +
            box.HalfSize.Z * Math.Abs(axis * box.GetAxis(2));
    }

    /// <summary>
    /// Checks if two oriented bounding boxes overlap along a single given axis.
    /// This is a core part of the Separating Axis Theorem (SAT).
    /// </summary>
    /// <param name="one">The first collision box.</param>
    /// <param name="two">The second collision box.</param>
    /// <param name="axis">The axis to test for overlap. It does not need to be normalized.</param>
    /// <returns>True if the boxes overlap on the given axis, false otherwise.</returns>
    public static bool OverlapOnAxis(
        CollisionBox one,
        CollisionBox two,
        Vector3 axis
    )
    {
        // Guard against degenerate axes (e.g., cross of nearly parallel axes)
        const Real eps = 1e-8f;
        if (axis.SquareMagnitude() < eps)
        {
            // Treat as overlapping on a degenerate axis (skip the test)
            return true;
        }


        // Project the half-size of one onto the axis.
        Real oneProject = TransformToAxis(one, axis);
        Real twoProject = TransformToAxis(two, axis);
        // Find the vector between the two centers.
        Vector3 toCenter = two.GetAxis(3) - one.GetAxis(3);
        // Project this onto the axis.
        Real distance = (Real)Math.Abs(toCenter * axis);
        // Check for overlap.
        return (distance < oneProject + twoProject);
    }

    /// <summary>
    /// Detects and resolves collisions between an oriented bounding box and a half-space plane.
    /// It checks each vertex of the box against the plane and generates a contact for each vertex that has penetrated.
    /// </summary>
    /// <param name="box">The collision box.</param>
    /// <param name="plane">The collision plane defining the half-space.</param>
    /// <param name="data">The collision data object to store generated contacts.</param>
    /// <returns>The number of contacts generated (from 0 to 8).</returns>
    public static uint BoxAndHalfSpace(CollisionBox box, CollisionPlane plane, CollisionData data)
    {
        // Make sure we have contacts
        if (data.ContactsLeft <= 0) return 0;

        // Check for intersection
        if (!IntersectionTests.BoxAndHalfSpace(box, plane))
        {
            return 0;
        }

        // We have an intersection, so find the intersection points. We can make
        // do with only checking vertices. If the box is resting on a plane
        // or on an edge, it will be reported as four or two contact points.

        // Go through each combination of + and - for each half-size
        Real[,] mults =
        {
            { 1, 1, 1 }, { -1, 1, 1 }, { 1, -1, 1 }, { -1, -1, 1 },
            { 1, 1, -1 }, { -1, 1, -1 }, { 1, -1, -1 }, { -1, -1, -1 }
        };

        uint contactsUsed = 0;
        for (uint i = 0; i < 8; i++)
        {
            // Calculate the position of each vertex
            Vector3 vertexPos = new Vector3(mults[i, 0], mults[i, 1], mults[i, 2]);
            vertexPos.ComponentProductUpdate(box.HalfSize);
            vertexPos = box.Transform.Transform(vertexPos);

            // Calculate the distance from the plane
            Real vertexDistance = vertexPos * plane.Direction;

            // Compare this to the plane's distance
            if (vertexDistance <= plane.Offset)
            {
                // Create the contact data.

                // The contact point is halfway between the vertex and the
                // plane - we multiply the direction by half the separation
                // distance and add the vertex location.
                Contact contact = data.ContactList[data.NextContactIndex];
                contact.ContactPoint = plane.Direction;
                contact.ContactPoint *= (vertexDistance - plane.Offset);
                contact.ContactPoint += vertexPos;
                contact.ContactNormal = plane.Direction;
                contact.Penetration = plane.Offset - vertexDistance;

                // Write the appropriate data
                contact.SetBodyData(box.Body, null,
                    data.Friction, data.Restitution);

                // Move onto the next contact
                data.NextContactIndex++;
                contactsUsed++;
                if (contactsUsed == data.ContactsLeft)
                {
                    data.AddContacts(contactsUsed);
                    return contactsUsed;
                }
            }
        }

        data.AddContacts(contactsUsed);
        return contactsUsed;
    }

    public static Real penetrationOnAxis(
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

        // Return the overlap (i.e., positive indicates
        // overlap, negative indicates separation).
        return oneProject + twoProject - distance;
    }

    /// <summary>
    /// A helper for the box-box SAT collision detector. It tests a single potential separating axis.
    /// If the axis separates the boxes, it returns false. Otherwise, it calculates the penetration
    /// and updates the smallest penetration found so far if this one is smaller.
    /// </summary>
    /// <param name="one">The first collision box.</param>
    /// <param name="two">The second collision box.</param>
    /// <param name="axis">The potential separating axis to test.</param>
    /// <param name="toCentre">The vector from the center of box one to the center of box two.</param>
    /// <param name="index">The index of the axis being tested (0-14).</param>
    /// <param name="smallestPenetration">A reference to the smallest penetration found so far.</param>
    /// <param name="smallestCase">A reference to the index of the axis with the smallest penetration.</param>
    /// <returns>False if a separating axis is found, true otherwise.</returns>
    public static bool TryAxis(
        CollisionBox one,
        CollisionBox two,
        Vector3 axis,
        Vector3 toCentre,
        uint index,
        ref Real smallestPenetration,
        ref uint smallestCase)
    {
        var axisCopy = new Vector3(axis);
        // Make sure we have a normalized axis and don't check almost parallel axes
        if (axisCopy.SquareMagnitude() < Epsilon) return true;
        axisCopy.Normalise();

        Real penetration = penetrationOnAxis(one, two, axisCopy, toCentre);

        if (penetration < 0) return false;
        if (penetration < smallestPenetration)
        {
            smallestPenetration = penetration;
            smallestCase = index;
        }

        return true;
    }

    /// <summary>
    /// Detects collisions between two oriented bounding boxes using the Separating Axis Theorem (SAT).
    /// It tests 15 potential separating axes. If no separating axis is found, a collision is confirmed.
    /// It then generates a single contact corresponding to the axis of minimum penetration.
    /// </summary>
    /// <param name="one">The first collision box.</param>
    /// <param name="two">The second collision box.</param>
    /// <param name="data">The collision data object to store the generated contact.</param>
    /// <returns>1 if a contact was generated, 0 otherwise.</returns>
    public static uint BoxAndBox(CollisionBox one, CollisionBox two, CollisionData data)
    {
        // Check for the initial intersection
        if (!IntersectionTests.BoxAndBox(one, two)) return 0;

        // Find the vector between the two centres
        Vector3 toCentre = two.GetAxis(3) - one.GetAxis(3);

        // We start assuming there is no contact
        Real pen = Real.MaxValue;
        uint best = 0xffffff;

        // Now we check each axe, returning if it gives us
        // a separating axis and keeping track of the axis with
        // the smallest penetration otherwise.
        if (!TryAxis(one, two, one.GetAxis(0), toCentre, 0, ref pen, ref best)) return 0;
        if (!TryAxis(one, two, one.GetAxis(1), toCentre, 1, ref pen, ref best)) return 0;
        if (!TryAxis(one, two, one.GetAxis(2), toCentre, 2, ref pen, ref best)) return 0;

        if (!TryAxis(one, two, two.GetAxis(0), toCentre, 3, ref pen, ref best)) return 0;
        if (!TryAxis(one, two, two.GetAxis(1), toCentre, 4, ref pen, ref best)) return 0;
        if (!TryAxis(one, two, two.GetAxis(2), toCentre, 5, ref pen, ref best)) return 0;

        // Store the best axis-major, in case we run into almost
        // parallel edge collisions later
        uint bestSingleAxis = best;

        if (!TryAxis(one, two, one.GetAxis(0) % two.GetAxis(0), toCentre, 6, ref pen, ref best)) return 0;
        if (!TryAxis(one, two, one.GetAxis(0) % two.GetAxis(1), toCentre, 7, ref pen, ref best)) return 0;
        if (!TryAxis(one, two, one.GetAxis(0) % two.GetAxis(2), toCentre, 8, ref pen, ref best)) return 0;
        if (!TryAxis(one, two, one.GetAxis(1) % two.GetAxis(0), toCentre, 9, ref pen, ref best)) return 0;
        if (!TryAxis(one, two, one.GetAxis(1) % two.GetAxis(1), toCentre, 10, ref pen, ref best)) return 0;
        if (!TryAxis(one, two, one.GetAxis(1) % two.GetAxis(2), toCentre, 11, ref pen, ref best)) return 0;
        if (!TryAxis(one, two, one.GetAxis(2) % two.GetAxis(0), toCentre, 12, ref pen, ref best)) return 0;
        if (!TryAxis(one, two, one.GetAxis(2) % two.GetAxis(1), toCentre, 13, ref pen, ref best)) return 0;
        if (!TryAxis(one, two, one.GetAxis(2) % two.GetAxis(2), toCentre, 14, ref pen, ref best)) return 0;

        // Make sure we've got a result
        Debug.Assert(best != 0xffffff);

        // We now know there's a collision, and we know which
        // of the axes gave the smallest penetration. We now
        // can deal with it in different ways depending on
        // the case.
        if (best < 3)
        {
            // We've got a vertex of box two on the face of box one.
            FillPointFaceBoxBox(one, two, toCentre, data, best, pen);
            data.AddContacts(1);
            data.NextContactIndex++;
            return 1;
        }

        if (best < 6)
        {
            // We've got a vertex of box one on the face of box two.
            // We use the same algorithm as above, but swap around
            // one and two (and therefore also the vector between their
            // centres).
            FillPointFaceBoxBox(two, one, toCentre * -1.0f, data, best - 3, pen);
            data.AddContacts(1);
            data.NextContactIndex++;
            return 1;
        }

        // We've got an edge-edge contact. Find out which axes
        best -= 6;
        uint oneAxisIndex = best / 3;
        uint twoAxisIndex = best % 3;
        Vector3 oneAxis = one.GetAxis((int)oneAxisIndex);
        Vector3 twoAxis = two.GetAxis((int)twoAxisIndex);
        Vector3 axis = oneAxis % twoAxis;
        axis.Normalise();

        // The axis should point from box one to box two.
        if (axis * toCentre > 0) axis = axis * -1.0f;

        // We have the axes, but not the edges: each axis has 4 edges parallel
        // to it, we need to find which of the 4 for each object. We do
        // that by finding the point in the center of the edge. We know
        // its component in the direction of the box's collision axis is zero
        // (its mid-point), and we determine which of the extremes in each
        // of the other axes is closest.
        Vector3 ptOnOneEdge = new Vector3(one.HalfSize.X, one.HalfSize.Y, one.HalfSize.Z);
        Vector3 ptOnTwoEdge = new Vector3(two.HalfSize.X, two.HalfSize.Y, two.HalfSize.Z);
        for (var i = 0; i < 3; i++)
        {
            if (i == oneAxisIndex) ptOnOneEdge[i] = 0;
            else if (one.GetAxis(i) * axis > 0) ptOnOneEdge[i] = -ptOnOneEdge[i];

            if (i == twoAxisIndex) ptOnTwoEdge[i] = 0;
            else if (two.GetAxis(i) * axis < 0) ptOnTwoEdge[i] = -ptOnTwoEdge[i];
        }

        // Move them into world coordinates (they are already oriented
        // correctly, since they have been derived from the axes).
        ptOnOneEdge = one.Transform * ptOnOneEdge;
        ptOnTwoEdge = two.Transform * ptOnTwoEdge;

        // So we have a point and a direction for the colliding edges.
        // We need to find out the point of the closest approach of the two
        // line-segments.
        Vector3 vertex = ContactPoint(
            ptOnOneEdge, oneAxis, one.HalfSize[oneAxisIndex],
            ptOnTwoEdge, twoAxis, two.HalfSize[twoAxisIndex],
            bestSingleAxis > 2
        );

        // We can fill the contact.
        Contact contact = data.ContactList[data.NextContactIndex];

        contact.Penetration = pen;
        contact.ContactNormal = axis;
        contact.ContactPoint = vertex;
        contact.SetBodyData(one.Body, two.Body, data.Friction, data.Restitution);
        data.AddContacts(1);
        data.NextContactIndex++;
        return 1;
    }

    /// <summary>
    /// Detects a collision between an oriented bounding box and a single point.
    /// </summary>
    /// <param name="box">The collision box.</param>
    /// <param name="point">The point in world coordinates.</param>
    /// <param name="data">The collision data object to store the generated contact.</param>
    /// <returns>1 if a contact was generated, 0 otherwise.</returns>
    public static uint BoxAndPoint(CollisionBox box, Vector3 point, CollisionData data)
    {
        // Transform the point into box coordinates
        Vector3 relPt = box.Transform.TransformInverse(point);

        // Check each axis, looking for the axis on which the
        // penetration is the least deep.
        Real minDepth = box.HalfSize.X - (Real)Math.Abs(relPt.X);
        if (minDepth < 0) return 0;
        var normal = box.GetAxis(0) * ((relPt.X < 0) ? -1 : 1);

        Real depth = box.HalfSize.Y - (Real)Math.Abs(relPt.Y);
        if (depth < 0) return 0;
        if (depth < minDepth)
        {
            minDepth = depth;
            normal = box.GetAxis(1) * ((relPt.Y < 0) ? -1 : 1);
        }

        depth = box.HalfSize.Z - (Real)Math.Abs(relPt.Z);
        if (depth < 0) return 0;
        if (depth < minDepth)
        {
            minDepth = depth;
            normal = box.GetAxis(2) * ((relPt.Z < 0) ? -1 : 1);
        }

        // Compile the contact
        Contact contact = data.ContactList[data.NextContactIndex];
        contact.ContactNormal = normal;
        contact.ContactPoint = point;
        contact.Penetration = minDepth;

        // Note that we don't know what rigid body the point
        // belongs to, so we just use NULL. Where this is called,
        // this value can be left or filled in.
        contact.SetBodyData(box.Body, null,
            data.Friction, data.Restitution);

        data.AddContacts(1);
        data.NextContactIndex++;
        return 1;
    }

    /// <summary>
    /// A helper method for box-box collisions that generates contact data for a vertex-face collision.
    /// It identifies the colliding vertex on one box and the face on the other and fills the contact details.
    /// </summary>
    /// <param name="one">The box whose face is being collided with.</param>
    /// <param name="two">The box whose vertex is colliding.</param>
    /// <param name="toCentre">The vector from the center of box one to the center of box two.</param>
    /// <param name="data">The collision data object to store the contact.</param>
    /// <param name="best">The index of the axis of collision (a face normal of box one).</param>
    /// <param name="pen">The penetration depth.</param>
    public static void FillPointFaceBoxBox(
        CollisionBox one,
        CollisionBox two,
        Vector3 toCentre,
        CollisionData data,
        uint best,
        Real pen
    )
    {
        // This method is called when we know that a vertex from

        // box two is in contact with box one.
        Contact contact = data.ContactList[data.NextContactIndex];

        // We know which axis the collision is on (i.e., best),
        // but we need to work out which of the two faces on
        // this axis.
        Vector3 normal = one.GetAxis((int)best);
        if (one.GetAxis((int)best) * toCentre > 0)
        {
            normal = normal * -1.0f;
        }

        // Work out which vertex of box two we're colliding with.
        // Using toCentre doesn't work!
        Vector3 vertex = new Vector3(two.HalfSize.X, two.HalfSize.Y, two.HalfSize.Z);
        if (two.GetAxis(0) * normal < 0) vertex.X = -vertex.X;
        if (two.GetAxis(1) * normal < 0) vertex.Y = -vertex.Y;
        if (two.GetAxis(2) * normal < 0) vertex.Z = -vertex.Z;

        // Create the contact data
        contact.ContactNormal = normal;
        contact.Penetration = pen;

        two.CalculateInternals();
        contact.ContactPoint = two.Transform * vertex;
        contact.SetBodyData(one.Body, two.Body, data.Friction, data.Restitution);
    }

    /// <summary>
    /// Calculates the closest point between two line segments in 3D space. This is used for edge-edge collisions.
    /// </summary>
    /// <param name="pOne">A point on the first edge.</param>
    /// <param name="dOne">The direction vector of the first edge.</param>
    /// <param name="oneSize">The half-length of the first edge.</param>
    /// <param name="pTwo">A point on the second edge.</param>
    /// <param name="dTwo">The direction vector of the second edge.</param>
    /// <param name="twoSize">The half-length of the second edge.</param>
    /// <param name="useOne">In the case of a non-crossing (edge-face) contact, this determines which edge's point to return.</param>
    /// <returns>The point of closest approach between the two segments.</returns>
    public static Vector3 ContactPoint(
        Vector3 pOne,
        Vector3 dOne,
        Real oneSize,
        Vector3 pTwo,
        Vector3 dTwo,
        Real twoSize,
        bool useOne)
    {
        Vector3 toSt, cOne, cTwo;
        Real dpStaOne, dpStaTwo, dpOneTwo, smOne, smTwo;
        Real denom, mua, mub;

        smOne = dOne.SquareMagnitude();
        smTwo = dTwo.SquareMagnitude();
        dpOneTwo = dTwo * dOne;

        toSt = pOne - pTwo;
        dpStaOne = dOne * toSt;
        dpStaTwo = dTwo * toSt;

        denom = smOne * smTwo - dpOneTwo * dpOneTwo;

        // Zero denominator indicates parallel lines
        if ((Real)Math.Abs(denom) < (Real)0.0001)
        {
            return useOne ? (Vector3)pOne.Clone() : (Vector3)pTwo.Clone();
        }

        mua = (dpOneTwo * dpStaTwo - smTwo * dpStaOne) / denom;
        mub = (smOne * dpStaTwo - dpOneTwo * dpStaOne) / denom;

        // If either of the edges has the nearest point out
        // of bounds, then the edges aren't crossed; we have
        // an edge-face contact. Our point is on the edge, which
        // we know from the useOne parameter.
        if (mua > oneSize ||
            mua < -oneSize ||
            mub > twoSize ||
            mub < -twoSize)
        {
            return useOne ? (Vector3)pOne.Clone() : (Vector3)pTwo.Clone();
        }

        cOne = pOne + dOne * mua;
        cTwo = pTwo + dTwo * mub;

        return cOne * 0.5f + cTwo * 0.5f;
    }

    /// <summary>
    /// Detects collisions between an oriented bounding box and a sphere.
    /// It works by finding the point on the box closest to the sphere's center and checking the distance.
    /// </summary>
    /// <param name="engineBox">The collision box.</param>
    /// <param name="engineBall">The collision sphere.</param>
    /// <param name="collisionData">The collision data object to store the generated contact.</param>
    /// <returns>True if a contact was generated, false otherwise.</returns>
    public static bool BoxAndSphere(CollisionBox engineBox, CollisionSphere engineBall, CollisionData collisionData)
    {
        Vector3 center = engineBall.GetAxis(3);
        Vector3 relCenter = engineBox.Transform.TransformInverse(center);

        if (Math.Abs(relCenter.X) - engineBox.HalfSize.X > engineBall.Radius ||
            Math.Abs(relCenter.Y) - engineBox.HalfSize.Y > engineBall.Radius ||
            Math.Abs(relCenter.Z) - engineBox.HalfSize.Z > engineBall.Radius)
        {
            return false;
        }

        Vector3 closestPt = new Vector3();
        Real dist = relCenter.X;
        if (dist > engineBox.HalfSize.X) dist = engineBox.HalfSize.X;
        if (dist < -engineBox.HalfSize.X) dist = -engineBox.HalfSize.X;
        closestPt.X = dist;

        dist = relCenter.Y;
        if (dist > engineBox.HalfSize.Y) dist = engineBox.HalfSize.Y;
        if (dist < -engineBox.HalfSize.Y) dist = -engineBox.HalfSize.Y;
        closestPt.Y = dist;

        dist = relCenter.Z;
        if (dist > engineBox.HalfSize.Z) dist = engineBox.HalfSize.Z;
        if (dist < -engineBox.HalfSize.Z) dist = -engineBox.HalfSize.Z;
        closestPt.Z = dist;

        dist = (closestPt - relCenter).SquareMagnitude();
        if (dist > engineBall.Radius * engineBall.Radius) return false;

        Vector3 closestPtWorld = engineBox.Transform.Transform(closestPt);

        Contact contact = collisionData.ContactList[collisionData.NextContactIndex];
        contact.ContactNormal = closestPtWorld - center;
        contact.ContactNormal.Normalise();
        contact.ContactPoint = closestPtWorld;
        contact.Penetration = engineBall.Radius - (Real)Math.Sqrt(dist);
        contact.SetBodyData(engineBox.Body, engineBall.Body, collisionData.Friction, collisionData.Restitution);
        collisionData.AddContacts(1);
        collisionData.NextContactIndex++;

        return true;
    }

    /// <summary>
    /// Detects collisions between two spheres. A collision occurs if the distance
    /// between their centers is less than the sum of their radii.
    /// </summary>
    /// <param name="one">The first collision sphere.</param>
    /// <param name="two">The second collision sphere.</param>
    /// <param name="data">The collision data object to store the generated contact.</param>
    /// <returns>True if a contact was generated, false otherwise.</returns>
    public static bool SphereAndSphere(CollisionSphere one, CollisionSphere two, CollisionData data)
    {
        if (data.ContactsLeft <= 0) return false;

        Vector3 midline = one.GetAxis(3) - two.GetAxis(3);
        Real sizeSq = midline.SquareMagnitude();
        Real radiiSum = one.Radius + two.Radius;

        // Use squared distances for a faster check to avoid the square root.
        if (sizeSq >= radiiSum * radiiSum) return false;

        Real size = (Real)Math.Sqrt(sizeSq);

        // Handle the case where spheres are at the same position to avoid division by zero.
        if (size <= 0.0f) return false;

        Vector3 normal = midline * (1.0f / size);

        Contact contact = data.ContactList[data.NextContactIndex];
        contact.ContactNormal = normal;
        contact.ContactPoint = one.GetAxis(3) + midline * 0.5f;
        contact.Penetration = (one.Radius + two.Radius - size);
        contact.SetBodyData(one.Body, two.Body, data.Friction, data.Restitution);
        data.AddContacts(1);
        data.NextContactIndex++;
        return true;
    }

    /// <summary>
    /// Detects collisions between a sphere and a half-space plane. A collision occurs if the
    /// sphere's surface penetrates the plane.
    /// </summary>
    /// <param name="sphere">The collision sphere.</param>
    /// <param name="plane">The collision plane defining the half-space.</param>
    /// <param name="data">The collision data object to store the generated contact.</param>
    /// <returns>True if a contact was generated, false otherwise.</returns>
    public static bool SphereAndHalfSpace(CollisionSphere sphere, CollisionPlane plane, CollisionData data)
    {
        if (data.ContactsLeft <= 0) return false;

        var position = sphere.GetAxis(3);
        // Find the distance from the sphere's center to the plane
        Real ballDistance = plane.Direction * position - plane.Offset - sphere.Radius;

        if (ballDistance >= 0.0f) return false;
        // Check if we're within radius
        // Create the contact - it has a normal in the plane direction
        // and a contact point in the sphere's center, shifted by
        // the radius in the plane direction.
        Contact contact = data.ContactList[data.NextContactIndex];
        contact.ContactNormal = plane.Direction;
        contact.Penetration = -ballDistance;
        contact.ContactPoint = position - plane.Direction * (ballDistance + sphere.Radius);
        contact.SetBodyData(sphere.Body, null, data.Friction, data.Restitution);
        data.AddContacts(1);
        data.NextContactIndex++;
        return true;
    }
}