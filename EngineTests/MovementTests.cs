using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Engine;
using Engine.RigidBodies;
using Engine.World;

namespace EngineTests
{
    public class MovementTests
    {
        private static Box InitializeBox()
        {
            Box box = new Box();
            box.HalfSize = new Vector3(0.5f, 0.5f, 0.5f);
            box.Body.Position = new Vector3(0, 0, 0);
            box.Body.SetAwake();
            box.Body.Velocity = new Vector3(0, 0, 0);
            box.Body.Acceleration = new Vector3(0, 0, 0);
            box.Body.InverseMass = 1.0f;
            box.Body.LinearDamping = 1.0f;
            return box;
        }

        [Test]
        public void SingleBox_Move_NoObstacles_MovesToTarget()
        {
            Box box = InitializeBox();
            box.Body.Velocity = new Vector3(1, 0, 0); // Move along x-axis at 1 unit/sec
            box.Body.Integrate(1.0f); // Simulate for 1 second
            var pos = box.Body.Position;

            Assert.That(pos.X, Is.EqualTo(1.0f).Within(0.0001f));
        }

        [Test]
        public void LinearDamping_AppliedOverTime_VelocityReduces()
        {
            Box box = InitializeBox();
            box.Body.Velocity = new Vector3(10, 0, 0); // Initial velocity
            box.Body.LinearDamping = 0.9f; // 10% velocity loss per second
            // Simulate for 3 seconds
            box.Body.Integrate(3.0f);
            // After 1 second, velocity should be reduced by damping factor
            Assert.That(box.Body.Velocity.X, Is.EqualTo(7.29f).Within(0.0001f));
        }

        [Test]
        public void Force_AppliedOverTime_VelocityIncreases()
        {
            Box box = InitializeBox();
            box.Body.Velocity = new Vector3(10, 0, 0); // Initial velocity
            box.Body.AddForce(new Vector3(5, 0, 0)); // Apply force of 5 units per second squared along the x-axis
            box.Body.Integrate(1.0f); // Simulate for 1 second
            Assert.That(box.Body.Velocity.X, Is.EqualTo(15.0f).Within(0.0001f));
        }
    }
}
