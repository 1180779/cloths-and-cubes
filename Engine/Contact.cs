using System.Diagnostics;

using Engine.RigidBodies;

namespace Engine;

public class Contact
{
    public Contact()
    {
    }

    /**
     * The contact resolver object needs access into the contacts to
     * set and affect the contact.
     */
    /**
     * Holds the bodies that are involved in the contact. The
     * second of these can be NULL, for contacts with the scenery.
     ensure only two
     */
    public readonly RigidBody?[] Body = new RigidBody?[2];

    /// <summary>
    /// Holds the lateral friction coefficient at the contact.
    /// </summary>
    public Real Friction { get; set; }

    /// <summary>
    /// Holds the normal restitution coefficient at the contact.
    /// </summary>
    public Real Restitution { get; set; }

    /// <summary>
    /// Holds the position of the contact in world coordinates.
    /// </summary>
    public Vector3 ContactPoint { get; set; } = new();

    /// <summary>
    /// Holds the direction of the contact in world coordinates.
    /// </summary>
    public Vector3 ContactNormal { get; set; } = new();

    /// <summary>
    /// Holds the depth of penetration at the contact point. If both
    /// bodies are specified, then the contact point should be midway
    /// between the inter-penetrating points.
    /// </summary>
    public Real Penetration { get; set; }

    // /// <summary>
    // /// Sets the data that doesn't normally depend on the position
    // /// of the contact (i.e. the bodies, and their material properties).
    // /// </summary>
    //void setBodyData(RigidBody* one, RigidBody *two,
    //                 float friction, float restitution);


    /// <summary>
    /// A transform matrix that converts co-ordinates in the contact's
    /// frame of reference to world co-ordinates. The columns of this
    /// matrix form an orthonormal set of vectors.
    /// </summary>
    public Matrix3 ContactToWorld { get; set; } = new();

    /// <summary>
    /// Holds the closing velocity at the point of contact. This is set
    /// when the calculateInternals function is run.
    /// </summary>
    public Vector3 ContactVelocity { get; set; } = new();

    /// <summary>
    /// Holds the required change in velocity for this contact to be resolved.
    /// </summary>
    public Real DesiredDeltaVelocity { get; set; }

    /// <summary>
    /// Holds the world space position of the contact point relative to
    /// the center of each body. This is set when the calculateInternals
    /// function is run.
    /// Ensure only two elements
    /// </summary>
    public Vector3[] RelativeContactPosition { get; set; } = [new(), new()];

    public void SetBodyData(
        RigidBody one,
        RigidBody? two,
        Real friction,
        Real restitution)
    {
        Body[0] = one;
        Body[1] = two;
        Friction = friction;
        Restitution = restitution;
    }


    /// <summary>
    /// Calculates internal data from state data. This is called before
    /// the resolution algorithm tries to do any resolution. It should
    /// never need to be called manually.
    /// </summary>
    internal void CalculateInternals(Real duration)
    {
        if (Body[0] == null)
        {
            SwapBodies();
        }

        Debug.Assert(Body[0] != null);

        CalculateContactBasis();
        RelativeContactPosition[0] = ContactPoint - Body[0].Position;
        if (Body[1] != null)
        {
            RelativeContactPosition[1] = ContactPoint - Body[1].Position;
        }

        ContactVelocity = CalculateLocalVelocity(0, duration);
        if (Body[1] != null)
        {
            ContactVelocity -= CalculateLocalVelocity(1, duration);
        }

        CalculateDesiredDeltaVelocity(duration);
    }

    /// <summary>
    /// Reverses the contact. This involves swapping the two rigid bodies
    /// and reversing the contact normal. The internal values should then
    /// be recalculated using calculateInternals (this is not done
    /// automatically).
    /// </summary>
    internal void SwapBodies()
    {
        ContactNormal *= -1;
        (Body[0], Body[1]) = (Body[1], Body[0]);
    }

    /// <summary>
    /// Updates the awake state of rigid bodies that are taking
    /// place in the given contact. A body will be made awake if it
    /// is in contact with a body that is awake.
    /// </summary>
    internal void MatchAwakeState()
    {
        Debug.Assert(Body[0] != null);
        if (Body[1] == null) return;

        bool body0awake = Body[0].IsAwake;
        bool body1awake = Body[1].IsAwake;
        if (body0awake ^ body1awake)
        {
            if (body0awake) Body[1].SetAwake();
            else Body[0].SetAwake();
        }
    }

    /// <summary>
    /// Calculates and sets the internal value for the desired delta velocity.
    /// </summary>
    protected Real VelocityLimit => (Real)0.25;

    internal void CalculateDesiredDeltaVelocity(Real duration)
    {
        // Calculate the acceleration induced velocity accumulated this frame
        Real velocityFromAcc = 0;

        if (Body[0]!.IsAwake)
        {
            velocityFromAcc +=
                Body[0].LastFrameAcceleration * duration * ContactNormal;
        }

        if (Body[1] != null && Body[1].IsAwake)
        {
            velocityFromAcc -=
                Body[1].LastFrameAcceleration * duration * ContactNormal;
        }

        // If the velocity is very slow, limit the restitution
        Real thisRestitution = Restitution;
        if (Math.Abs(ContactVelocity.X) < VelocityLimit)
        {
            thisRestitution = 0;
        }

        // Combine the bounce velocity with the removed
        // acceleration velocity.
        DesiredDeltaVelocity = -ContactVelocity.X - thisRestitution * (ContactVelocity.X - velocityFromAcc);
    }

    /// <summary>
    /// Calculates and returns the velocity of the contact
    /// point on the given body.
    /// </summary>
    internal Vector3 CalculateLocalVelocity(uint bodyIndex, Real duration)
    {
        var thisBody = Body[(int)bodyIndex];

        // Work out the velocity of the contact point.
        Vector3 velocity = thisBody.Rotation % RelativeContactPosition[bodyIndex];
        velocity += thisBody.Velocity;

        // Turn the velocity into contact-coordinates.
        Vector3 contactVel = ContactToWorld.Transpose * velocity;

        // Calculate the ammount of velocity that is due to forces without
        // reactions.
        Vector3 accVelocity = thisBody.LastFrameAcceleration * duration;

        // Calculate the velocity in contact-coordinates.
        accVelocity = ContactToWorld.Transpose * accVelocity;

        // We ignore any component of acceleration in the contact normal
        // direction, we are only interested in planar acceleration
        accVelocity.X = 0;

        // Add the planar velocities - if there's enough friction they will
        // be removed during velocity resolution
        contactVel += accVelocity;

        // And return it
        return contactVel;
    }

    /// <summary>
    /// Calculates an orthonormal basis for the contact point, based on
    /// the primary friction direction (for anisotropic friction) or
    /// a random orientation (for isotropic friction).
    /// </summary>
    internal void CalculateContactBasis()
    {
        Vector3[] contactTangent = [new(), new()];

        // Check whether the Z-axis is nearer to the X or Y axis
        if (Math.Abs(ContactNormal.X) > Math.Abs(ContactNormal.Y))
        {
            // Scaling factor to ensure the results are normalised
            Real s = (Real)1.0 / Real.Sqrt(ContactNormal.Z * ContactNormal.Z +
                ContactNormal.X * ContactNormal.X);

            // The new X-axis is at right angles to the world Y-axis
            contactTangent[0].X = ContactNormal.Z * s;
            contactTangent[0].Y = 0;
            contactTangent[0].Z = -ContactNormal.X * s;

            // The new Y-axis is at right angles to the new X- and Z- axes
            contactTangent[1].X = ContactNormal.Y * contactTangent[0].Z;
            contactTangent[1].Y = ContactNormal.Z * contactTangent[0].X -
                ContactNormal.X * contactTangent[0].Z;
            contactTangent[1].Z = -ContactNormal.Y * contactTangent[0].X;
        }
        else
        {
            // Scaling factor to ensure the results are normalised
            Real s = (Real)1.0 / Real.Sqrt(ContactNormal.Z * ContactNormal.Z +
                ContactNormal.Y * ContactNormal.Y);

            // The new X-axis is at right angles to the world X-axis
            contactTangent[0].X = 0;
            contactTangent[0].Y = -ContactNormal.Z * s;
            contactTangent[0].Z = ContactNormal.Y * s;

            // The new Y-axis is at right angles to the new X- and Z- axes
            contactTangent[1].X = ContactNormal.Y * contactTangent[0].Z -
                ContactNormal.Z * contactTangent[0].Y;
            contactTangent[1].Y = -ContactNormal.X * contactTangent[0].Z;
            contactTangent[1].Z = ContactNormal.X * contactTangent[0].Y;
        }

        // Make a matrix from the three vectors.
        ContactToWorld.SetComponents(
            ContactNormal,
            contactTangent[0],
            contactTangent[1]);
    }

    // /// <summary>
    // /// Applies an impulse to the given body, returning the
    // /// change in velocities.
    // /// </summary>
    // internal void ApplyImpulse(Vector3 impulse, RigidBody body, Vector3 velocityChange, Vector3 rotationChange)
    // {
    // }

    /// <summary>
    /// Performs an inertia-weighted impulse-based resolution of this
    /// contact alone.
    /// </summary>
    internal void ApplyVelocityChange(Vector3[] velocityChange, Vector3[] rotationChange)
    {
        // Get hold of the inverse mass and inverse inertia tensor, both in world coordinates.
        Matrix3[] inverseInertiaTensor = new Matrix3[2];
        inverseInertiaTensor[0] = (Matrix3)Body[0].InverseInertiaTensorWorld.Clone();
        if (Body[1] != null)
        {
            inverseInertiaTensor[1] = (Matrix3)Body[1].InverseInertiaTensorWorld.Clone();
        }

        // We will calculate the impulse for each contact axis
        Vector3 impulseContact;

        if (Friction == (Real)0.0)
        {
            // Use the short format for frictionless contacts
            impulseContact = CalculateFrictionlessImpulse(inverseInertiaTensor);
        }
        else
        {
            // Otherwise we may have impulses that aren't in the direction of the
            // contact, so we need the more complex version.
            impulseContact = CalculateFrictionImpulse(inverseInertiaTensor);
        }

        // Convert impulse to world coordinates 
        Vector3 impulse = ContactToWorld.Transform(impulseContact);

        // Split in the impulse into linear and rotational components
        Vector3 impTorque = RelativeContactPosition[0] % impulse;
        rotationChange[0] = inverseInertiaTensor[0].Transform(impTorque);
        velocityChange[0].Clear();
        velocityChange[0].AddScaledVector(impulse, Body[0].InverseMass);

        // Apply the changes
        Body[0].Velocity += velocityChange[0];
        Body[0].Rotation += rotationChange[0];

        if (Body[1] != null)
        {
            // Work out body one's linear and angular changes
            impTorque = impulse % RelativeContactPosition[1];
            rotationChange[1] = inverseInertiaTensor[1].Transform(impTorque);
            velocityChange[1].Clear();
            velocityChange[1].AddScaledVector(impulse, -Body[1].InverseMass);

            // And apply them
            Body[1].Velocity += velocityChange[1];
            Body[1].Rotation += rotationChange[1];
        }
    }

    /// <summary>
    /// Performs an inertia-weighted penetration resolution of this
    /// contact alone.
    /// </summary>
    public void ApplyPositionChange(Vector3[] linearChange, Vector3[] angularChange, Real penetration)
    {
        const Real angularLimit = (Real)0.2f;
        Real[] angularMove = new Real[2];
        Real[] linearMove = new Real[2];

        Real totalInertia = 0;
        Real[] linearInertia = new Real[2];
        Real[] angularInertia = new Real[2];

        // We need to work out the inertia of each object in the direction
        // of the contact normal, due to angular inertia only.
        for (uint i = 0; i < 2; i++)
        {
            if (Body[i] != null)
            {
                // Skip bodies with infinite mass (pinned/anchored bodies)
                // They have inverse mass = 0 and zero inertia tensor, which would cause division by zero
                if (Body[i].InverseMass == 0)
                {
                    linearInertia[i] = 0;
                    angularInertia[i] = 0;
                    continue;
                }

                Matrix3 inverseInertiaTensor = (Matrix3)Body[i].InverseInertiaTensorWorld.Clone();

                // Use the same procedure as for calculating frictionless
                // velocity change to work out the angular inertia.
                Vector3 angularInertiaWorld = RelativeContactPosition[i] % ContactNormal;
                angularInertiaWorld = inverseInertiaTensor.Transform(angularInertiaWorld);
                angularInertiaWorld = angularInertiaWorld % RelativeContactPosition[i];
                angularInertia[i] = angularInertiaWorld * ContactNormal;

                // The linear component is simply the inverse mass
                linearInertia[i] = Body[i].InverseMass;

                // Keep track of the total inertia from all components
                totalInertia += linearInertia[i] + angularInertia[i];
            }
        }

        if (totalInertia <= 0)
        {
            // Both bodies have infinite mass, so no resolution is possible.
            return;
        }

        // Loop through again calculating and applying the changes
        for (uint i = 0; i < 2; i++)
        {
            if (Body[i] != null)
            {
                // Skip bodies with infinite mass (they shouldn't move)
                if (Body[i].InverseMass == 0)
                {
                    angularMove[i] = 0;
                    linearMove[i] = 0;
                    angularChange[i].Clear();
                    linearChange[i].Clear();
                    continue;
                }

                // // Handle case where totalInertia is zero (both bodies are immovable)
                // if (totalInertia == 0)
                // {
                //     angularMove[i] = 0;
                //     linearMove[i] = 0;
                //     angularChange[i].Clear();
                //     linearChange[i].Clear();
                //     continue;
                // }

                // The linear and angular movements required are in proportion to
                // the two inverse inertias.
                Real sign = (i == 0) ? 1 : -1;
                angularMove[i] = sign * penetration * (angularInertia[i] / totalInertia);
                Debug.Assert(!float.IsNaN(angularMove[i]) && !float.IsInfinity(angularMove[i]));
                linearMove[i] = sign * penetration * (linearInertia[i] / totalInertia);
                Debug.Assert(!float.IsNaN(linearMove[i]) && !float.IsInfinity(linearMove[i]));

                // To avoid angular projections that are too great (when mass is large
                // but inertia tensor is small) limit the angular move.
                Vector3 projection = RelativeContactPosition[i];
                projection.AddScaledVector(
                    ContactNormal,
                    -RelativeContactPosition[i].ScalarProduct(ContactNormal)
                );

                // Use the small angle approximation for the sine of the angle
                Real maxMagnitude = angularLimit * projection.Magnitude;

                if (angularMove[i] < -maxMagnitude)
                {
                    Real totalMove = angularMove[i] + linearMove[i];
                    angularMove[i] = -maxMagnitude;
                    linearMove[i] = totalMove - angularMove[i];
                    Debug.Assert(!float.IsNaN(linearMove[i]) && !float.IsInfinity(linearMove[i]));
                }
                else if (angularMove[i] > maxMagnitude)
                {
                    Real totalMove = angularMove[i] + linearMove[i];
                    angularMove[i] = maxMagnitude;
                    linearMove[i] = totalMove - angularMove[i];
                    Debug.Assert(!float.IsNaN(linearMove[i]) && !float.IsInfinity(linearMove[i]));
                }

                // We have the linear amount of movement required by turning
                // the rigid body (in angularMove[i]). We now need to
                // calculate the desired rotation to achieve that.
                if (angularMove[i] == 0)
                {
                    // Easy case - no angular movement means no rotation.
                    angularChange[i].Clear();
                }
                else
                {
                    // Work out the direction we'd like to rotate in.
                    Vector3 targetAngularDirection = RelativeContactPosition[i].VectorProduct(ContactNormal);
                    targetAngularDirection.DebugAssertNotNan();

                    Matrix3 inverseInertiaTensor = (Matrix3)Body[i].InverseInertiaTensorWorld.Clone();

                    // Work out the direction we'd need to rotate to achieve that
                    angularChange[i] = inverseInertiaTensor.Transform(targetAngularDirection) *
                        (angularMove[i] / angularInertia[i]);
                }

                // Velocity change is easier - it is just the linear movement
                // along the contact normal.
                linearChange[i] = ContactNormal * linearMove[i];
                linearChange[i].DebugAssertNotNan();

                // Now we can start to apply the values we've calculated.
                // Apply the linear movement
                Body[i].Position.DebugAssertNotNan();
                Vector3 pos = Body[i].Position;
                pos.AddScaledVector(ContactNormal, linearMove[i]);
                Body[i].Position = pos;
                Body[i].Position.DebugAssertNotNan();

                // And the change in orientation
                Body[i].Orientation.DebugAssertNotNan();
                Quaternion q = Body[i].Orientation;
                q.AddScaledVector(angularChange[i], (Real)1.0);
                Body[i].Orientation = q;
                Body[i].Orientation.DebugAssertNotNan();

                // We need to calculate the derived data for any body that is
                // asleep, so that the changes are reflected in the object's
                // data. Otherwise the resolution will not change the position
                // of the object, and the next collision detection round will
                // have the same penetration.
                if (!Body[i].IsAwake) Body[i].CalculateDerivedData();
            }
        }
    }

    /**
     * Calculates the impulse needed to resolve this contact,
     * given that the contact has no friction. A pair of inertia
     * tensors - one for each contact object - is specified to
     * save calculation time: the calling function has access to
     * these anyway.
     */
    internal Vector3 CalculateFrictionlessImpulse(Matrix3[] inverseInertiaTensor)
    {
        Vector3 impulseContact = new();

        // Build a vector that shows the change in velocity in
        // world space for a unit impulse in the direction of the contact
        // normal.
        Vector3 deltaVelWorld = RelativeContactPosition[0] % ContactNormal;
        deltaVelWorld = inverseInertiaTensor[0].Transform(deltaVelWorld);
        deltaVelWorld = deltaVelWorld % RelativeContactPosition[0];

        // Work out the change in velocity in contact coordinates.
        Real deltaVelocity = deltaVelWorld * ContactNormal;

        // Add the linear component of velocity change 
        deltaVelocity += Body[0].InverseMass;

        // Check if we need the second body's data
        if (Body[1] != null)
        {
            // Go through the same transformation sequence again
            deltaVelWorld = RelativeContactPosition[1] % ContactNormal;
            deltaVelWorld = inverseInertiaTensor[1].Transform(deltaVelWorld);
            deltaVelWorld = deltaVelWorld % RelativeContactPosition[1];

            // Add the change in velocity due to rotation
            deltaVelocity += deltaVelWorld * ContactNormal;

            // Add the change in velocity due to linear motion
            deltaVelocity += Body[1].InverseMass;
        }

        // Calculate the required size of the impulse
        impulseContact.X = DesiredDeltaVelocity / deltaVelocity;
        impulseContact.Y = 0;
        impulseContact.Z = 0;
        return impulseContact;
    }

    /// <summary>
    /// Calculates the impulse needed to resolve this contact,
    /// given that the contact has a non-zero coefficient of
    /// friction. A pair of inertia tensors - one for each contact
    /// object - is specified to save calculation time: the calling
    /// function has access to these anyway.
    /// </summary>
    internal Vector3 CalculateFrictionImpulse(Matrix3[] inverseInertiaTensor)
    {
        Vector3 impulseContact;
        Real inverseMass = Body[0]!.InverseMass;

        // The equivalent of a cross product in matrices is multiplication
        // by a skew symmetric matrix - we build the matrix for converting
        // between linear and angular quantities.
        Matrix3 impulseToTorque = new();
        impulseToTorque.SetSkewSymmetric(RelativeContactPosition[0]);

        // Build the matrix to convert contact impulse to change in velocity
        // in world coordinates.
        Matrix3 deltaVelWorld = impulseToTorque;
        deltaVelWorld *= inverseInertiaTensor[0];
        deltaVelWorld *= impulseToTorque;
        deltaVelWorld *= -1;

        // Check if we need to add body two's data
        if (Body[1] != null)
        {
            // Set the cross product matrix
            impulseToTorque.SetSkewSymmetric(RelativeContactPosition[1]);

            // Calculate the velocity change matrix
            Matrix3 deltaVelWorld2 = impulseToTorque;
            deltaVelWorld2 *= inverseInertiaTensor[1];
            deltaVelWorld2 *= impulseToTorque;
            deltaVelWorld2 *= -1;

            // Add to the total delta velocity.
            deltaVelWorld += deltaVelWorld2;

            // Add to the inverse mass
            inverseMass += Body[1].InverseMass;
        }

        // Do a change of basis to convert into contact coordinates.
        Matrix3 deltaVelocity = ContactToWorld.Transpose;
        deltaVelocity *= deltaVelWorld;
        deltaVelocity *= ContactToWorld;

        // Add in the linear velocity change
        deltaVelocity[0] += inverseMass;
        deltaVelocity[4] += inverseMass;
        deltaVelocity[8] += inverseMass;

        // Invert to get the impulse needed per unit velocity
        Matrix3 impulseMatrix = deltaVelocity.Inverse;

        // Find the target velocities to kill
        Vector3 velKill = new(DesiredDeltaVelocity,
            -ContactVelocity.Y,
            -ContactVelocity.Z);

        // Find the impulse to kill target velocities
        impulseContact = impulseMatrix.Transform(velKill);

        // Check for exceeding friction
        Real planarImpulse = Real.Sqrt(
            impulseContact.Y * impulseContact.Y +
            impulseContact.Z * impulseContact.Z
        );

        if (planarImpulse > impulseContact.X * Friction)
        {
            // We need to use dynamic friction
            impulseContact.Y /= planarImpulse;
            impulseContact.Z /= planarImpulse;

            impulseContact.X = deltaVelocity[0] +
                deltaVelocity[1] * Friction * impulseContact.Y +
                deltaVelocity[2] * Friction * impulseContact.Z;
            impulseContact.X = DesiredDeltaVelocity / impulseContact.X;
            impulseContact.Y *= Friction * impulseContact.X;
            impulseContact.Z *= Friction * impulseContact.X;
        }

        return impulseContact;
    }
};