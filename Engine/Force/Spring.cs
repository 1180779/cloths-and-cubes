using Engine.RigidBodies;

namespace Engine.Force;

class Spring : IForceGenerator
{
    /** The point of connection of the spring, in local coordinates. */
    Vector3 connectionPoint;

    /**
    * The point of connection of the spring to the other object,
    * in that object’s local coordinates.
*/
    Vector3 otherConnectionPoint;

    /** The particle at the other end of the spring. */
    RigidBody other;

    /** Holds the spring constant. */
    float springConstant;

    /** Holds the rest length of the spring. */
    float restLength;

    Spring(
        Vector3 localConnectionPt,
        RigidBody othe,
        Vector3 otherConnectionPt,
        float springConstan,
        float restLengt)
    {
        restLength = restLengt;
        springConstant = springConstan;
        otherConnectionPoint = otherConnectionPt;
        connectionPoint = localConnectionPt;
        other = othe;
    }

    public void UpdateForce(RigidBody body, Real duration)
    {
        // Calculate the two ends in world space.
        Vector3 lws = body.GetPointInWorldSpace(connectionPoint);
        Vector3 ows = other.GetPointInWorldSpace(otherConnectionPoint);
        // Calculate the vector of the spring.
        Vector3 force = lws - ows;
        // Calculate the magnitude of the force.
        Real magnitude = force.Magnitude;
        magnitude = Real.Abs(magnitude - restLength);
        magnitude *= springConstant;
        // Calculate the final force and apply it.
        force.Normalise();
        force *= -magnitude;
        body.AddForceAtPoint(force, lws);
    }
}