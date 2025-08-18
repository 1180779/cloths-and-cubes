
namespace Engine.Physics
{
        public class RigidBody
        {
                /**
        * Holds the inverse of the mass of the rigid body. It is more
        * useful to hold the inverse mass because integration is simpler,
        * and because in float time simulation it is more useful to have
        * bodies with infinite mass (immovable) than zero mass (completely
        * unstable in numerical simulation).
*/
                public float inverseMass;
                /**
                * Holds the linear position of the rigid body in world space.
*/
                public Vector3 position;
                /**
                * Holds the angular orientation of the rigid body in world space.
*/
                public Quaternion orientation;
                /**
                * Holds the linear velocity of the rigid body in world space.
*/
                public Vector3 velocity;
                /**
                * Holds the angular velocity, or rotation, or the rigid body
                * in world space.
*/
                public Vector3 rotation;
                /**
                * Holds a transform matrix for converting body space into world
                * space and vice versa. This can be achieved by calling the
                * getPointIn*Space functions.
Matrix4 transformMatrix; 
*/
                Matrix3 inverseInertiaTensorWorld;

                /**
                 * Holds the amount of motion of the body. This is a recency
                 * weighted mean that can be used to put a body to sleap.
                 */
                float motion;

                /**
                 * A body can be put to sleep to avoid it being updated
                 * by the integration functions or affected by collisions
                 * with the world.
                 */
                bool isAwake;

                /**
                 * Some bodies may never be allowed to fall asleep.
                 * User controlled bodies, for example, should be
                 * always awake.
                 */
                bool canSleep;

                /**
                 * Holds a transform matrix for converting body space into
                 * world space and vice versa. This can be achieved by calling
                 * the getPointIn*Space functions.
                 *
                 * @see getPointInLocalSpace
                 * @see getPointInWorldSpace
                 * @see getTransform
                 */
                Matrix4 transformMatrix;

                /*@}*/


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
                /*@{*/

                /**
                 * Holds the accumulated force to be applied at the next
                 * integration step.
                 */
                Vector3 forceAccum;

                /**
                 * Holds the accumulated torque to be applied at the next
                 * integration step.
                 */
                Vector3 torqueAccum;

                /**
                  * Holds the acceleration of the rigid body.  This value
                  * can be used to set acceleration due to gravity (its primary
                  * use), or any other constant acceleration.
                  */
                Vector3 acceleration;

                /**
                 * Holds the linear acceleration of the rigid body, for the
                 * previous frame.
                 */
                Vector3 lastFrameAcceleration;
                float angularDamping;
                /**
        * Holds the amount of damping applied to linear
        * motion.  Damping is required to remove energy added
        * through numerical instability in the integrator.
        */
                float linearDamping;

                /*@}*/
                public Matrix3 inverseInertiaTensor;
                static void _calculateTransformMatrix(ref Matrix4 transformMatrix, Vector3 position, Quaternion orientation)
                {
                        transformMatrix.data[0] = 1 - 2 * orientation.j * orientation.j * orientation.k * orientation.k;
                        transformMatrix.data[1] = 2 * orientation.i * orientation.j -
                        2 * orientation.r * orientation.k;
                        transformMatrix.data[2] = 2 * orientation.i * orientation.k +
                        2 * orientation.r * orientation.j;
                        transformMatrix.data[3] = position.x;
                        transformMatrix.data[4] = 2 * orientation.i * orientation.j +
                        2 * orientation.r * orientation.k;
                        transformMatrix.data[5] = 1 - 2 * orientation.i * orientation.i * orientation.k * orientation.k;
                        transformMatrix.data[6] = 2 * orientation.j * orientation.k -
                        2 * orientation.r * orientation.i;
                        transformMatrix.data[7] = position.y;
                        transformMatrix.data[8] = 2 * orientation.i * orientation.k -
                        2 * orientation.r * orientation.j;
                        transformMatrix.data[9] = 2 * orientation.j * orientation.k +
                        2 * orientation.r * orientation.i;
                        transformMatrix.data[10] = 1 - 2 * orientation.i * orientation.i * orientation.j * orientation.j;
                        transformMatrix.data[11] = position.z;
                }
                void calculateDerivedData()
                {
                        // Calculate the transform matrix for the body.
                        _calculateTransformMatrix(ref transformMatrix, position, orientation);
                        _transformInertiaTensor(inverseInertiaTensorWorld, orientation, inverseInertiaTensor, transformMatrix);
                }
                void setInertiaTensor(Matrix3 inertiaTensor)
                {
                        inverseInertiaTensor.setInverse(inertiaTensor);
                        //_checkInverseInertiaTensor(inverseInertiaTensor);
                        //sprawdzić jesli cos jebnie nie wiem co mialoby to robic dokladnie

                }
                static void _transformInertiaTensor(Matrix3 iitWorld, Quaternion q, Matrix3 iitBody, Matrix4 rotmat)
                {
                        float t4 = rotmat.data[0] * iitBody.data[0] +
                        rotmat.data[1] * iitBody.data[3] +
                        rotmat.data[2] * iitBody.data[6];
                        float t9 = rotmat.data[0] * iitBody.data[1] +
                        rotmat.data[1] * iitBody.data[4] +
                        rotmat.data[2] * iitBody.data[7];
                        float t14 = rotmat.data[0] * iitBody.data[2] +
                        rotmat.data[1] * iitBody.data[5] +
                        rotmat.data[2] * iitBody.data[8];
                        float t28 = rotmat.data[4] * iitBody.data[0] +
                        rotmat.data[5] * iitBody.data[3] +
                        rotmat.data[6] * iitBody.data[6];
                        float t33 = rotmat.data[4] * iitBody.data[1] +
                        rotmat.data[5] * iitBody.data[4] +
                        rotmat.data[6] * iitBody.data[7];
                        float t38 = rotmat.data[4] * iitBody.data[2] +
                        rotmat.data[5] * iitBody.data[5] +
                        rotmat.data[6] * iitBody.data[8];
                        float t52 = rotmat.data[8] * iitBody.data[0] +
                        rotmat.data[9] * iitBody.data[3] +
                        rotmat.data[10] * iitBody.data[6];
                        float t57 = rotmat.data[8] * iitBody.data[1] +
                        rotmat.data[9] * iitBody.data[4] +
                        rotmat.data[10] * iitBody.data[7];
                        float t62 = rotmat.data[8] * iitBody.data[2] +
                        rotmat.data[9] * iitBody.data[5] +
                        rotmat.data[10] * iitBody.data[8];
                        iitWorld.data[0] = t4 * rotmat.data[0] +
                        t9 * rotmat.data[1] +
                        t14 * rotmat.data[2];
                        iitWorld.data[1] = t4 * rotmat.data[4] +
                        t9 * rotmat.data[5] +
                        t14 * rotmat.data[6];
                        iitWorld.data[2] = t4 * rotmat.data[8] +
                        t9 * rotmat.data[9] +
                        t14 * rotmat.data[10];
                        iitWorld.data[3] = t28 * rotmat.data[0] +
                        t33 * rotmat.data[1] +
                        t38 * rotmat.data[2];
                        iitWorld.data[4] = t28 * rotmat.data[4] +
                        t33 * rotmat.data[5] +
                        t38 * rotmat.data[6];
                        iitWorld.data[5] = t28 * rotmat.data[8] +
                        t33 * rotmat.data[9] +
                        t38 * rotmat.data[10];
                        iitWorld.data[6] = t52 * rotmat.data[0] +
                        t57 * rotmat.data[1] +
                        t62 * rotmat.data[2];
                        iitWorld.data[7] = t52 * rotmat.data[4] +
                        t57 * rotmat.data[5] +
                        t62 * rotmat.data[6];
                        iitWorld.data[8] = t52 * rotmat.data[8] +
                        t57 * rotmat.data[9] +
                        t62 * rotmat.data[10];
                }
                public void addForce(Vector3 force)
                {
                        forceAccum += force;
                }
                void integrate(float duration)
                {
                        // Calculate linear acceleration from force inputs.
                        lastFrameAcceleration = acceleration;
                        lastFrameAcceleration.addScaledVector(forceAccum, inverseMass);
                        // Calculate angular acceleration from torque inputs.
                        Vector3 angularAcceleration =
                        inverseInertiaTensorWorld.transform(torqueAccum);
                        // Adjust velocities
                        // Update linear velocity from both acceleration and impulse.
                        velocity.addScaledVector(lastFrameAcceleration, duration);
                        // Update angular velocity from both acceleration and impulse.
                        rotation.addScaledVector(angularAcceleration, duration);
                        // Impose drag.
                        velocity *= MathF.Pow(linearDamping, duration);
                        rotation *= MathF.Pow(angularDamping, duration);
                        // Adjust positions
                        // Update linear position.
                        position.addScaledVector(velocity, duration);
                        // Update angular position.
                        orientation.addScaledVector(rotation, duration);
                        // Impose drag.
                        velocity *= MathF.Pow(linearDamping, duration);
                        rotation *= MathF.Pow(angularDamping, duration);
                        // Normalize the orientation, and update the matrices with the new
                        // position and orientation.
                        calculateDerivedData();
                        // Clear accumulators.
                        clearAccumulators();
                }
                void clearAccumulators()
                {
                        forceAccum.clear();
                        torqueAccum.clear();
                }
                void addForceAtBodyPoint(Vector3 force, Vector3 point)
                {
                        // Convert to coordinates relative to the center of mass.
                        Vector3 pt = getPointInWorldSpace(point);
                        addForceAtPoint(force, pt);
                }
                public Vector3 getPointInWorldSpace(Vector3 point)
                {
                        return transformMatrix.transform(point);
                }
                public void addForceAtPoint(Vector3 force, Vector3 point)
                {
                        // Convert to coordinates relative to center of mass.
                        Vector3 pt = point;
                        pt -= position;

                        forceAccum += force;
                        torqueAccum += pt % force;

                        isAwake = true;
                }

                internal float getMass()
                {
                        if (inverseMass == 0)
                        {
                                return float.MaxValue;
                        }
                        else return 1 / inverseMass;
                }
        }
}