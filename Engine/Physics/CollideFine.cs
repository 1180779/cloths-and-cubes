using System.Diagnostics;
namespace Engine.Physics
{


    public struct CollisionData
    {
        /// <summary>
        /// Holds all contacts. Acts like contactArray in C++.
        /// </summary>
        public List<Contact> contactList;

        /// <summary>
        /// Index of the next available contact slot (acts like the contacts pointer in C++).
        /// </summary>
        public int nextContactIndex;

        /// <summary>
        /// Maximum number of contacts allowed.
        /// </summary>
        public int maxContacts;

        /// <summary>
        /// Number of contacts found so far.
        /// </summary>
        public uint contactCount { get; private set; }

        /// <summary>
        /// Friction value to write into any collisions.
        /// </summary>
        public float friction;

        /// <summary>
        /// Restitution value to write into any collisions.
        /// </summary>
        public float restitution;

        /// <summary>
        /// Collision tolerance — even uncolliding objects this close should have collisions generated.
        /// </summary>
        public float tolerance;

        /// <summary>
        /// Checks if there are more contacts available.
        /// </summary>
        public bool HasMoreContacts()
        {
            return (maxContacts - nextContactIndex) > 0;
        }

        /// <summary>
        /// Resets the data so that it has no used contacts recorded.
        /// </summary>
        public void Reset(uint maxContacts1)
        {
            maxContacts = (int)maxContacts1;
            contactCount = 0;
            nextContactIndex = 0;

            if (contactList == null)
                contactList = new List<Contact>(maxContacts);
            else
                contactList.Clear();
        }

        /// <summary>
        /// Notifies the data that the given number of contacts have been added.
        /// </summary>
        public void addContacts(uint count)
        {
            if (count == 0) return;

            // Adjust available space
            nextContactIndex += (int)count;
            contactCount += count;

            // Ensure the list has space for these contacts (dummy placeholders if needed)
            while (contactList.Count < nextContactIndex && contactList.Count < maxContacts)
            {
                contactList.Add(default);
            }
        }
    }
    class CollisionPlane
    {

        /**
         * The plane normal
         */
        public Vector3 direction;

        /**
         * The distance of the plane from the origin.
         */
        public float offset;
    };
    class CollisionPrimitive
    {

        public RigidBody body;
        public Matrix4 offset;
        public Matrix4 transform;
        public Vector3 getAxis(int index)
        {
            return transform.getAxisVector(index);
        }
        public Matrix4 getTransform()
        {
            return transform;
        }



    };
    class CollisionBox : CollisionPrimitive
    {

        public Vector3 halfSize;
    };
    class CollisionDetector
    {

        static float transformToAxis(CollisionBox box, Vector3 axis)
        {
            return box.halfSize.x * MathF.Abs(axis * box.getAxis(0)) + box.halfSize.y * MathF.Abs(axis * box.getAxis(1)) + box.halfSize.z * MathF.Abs(axis * box.getAxis(2));
        }
        bool overlapOnAxis(CollisionBox one, CollisionBox two, Vector3 axis
)
        {
            // Project the half-size of one onto axis.
            float oneProject = transformToAxis(one, axis);
            float twoProject = transformToAxis(two, axis);
            // Find the vector between the two centers.
            Vector3 toCenter = two.getAxis(3) - one.getAxis(3);
            // Project this onto the axis.
            float distance = MathF.Abs(toCenter * axis);
            // Check for overlap.
            return (distance < oneProject + twoProject);
        }

        static uint boxAndHalfSpace(CollisionBox box, CollisionPlane plane, CollisionData data)
        {
            // Make sure we have contacts
            if (data.maxContacts <= 0) return 0;

            // Check for intersection
            if (!IntersectionTests.boxAndHalfSpace(box, plane))
            {
                return 0;
            }

            // We have an intersection, so find the intersection points. We can make
            // do with only checking vertices. If the box is resting on a plane
            // or on an edge, it will be reported as four or two contact points.

            // Go through each combination of + and - for each half-size
            float[][] mults =
          {
    new float[] {  1,  1,  1 },
    new float[] { -1,  1,  1 },
    new float[] {  1, -1,  1 },
    new float[] { -1, -1,  1 },
    new float[] {  1,  1, -1 },
    new float[] { -1,  1, -1 },
    new float[] {  1, -1, -1 },
    new float[] { -1, -1, -1 }
};


            Contact contact = data.contactList[data.nextContactIndex];
            uint contactsUsed = 0;
            for (uint i = 0; i < 8; i++)
            {

                // Calculate the position of each vertex
                Vector3 vertexPos = new Vector3(mults[i][0], mults[i][1], mults[i][2]);
                vertexPos.componentProductUpdate(box.halfSize);
                vertexPos = box.transform.transform(vertexPos);

                // Calculate the distance from the plane
                float vertexDistance = vertexPos * plane.direction;

                // Compare this to the plane's distance
                if (vertexDistance <= plane.offset)
                {
                    // Create the contact data.

                    // The contact point is halfway between the vertex and the
                    // plane - we multiply the direction by half the separation
                    // distance and add the vertex location.
                    contact.contactPoint = plane.direction;
                    contact.contactPoint *= (vertexDistance - plane.offset);
                    contact.contactPoint += vertexPos;
                    contact.contactNormal = plane.direction;
                    contact.penetration = plane.offset - vertexDistance;

                    // Write the appropriate data
                    contact.setBodyData(box.body, null,
                        data.friction, data.restitution);

                    // Move onto the next contact
                    data.nextContactIndex++;
                    contactsUsed++;
                    if (contactsUsed == (uint)data.maxContacts) return contactsUsed;
                }
            }

            data.addContacts(contactsUsed);
            return contactsUsed;
        }
        static float penetrationOnAxis(
   CollisionBox one,
     CollisionBox two,
     Vector3 axis,
    Vector3 toCentre
    )
        {
            // Project the half-size of one onto axis
            float oneProject = transformToAxis(one, axis);
            float twoProject = transformToAxis(two, axis);

            // Project this onto the axis
            float distance = MathF.Abs(toCentre * axis);

            // Return the overlap (i.e. positive indicates
            // overlap, negative indicates separation).
            return oneProject + twoProject - distance;
        }

        static bool tryAxis(CollisionBox one, CollisionBox two, Vector3 axis, Vector3 toCentre, uint index, float smallestPenetration, uint smallestCase)
        {
            // Make sure we have a normalized axis, and don't check almost parallel axes
            if (axis.squareMagnitude() < 0.0001f) return true;
            axis.normalise();

            float penetration = penetrationOnAxis(one, two, axis, toCentre);

            if (penetration < 0) return false;
            if (penetration < smallestPenetration)
            {
                smallestPenetration = penetration;
                smallestCase = index;
            }
            return true;
        }




        static uint boxAndBox(CollisionBox one, CollisionBox two, CollisionData data)
        {
            Vector3 toCentre = two.getAxis(3) - one.getAxis(3);

            // We start assuming there is no contact
            float pen = float.MaxValue;
            uint best = 0xffffff;
            bool CheckOverlap(Vector3 axis, int index)
            {
                if (!CollisionDetector.tryAxis(one, two, axis, toCentre, (uint)index, pen, best))
                    return false;
                return true;
            }
            // Now we check each axes, returning if it gives us
            // a separating axis, and keeping track of the axis with
            // the smallest penetration otherwise.
            if (!CheckOverlap(one.getAxis(0), 0)) return 0;
            if (!CheckOverlap(one.getAxis(1), 1)) return 0;
            if (!CheckOverlap(one.getAxis(2), 2)) return 0;

            if (!CheckOverlap(two.getAxis(0), 3)) return 0;
            if (!CheckOverlap(two.getAxis(1), 4)) return 0;
            if (!CheckOverlap(two.getAxis(2), 5)) return 0;

            // Store best face-axis result
            uint bestSingleAxis = best;

            // Cross-product axes (edge-edge cases)
            if (!CheckOverlap(Vector3.vectorProduct(one.getAxis(0), two.getAxis(0)), 6)) return 0;
            if (!CheckOverlap(Vector3.vectorProduct(one.getAxis(0), two.getAxis(1)), 7)) return 0;
            if (!CheckOverlap(Vector3.vectorProduct(one.getAxis(0), two.getAxis(2)), 8)) return 0;
            if (!CheckOverlap(Vector3.vectorProduct(one.getAxis(1), two.getAxis(0)), 9)) return 0;
            if (!CheckOverlap(Vector3.vectorProduct(one.getAxis(1), two.getAxis(1)), 10)) return 0;
            if (!CheckOverlap(Vector3.vectorProduct(one.getAxis(1), two.getAxis(2)), 11)) return 0;
            if (!CheckOverlap(Vector3.vectorProduct(one.getAxis(2), two.getAxis(0)), 12)) return 0;
            if (!CheckOverlap(Vector3.vectorProduct(one.getAxis(2), two.getAxis(1)), 13)) return 0;
            if (!CheckOverlap(Vector3.vectorProduct(one.getAxis(2), two.getAxis(2)), 14)) return 0;

            // Ensure we have a result
            Debug.Assert(best != 0xFFFFFF);

            // We now know there's a collision, and we know which
            // of the axes gave the smallest penetration. We now
            // can deal with it in different ways depending on
            // the case.
            if (best < 3)
            {
                // We've got a vertex of box two on a face of box one.
                fillPointFaceBoxBox(one, two, toCentre, data, best, pen);
                data.addContacts(1);
                return 1;
            }
            else if (best < 6)
            {
                // We've got a vertex of box one on a face of box two.
                // We use the same algorithm as above, but swap around
                // one and two (and therefore also the vector between their
                // centres).
                fillPointFaceBoxBox(two, one, toCentre * -1.0f, data, best - 3, pen);
                data.addContacts(1);
                return 1;
            }
            else
            {
                // We've got an edge-edge contact. Find out which axes
                best -= 6;
                uint oneAxisIndex = best / 3;
                uint twoAxisIndex = best % 3;
                Vector3 oneAxis = one.getAxis((int)oneAxisIndex);
                Vector3 twoAxis = two.getAxis((int)twoAxisIndex);
                Vector3 axis = oneAxis % twoAxis;
                axis.normalise();

                // The axis should point from box one to box two.
                if (axis * toCentre > 0) axis = axis * -1.0f;

                // We have the axes, but not the edges: each axis has 4 edges parallel
                // to it, we need to find which of the 4 for each object. We do
                // that by finding the point in the centre of the edge. We know
                // its component in the direction of the box's collision axis is zero
                // (its a mid-point) and we determine which of the extremes in each
                // of the other axes is closest.
                Vector3 ptOnOneEdge = one.halfSize;
                Vector3 ptOnTwoEdge = two.halfSize;
                for (uint i = 0; i < 3; i++)
                {
                    if (i == oneAxisIndex) ptOnOneEdge[i] = 0;
                    else if (one.getAxis((int)i) * axis > 0) ptOnOneEdge[i] = -ptOnOneEdge[i];

                    if (i == twoAxisIndex) ptOnTwoEdge[i] = 0;
                    else if (two.getAxis((int)i) * axis < 0) ptOnTwoEdge[i] = -ptOnTwoEdge[i];
                }

                // Move them into world coordinates (they are already oriented
                // correctly, since they have been derived from the axes).
                ptOnOneEdge = one.transform * ptOnOneEdge;
                ptOnTwoEdge = two.transform * ptOnTwoEdge;

                // So we have a point and a direction for the colliding edges.
                // We need to find out point of closest approach of the two
                // line-segments.
                Vector3 vertex = contactPoint(
                    ptOnOneEdge, oneAxis, one.halfSize[oneAxisIndex],
                    ptOnTwoEdge, twoAxis, two.halfSize[twoAxisIndex],
                    bestSingleAxis > 2
                    );

                // We can fill the contact.
                Contact contact = data.contactList[data.nextContactIndex];

                contact.penetration = pen;
                contact.contactNormal = axis;
                contact.contactPoint = vertex;
                contact.setBodyData(one.body, two.body,
                    data.friction, data.restitution);
                data.addContacts(1);
                return 1;
            }
            return 0;
        }
        static uint boxAndPoint(CollisionBox box, Vector3 point, CollisionData data)
        {
            // Transform the point into box coordinates
            Vector3 relPt = box.transform.transformInverse(point);

            Vector3 normal;

            // Check each axis, looking for the axis on which the
            // penetration is least deep.
            float min_depth = box.halfSize.x - MathF.Abs(relPt.x);
            if (min_depth < 0) return 0;
            normal = box.getAxis(0) * ((relPt.x < 0) ? -1 : 1);

            float depth = box.halfSize.y - MathF.Abs(relPt.y);
            if (depth < 0) return 0;
            else if (depth < min_depth)
            {
                min_depth = depth;
                normal = box.getAxis(1) * ((relPt.y < 0) ? -1 : 1);
            }

            depth = box.halfSize.z - MathF.Abs(relPt.z);
            if (depth < 0) return 0;
            else if (depth < min_depth)
            {
                min_depth = depth;
                normal = box.getAxis(2) * ((relPt.z < 0) ? -1 : 1);
            }

            // Compile the contact
            Contact contact = data.contactList[data.nextContactIndex];
            contact.contactNormal = normal;
            contact.contactPoint = point;
            contact.penetration = min_depth;

            // Note that we don't know what rigid body the point
            // belongs to, so we just use NULL. Where this is called
            // this value can be left, or filled in.
            contact.setBodyData(box.body, null,
                data.friction, data.restitution);

            data.addContacts(1);
            return 1;
        }

        static void fillPointFaceBoxBox(
     CollisionBox one,
     CollisionBox two,
    Vector3 toCentre,
    CollisionData data,
    uint best,
    float pen
    )
        {
            // This method is called when we know that a vertex from
            // box two is in contact with box one.

            Contact contact = data.contactList[data.nextContactIndex];

            // We know which axis the collision is on (i.e. best),
            // but we need to work out which of the two faces on
            // this axis.
            Vector3 normal = one.getAxis((int)best);
            if (one.getAxis((int)best) * toCentre > 0)
            {
                normal = normal * -1.0f;
            }

            // Work out which vertex of box two we're colliding with.
            // Using toCentre doesn't work!
            Vector3 vertex = two.halfSize;
            if (two.getAxis(0) * normal < 0) vertex.x = -vertex.x;
            if (two.getAxis(1) * normal < 0) vertex.y = -vertex.y;
            if (two.getAxis(2) * normal < 0) vertex.z = -vertex.z;

            // Create the contact data
            contact.contactNormal = normal;
            contact.penetration = pen;
            contact.contactPoint = two.getTransform() * vertex;
            contact.setBodyData(one.body, two.body,
                data.friction, data.restitution);
        }
        static Vector3 contactPoint(Vector3 pOne, Vector3 dOne, float oneSize, Vector3 pTwo, Vector3 dTwo, float twoSize, bool useOne)
        {
            Vector3 toSt, cOne, cTwo;
            float dpStaOne, dpStaTwo, dpOneTwo, smOne, smTwo;
            float denom, mua, mub;

            smOne = dOne.squareMagnitude();
            smTwo = dTwo.squareMagnitude();
            dpOneTwo = dTwo * dOne;

            toSt = pOne - pTwo;
            dpStaOne = dOne * toSt;
            dpStaTwo = dTwo * toSt;

            denom = smOne * smTwo - dpOneTwo * dpOneTwo;

            // Zero denominator indicates parrallel lines
            if (MathF.Abs(denom) < 0.0001f)
            {
                return useOne ? pOne : pTwo;
            }

            mua = (dpOneTwo * dpStaTwo - smTwo * dpStaOne) / denom;
            mub = (smOne * dpStaTwo - dpOneTwo * dpStaOne) / denom;

            // If either of the edges has the nearest point out
            // of bounds, then the edges aren't crossed, we have
            // an edge-face contact. Our point is on the edge, which
            // we know from the useOne parameter.
            if (mua > oneSize ||
                mua < -oneSize ||
                mub > twoSize ||
                mub < -twoSize)
            {
                return useOne ? pOne : pTwo;
            }
            else
            {
                cOne = pOne + dOne * mua;
                cTwo = pTwo + dTwo * mub;

                return cOne * 0.5f + cTwo * 0.5f;
            }
        }
    }
    class IntersectionTests
    {
        static float transformToAxis(
CollisionBox box,
 Vector3 axis
)
        {
            return
                box.halfSize.x * MathF.Abs(axis * box.getAxis(0)) +
                box.halfSize.y * MathF.Abs(axis * box.getAxis(1)) +
                box.halfSize.z * MathF.Abs(axis * box.getAxis(2));
        }
        static bool overlapOnAxis(
 CollisionBox one,
 CollisionBox two,
 Vector3 axis,
Vector3 toCentre
)
        {
            // Project the half-size of one onto axis
            float oneProject = transformToAxis(one, axis);
            float twoProject = transformToAxis(two, axis);

            // Project this onto the axis
            float distance = MathF.Abs(toCentre * axis);

            // Check for overlap
            return (distance < oneProject + twoProject);
        }

        public static bool boxAndBox(
         CollisionBox one,
         CollisionBox two)
        {

            {
                // Find the vector between the two centres
                Vector3 toCentre = two.getAxis(3) - one.getAxis(3);
                bool TEST_OVERLAP(Vector3 axis)
                {
                    return overlapOnAxis(one, two, axis, toCentre);
                }
                return (
                    // Check on box one's axes first
                    TEST_OVERLAP(one.getAxis(0)) &&
                    TEST_OVERLAP(one.getAxis(1)) &&
                    TEST_OVERLAP(one.getAxis(2)) &&

                    // And on two's
                    TEST_OVERLAP(two.getAxis(0)) &&
                    TEST_OVERLAP(two.getAxis(1)) &&
                    TEST_OVERLAP(two.getAxis(2)) &&

                    // Now on the cross products
                    TEST_OVERLAP(one.getAxis(0) % two.getAxis(0)) &&
                    TEST_OVERLAP(one.getAxis(0) % two.getAxis(1)) &&
                    TEST_OVERLAP(one.getAxis(0) % two.getAxis(2)) &&
                    TEST_OVERLAP(one.getAxis(1) % two.getAxis(0)) &&
                    TEST_OVERLAP(one.getAxis(1) % two.getAxis(1)) &&
                    TEST_OVERLAP(one.getAxis(1) % two.getAxis(2)) &&
                    TEST_OVERLAP(one.getAxis(2) % two.getAxis(0)) &&
                    TEST_OVERLAP(one.getAxis(2) % two.getAxis(1)) &&
                    TEST_OVERLAP(one.getAxis(2) % two.getAxis(2))
                );
            }
        }

        /**
         * Does an intersection test on an arbitrarily aligned box and a
         * half-space.
         *
         * The box is given as a transform matrix, including
         * position, and a vector of half-sizes for the extend of the
         * box along each local axis.
         *
         * The half-space is given as a direction (i.e. unit) vector and the
         * offset of the limiting plane from the origin, along the given
         * direction.
         */
        public static bool boxAndHalfSpace(
             CollisionBox box,
             CollisionPlane plane)
        {
            // Work out the projected radius of the box onto the plane direction
            float projectedRadius = transformToAxis(box, plane.direction);

            // Work out how far the box is from the origin
            float boxDistance =
                plane.direction *
                box.getAxis(3) -
                projectedRadius;

            // Check for the intersection
            return boxDistance <= plane.offset;
        }
    };
}