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

    public Spring(
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
        Real currentLength = force.Magnitude;
        //if (currentLength < (Real)1e-6) return; 
  
        Real magnitude = (currentLength - restLength) * springConstant;
        force.Normalise();
        force *= -magnitude;
        body.AddForce(force);
        other.AddForce(force * -1);
    }
}