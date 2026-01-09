using Engine.Collision;
using Engine.RigidBodies;

namespace Engine.ContactGenerators;

/// <summary>
/// Joints link together two rigid bodies and make sure they do not
/// separate.  In a general physics engine there may be many
/// different types of joint that reduce the number of relative
/// degrees of freedom between two objects. This joint is a common
/// position joint: each object has a location (given in
/// body-coordinates) that will be kept at the same point in the
/// simulation.
/// </summary>
public sealed class Joint : IContactGenerator
{
    /// <summary>
    /// The bodies connected by the joint. 
    /// </summary>
    private readonly RigidBody[] _bodies = new RigidBody[2];

    /// <summary>
    /// The trackable objects (collision primitives/wrappers) that contain the bodies.
    /// These are used to update joint indices after swap-and-pop operations.
    /// </summary>
    private readonly IJointTrackable?[] _trackables = new IJointTrackable?[2];

    /// <summary>
    /// Relative positions of the connection for each body, in local coordinates. 
    /// </summary>
    private readonly Vector3[] _relativePositions = new Vector3[2];

    /// <summary>
    /// The maximum displacement at the joint before the
    /// joint is considered to be violated. This is normally a
    /// small, epsilon value.  It can be larger, however in which
    /// case the joint will behave as if an inelastic cable joined
    /// the bodies at their joint locations.
    /// </summary>
    private Real _error;

    public Joint(
        CollisionPrimitive primitive1,
        Vector3 relativePosition1,
        CollisionPrimitive primitive2,
        Vector3 relativePosition2,
        Real error = 0.01f)
    {
        _trackables[0] = primitive1 as IJointTrackable;
        _trackables[1] = primitive2 as IJointTrackable;

        _bodies[0] = primitive1.Body;
        _bodies[1] = primitive2.Body;

        _relativePositions[0] = relativePosition1;
        _relativePositions[1] = relativePosition2;

        _error = error;

        // Trackables will be null by default, can be set later if needed
    }

    /// <summary>
    /// Removes this joint from the trackable objects that are associated with its connected bodies.
    /// This ensures that the joint is no longer tracked by the associated collision primitives.
    /// </summary>
    public void RemoveFromTrackables()
    {
        _trackables[0]?.RemoveConnectedJoint(this);
        _trackables[1]?.RemoveConnectedJoint(this);
    }

    /// <summary>
    /// Updates the joint index in all trackable objects connected to this joint.
    /// Called after a swap-and-pop operation changes this joint's position in the global list.
    /// </summary>
    /// <param name="newIndex">The new index of this joint in the global list.</param>
    public void UpdateIndicesInTrackables(int newIndex)
    {
        _trackables[0]?.UpdateJointIndex(this, newIndex);
        _trackables[1]?.UpdateJointIndex(this, newIndex);
    }

    public uint AddContacts(CollisionData data)
    {
        var body1PosWorld = _bodies[0].GetPointInWorldSpace(_relativePositions[0]);
        var body2PosWorld = _bodies[1].GetPointInWorldSpace(_relativePositions[1]);

        // calculate the length of the joint
        Vector3 body1To2 = body2PosWorld - body1PosWorld;
        var normal = body1To2.Normalized();
        var length = body1To2.Magnitude;

        if (length > _error)
        {
            // The constraint is violated; add contact
            Contact contact = data.ContactList[data.NextContactIndex];
            contact.Body[0] = _bodies[0];
            contact.Body[1] = _bodies[1];
            contact.ContactNormal = normal;
            contact.ContactPoint = (body1PosWorld + body2PosWorld) * 0.5f;
            contact.Penetration = length - _error;
            contact.Friction = 1.0f;
            contact.Restitution = 0.0f;
            data.NextContactIndex++;
            data.AddContacts(1);
            return 1; // Added one contact
        }

        return 0; // No contacts added
    }

    /// <summary>
    /// Removes a specific joint connection from a given object, if applicable.
    /// </summary>
    /// <param name="obj">
    /// The object from which the joint connection should be removed. This can
    /// either implement the <see cref="IBodyWithSingleJoint"/> or <see cref="IBodyWithJoints"/>
    /// interface.
    /// </param>
    /// <param name="joint">
    /// The joint to be removed from the specified object.
    /// </param>
    public static void RemoveJointConnection(object obj, Joint joint)
    {
        if (obj is IBodyWithSingleJoint bodyWithJoint)
        {
            if (bodyWithJoint.ConnectedJoint.IsSet &&
                bodyWithJoint.ConnectedJoint.Joint == joint)
            {
                bodyWithJoint.ConnectedJoint = new();
            }

            return;
        }

        if (obj is IBodyWithJoints bodyWithJoints)
        {
            bodyWithJoints.RemoveConnectedJoint(joint);
        }
    }
}