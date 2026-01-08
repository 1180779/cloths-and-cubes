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

    public Joint(RigidBody body1, Vector3 relativePosition1, RigidBody body2, Vector3 relativePosition2, Real error)
    {
        _bodies[0] = body1;
        _bodies[1] = body2;

        _relativePositions[0] = relativePosition1;
        _relativePositions[1] = relativePosition2;

        _error = error;
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
            return 1; // Added one contact
        }

        return 0; // No contacts added
    }
}