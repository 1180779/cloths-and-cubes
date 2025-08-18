namespace Engine.Physics
{
    public class Contact
    {
        // ... Other data as before ...

        /**
         * The contact resolver object needs access into the contacts to
         * set and effect the contact.
         */



        /**
         * Holds the bodies that are involved in the contact. The
         * second of these can be NULL, for contacts with the scenery.
         ensure only two
         */

        public RigidBody?[] body;

        /**
         * Holds the lateral friction coefficient at the contact.
         */
        public float friction;

        /**
         * Holds the normal restitution coefficient at the contact.
         */
        public float restitution;

        /**
         * Holds the position of the contact in world coordinates.
         */
        public Vector3 contactPoint;

        /**
         * Holds the direction of the contact in world coordinates.
         */
        public Vector3 contactNormal;

        /**
         * Holds the depth of penetration at the contact point. If both
         * bodies are specified then the contact point should be midway
         * between the inter-penetrating points.
         */
        public float penetration;

        /**
         * Sets the data that doesn't normally depend on the position
         * of the contact (i.e. the bodies, and their material properties).
         */
        //void setBodyData(RigidBody* one, RigidBody *two,
        //                 float friction, float restitution);



        /**
         * A transform matrix that converts co-ordinates in the contact's
         * frame of reference to world co-ordinates. The columns of this
         * matrix form an orthonormal set of vectors.
         */
        Matrix3 contactToWorld;

        /**
         * Holds the closing velocity at the point of contact. This is set
         * when the calculateInternals function is run.
         */
        Vector3 contactVelocity;

        /**
         * Holds the required change in velocity for this contact to be
         * resolved.
         */
        float desiredDeltaVelocity;

        /**
         * Holds the world space position of the contact point relative to
         * centre of each body. This is set when the calculateInternals
         * function is run.
         Ensure only two elements
         */
        Vector3[] relativeContactPosition;
        public void setBodyData(RigidBody one, RigidBody? two,
                          float friction, float restitution)
        {
            body[0] = one;
            body[1] = two;
            friction = friction;
            restitution = restitution;
        }


        /**
         * Calculates internal data from state data. This is called before
         * the resolution algorithm tries to do any resolution. It should
         * never need to be called manually.
         */
        //void calculateInternals(float duration);

        /**
         * Reverses the contact. This involves swapping the two rigid bodies
         * and reversing the contact normal. The internal values should then
         * be recalculated using calculateInternals (this is not done
         * automatically).
         */
        //void swapBodies();

        /**
         * Updates the awake state of rigid bodies that are taking
         * place in the given contact. A body will be made awake if it
         * is in contact with a body that is awake.
         */
        //void matchAwakeState();

        /**
         * Calculates and sets the internal value for the desired delta
         * velocity.
         */
        //void calculateDesiredDeltaVelocity(float duration);

        /**
         * Calculates and returns the velocity of the contact
         * point on the given body.
         */
        //Vector3 calculateLocalVelocity(uint bodyIndex, float duration);

        /**
         * Calculates an orthonormal basis for the contact point, based on
         * the primary friction direction (for anisotropic friction) or
         * a random orientation (for isotropic friction).
         */
        //void calculateContactBasis();

        /**
         * Applies an impulse to the given body, returning the
         * change in velocities.
         */
        // void applyImpulse(const Vector3 &impulse, RigidBody *body,
        //            Vector3 *velocityChange, Vector3 *rotationChange);

        /**
         * Performs an inertia-weighted impulse based resolution of this
         * contact alone.
         */
        //void applyVelocityChange(Vector3 velocityChange[2],
        //                         Vector3 rotationChange[2]);

        /**
         * Performs an inertia weighted penetration resolution of this
         * contact alone.
         */
        //void applyPositionChange(Vector3 linearChange[2],
        //                         Vector3 angularChange[2],
        //                        float penetration);

        /**
         * Calculates the impulse needed to resolve this contact,
         * given that the contact has no friction. A pair of inertia
         * tensors - one for each contact object - is specified to
         * save calculation time: the calling function has access to
         * these anyway.
         */
        // Vector3 calculateFrictionlessImpulse(Matrix3 *inverseInertiaTensor);

        /**
         * Calculates the impulse needed to resolve this contact,
         * given that the contact has a non-zero coefficient of
         * friction. A pair of inertia tensors - one for each contact
         * object - is specified to save calculation time: the calling
         * function has access to these anyway.
         */
        // Vector3 calculateFrictionImpulse(Matrix3 *inverseInertiaTensor);
    };
}