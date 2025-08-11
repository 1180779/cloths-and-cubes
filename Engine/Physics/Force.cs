namespace Engine.Physics
{
    interface iForceGenerator
    {
        public abstract void updateForce(RigidBody body, float duration);

    }
    class Gravity : iForceGenerator
    {
        /** Holds the acceleration due to gravity. */
        Vector3 gravity;
        Gravity(Vector3 g)
        {
            gravity = g;
        }
        public void updateForce(RigidBody body, float duration)
        {
            // Check that we do not have infinite mass
            if (body.inverseMass > 0) return;

            // Apply the mass-scaled force to the body
            body.addForce(gravity * body.getMass());
        }
    }
    class Spring : iForceGenerator
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
        Spring(Vector3 localConnectionPt, RigidBody othe, Vector3 otherConnectionPt, float springConstan, float restLengt)
        {
            restLength = restLengt;
            springConstant = springConstan;
            otherConnectionPoint = otherConnectionPt;
            connectionPoint = localConnectionPt;
            other = othe;
        }
        public void updateForce(RigidBody body, float duration)
        {
            // Calculate the two ends in world space.
            Vector3 lws = body.getPointInWorldSpace(connectionPoint);
            Vector3 ows = other.getPointInWorldSpace(otherConnectionPoint);
            // Calculate the vector of the spring.
            Vector3 force = lws - ows;
            // Calculate the magnitude of the force.
            float magnitude = force.magnitude();
            magnitude = float.Abs(magnitude - restLength);
            magnitude *= springConstant;
            // Calculate the final force and apply it.
            force.normalise();
            force *= -magnitude;
            body.addForceAtPoint(force, lws);
        }
    }
}