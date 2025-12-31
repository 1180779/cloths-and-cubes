using System.Diagnostics;

namespace Engine.RigidBodies;

public class RigidBody
{
    /// <summary>
    /// Holds the inverse of the mass of the rigid body. It is more
    /// useful to hold the inverse mass because integration is simpler,
    /// and because in float time simulation it is more useful to have
    /// bodies with infinite mass (immovable) than zero masses (completely
    /// unstable in numerical simulation).
    /// </summary>
    public Real InverseMass { get; set; }

    /// <summary>
    /// Holds the linear position of the rigid body in world space.
    /// </summary>
    public Vector3 Position = new();


    private Quaternion orientation = new();

    public ref Quaternion OrientationRef => ref orientation;

    /// <summary>
    /// Holds the angular orientation of the rigid body in world space.
    /// </summary>
    public Quaternion Orientation
    {
        get => orientation;
        set
        {
            orientation = value;
            orientation.Normalise();
        }
    }

    /// <summary>
    /// Holds the linear velocity of the rigid body in world space.
    /// </summary>
    public Vector3 Velocity = new();

    /// <summary>
    /// Holds the angular velocity, of rotation, of the rigid body
    /// in world space.
    /// </summary>
    public Vector3 Rotation = new();

    /// <summary>
    /// Holds a transform matrix for converting body space into world
    /// space and vice versa. This can be achieved by calling the
    /// getPointIn*Space functions.
    /// Matrix4 transformMatrix;
    /// </summary>
    public Matrix3 InverseInertiaTensorWorld = new();

    /// <summary>
    /// Holds the amount of motion of the body. This is a recency-
    /// weighted mean that can be used to put a body to sleep.
    /// </summary>
    Real motion;

    /// <summary>
    /// A body can be put to sleep to avoid it being updated
    /// by the integration functions or affected by collisions
    /// with the world.
    /// </summary>
    public bool IsAwake { get; private set; }

    /// <summary>
    /// Some bodies may never be allowed to fall asleep.
    /// User-controlled bodies, for example, should always be
    /// awake.
    /// </summary>
    public bool CanSleep = true;

    /// <summary>
    /// Holds a transform matrix for converting body space into
    /// world space and vice versa. This can be achieved by calling
    /// the getPointIn*Space functions.
    /// </summary>
    public Matrix4 TransformMatrix = new();


    // TODO: remove?
    /**
     * @name Force and Torque Accumulators
     *
     * These data members store the current force, torque and
     * acceleration of the rigid body. Forces can be added to the
     * rigid body in any order, and the class decomposes them into
     * their constituents, accumulating them for the next
     * simulation step. At the simulation step, the accelerations
     * are calculated and stored to be applied to the rigid body.
     */
    /// <summary>
    /// Holds the accumulated force to be applied at the next
    /// integration step.
    /// </summary>
    private Vector3 forceAccum = new();

    /// <summary>
    /// Holds the accumulated torque to be applied at the next
    /// integration step.
    /// </summary>
    Vector3 torqueAccum = new();

    /// <summary>
    /// Holds the acceleration of the rigid body.  This value
    /// can be used to set acceleration due to gravity (its primary
    /// use) or any other constant acceleration.
    /// </summary>
    public Vector3 Acceleration = new();

    /// <summary>
    /// Holds the linear acceleration of the rigid body, for the
    /// previous frame.
    /// </summary>
    public Vector3 LastFrameAcceleration { get; private set; } = new();

    public Real AngularDamping = 0.8f;

    /// <summary>
    /// Holds the amount of damping applied to linear
    /// motion.  Damping is required to remove energy added
    /// through numerical instability in the integrator.
    /// </summary>
    public Real LinearDamping = 0.95f;

    public Matrix3 InverseInertiaTensor { get; set; } = new();

    static void CalculateTransformMatrix(ref Matrix4 transformMatrix, Vector3 position, Quaternion orientation)
    {
        transformMatrix.Data[0] = 1 - 2 * orientation.J * orientation.J -
            2 * orientation.K * orientation.K;
        transformMatrix.Data[1] = 2 * orientation.I * orientation.J -
            2 * orientation.R * orientation.K;
        transformMatrix.Data[2] = 2 * orientation.I * orientation.K +
            2 * orientation.R * orientation.J;
        transformMatrix.Data[3] = position.X;

        transformMatrix.Data[4] = 2 * orientation.I * orientation.J +
            2 * orientation.R * orientation.K;
        transformMatrix.Data[5] = 1 - 2 * orientation.I * orientation.I -
            2 * orientation.K * orientation.K;
        transformMatrix.Data[6] = 2 * orientation.J * orientation.K -
            2 * orientation.R * orientation.I;
        transformMatrix.Data[7] = position.Y;

        transformMatrix.Data[8] = 2 * orientation.I * orientation.K -
            2 * orientation.R * orientation.J;
        transformMatrix.Data[9] = 2 * orientation.J * orientation.K +
            2 * orientation.R * orientation.I;
        transformMatrix.Data[10] = 1 - 2 * orientation.I * orientation.I -
            2 * orientation.J * orientation.J;
        transformMatrix.Data[11] = position.Z;
    }

    public void CalculateDerivedData()
    {
        orientation.Normalise();

        // Calculate the transform matrix for the body.
        CalculateTransformMatrix(ref TransformMatrix, Position, Orientation);

        // Calculate the inertiaTensor in world space.
        TransformInertiaTensor(ref InverseInertiaTensorWorld, Orientation, InverseInertiaTensor, TransformMatrix);
    }

    public void SetInertiaTensor(Matrix3 inertiaTensor)
    {
        InverseInertiaTensor.SetInverse(inertiaTensor);
        // TODO: check what this does by experimenting
        //_checkInverseInertiaTensor(inverseInertiaTensor);
    }

    private static void TransformInertiaTensor(ref Matrix3 iitWorld, Quaternion q, Matrix3 iitBody, Matrix4 rotmat)
    {
        Real t4 = rotmat.Data[0] * iitBody.Data[0] +
            rotmat.Data[1] * iitBody.Data[3] +
            rotmat.Data[2] * iitBody.Data[6];
        Real t9 = rotmat.Data[0] * iitBody.Data[1] +
            rotmat.Data[1] * iitBody.Data[4] +
            rotmat.Data[2] * iitBody.Data[7];
        Real t14 = rotmat.Data[0] * iitBody.Data[2] +
            rotmat.Data[1] * iitBody.Data[5] +
            rotmat.Data[2] * iitBody.Data[8];
        Real t28 = rotmat.Data[4] * iitBody.Data[0] +
            rotmat.Data[5] * iitBody.Data[3] +
            rotmat.Data[6] * iitBody.Data[6];
        Real t33 = rotmat.Data[4] * iitBody.Data[1] +
            rotmat.Data[5] * iitBody.Data[4] +
            rotmat.Data[6] * iitBody.Data[7];
        Real t38 = rotmat.Data[4] * iitBody.Data[2] +
            rotmat.Data[5] * iitBody.Data[5] +
            rotmat.Data[6] * iitBody.Data[8];
        Real t52 = rotmat.Data[8] * iitBody.Data[0] +
            rotmat.Data[9] * iitBody.Data[3] +
            rotmat.Data[10] * iitBody.Data[6];
        Real t57 = rotmat.Data[8] * iitBody.Data[1] +
            rotmat.Data[9] * iitBody.Data[4] +
            rotmat.Data[10] * iitBody.Data[7];
        Real t62 = rotmat.Data[8] * iitBody.Data[2] +
            rotmat.Data[9] * iitBody.Data[5] +
            rotmat.Data[10] * iitBody.Data[8];
        iitWorld.Data[0] = t4 * rotmat.Data[0] +
            t9 * rotmat.Data[1] +
            t14 * rotmat.Data[2];
        iitWorld.Data[1] = t4 * rotmat.Data[4] +
            t9 * rotmat.Data[5] +
            t14 * rotmat.Data[6];
        iitWorld.Data[2] = t4 * rotmat.Data[8] +
            t9 * rotmat.Data[9] +
            t14 * rotmat.Data[10];
        iitWorld.Data[3] = t28 * rotmat.Data[0] +
            t33 * rotmat.Data[1] +
            t38 * rotmat.Data[2];
        iitWorld.Data[4] = t28 * rotmat.Data[4] +
            t33 * rotmat.Data[5] +
            t38 * rotmat.Data[6];
        iitWorld.Data[5] = t28 * rotmat.Data[8] +
            t33 * rotmat.Data[9] +
            t38 * rotmat.Data[10];
        iitWorld.Data[6] = t52 * rotmat.Data[0] +
            t57 * rotmat.Data[1] +
            t62 * rotmat.Data[2];
        iitWorld.Data[7] = t52 * rotmat.Data[4] +
            t57 * rotmat.Data[5] +
            t62 * rotmat.Data[6];
        iitWorld.Data[8] = t52 * rotmat.Data[8] +
            t57 * rotmat.Data[9] +
            t62 * rotmat.Data[10];
    }

    public void AddForce(Vector3 force)
    {
        forceAccum += force;
        SetAwake();
    }

    public void Integrate(Real duration)
    {
        if (!IsAwake) return;

        // Calculate linear acceleration from force inputs.
        LastFrameAcceleration = Acceleration;
        LastFrameAcceleration += forceAccum * InverseMass;

        // Calculate angular acceleration from torque inputs.
        Vector3 angularAcceleration =
            InverseInertiaTensorWorld.Transform(torqueAccum);

        // Adjust velocities
        // Update linear velocity from both acceleration and impulse.
        Velocity += LastFrameAcceleration * duration;

        // Update angular velocity from both acceleration and impulse.
        Rotation += angularAcceleration * duration;

        // Impose drag.
        Velocity *= (Real)Math.Pow(LinearDamping, duration);
        Rotation *= (Real)Math.Pow(AngularDamping, duration);

        // Adjust positions
        // Update linear position.
        Position += Velocity * duration;

        // Update angular position.
        orientation.AddScaledVector(Rotation, duration);
        orientation.Normalise();

        // Normalize the orientation and update the matrices with the new
        // position and orientation
        CalculateDerivedData();

        // Clear accumulators.
        ClearAccumulators();

        // Update the kinetic energy store and possibly put the body to
        // sleep.
        if (CanSleep)
        {
            Real currentMotion = Velocity.SqMagnitude +
                Rotation.SqMagnitude;

            Real bias = (Real)Math.Pow(0.5, (double)duration);
            motion = bias * motion + (1 - bias) * currentMotion;

            var sleepEpsilon = Core.SleepEpsilon;
            if (motion < sleepEpsilon) SetAwake(false);
            else if (motion > 10 * sleepEpsilon) motion = 10 * sleepEpsilon;
        }
    }

    public void ClearAccumulators()
    {
        forceAccum.Clear();
        torqueAccum.Clear();
    }

    private void AddForceAtBodyPoint(Vector3 force, Vector3 point)
    {
        // Convert to coordinates relative to the center of mass.
        Vector3 pt = GetPointInWorldSpace(point);
        AddForceAtPoint(force, pt);
    }

    public Vector3 GetPointInWorldSpace(Vector3 point)
    {
        TransformMatrix.DebugAssertNotNan();
        return TransformMatrix.Transform(point);
    }

    public void AddForceAtPoint(Vector3 force, Vector3 point)
    {
        // Convert to coordinates relative to a center of mass.
        Vector3 pt = point;
        pt -= Position;

        forceAccum += force;
        torqueAccum += pt % force;

        SetAwake();
    }

    public void SetAwake(bool awake = true)
    {
        if (awake)
        {
            IsAwake = true;

            // Add a bit of motion to avoid it falling asleep immediately.
            motion = Core.SleepEpsilon * (Real)2.0;
        }
        else
        {
            IsAwake = false;
            this.Velocity.Clear();
            this.Rotation.Clear();
        }
    }

    public Real Mass
    {
        get
        {
            if (InverseMass == 0)
            {
                return Real.MaxValue;
            }

            return 1 / InverseMass;
        }
        set
        {
            Debug.Assert(value != 0);
            InverseMass = (Real)1.0 / value;
        }
    }
}