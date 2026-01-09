using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Engine;
using Engine.Collision.Bounding_Volume_Hierarchy;
using Engine.RigidBodies;

namespace EngineTests
{
    public class BroadPhaseTests
    {
        private static Engine.RigidBodies.Box InitializeBox()
        {
            Box box = new Box();
            // Create a cube centered on (0,0,0) with side length 1
            box.HalfSize = new Vector3(0.5f, 0.5f, 0.5f);
            box.Body.Position = new Vector3(0, 0, 0);

            // Set awake
            box.Body.SetAwake();

            // No rotation, no velocity
            box.Body.Velocity = new Vector3(0, 0, 0);
            box.Body.Acceleration = new Vector3(0, 0, 0);
            box.Body.Orientation = new Quaternion(0, 0, 0, 1);
            box.Body.Rotation = new Vector3(0, 0, 0);

            // Mass of 1, linear damping coefficient of 1 for ease of calculations
            box.Body.InverseMass = 1.0f;
            box.Body.LinearDamping = 1.0f;
            box.Body.AngularDamping = 1.0f;
            return box;
        }

        [Test]
        public static void BroadPhase_DetectsIntersectingBoxes()
        {
            // Initialize both boxes
            var boxA = InitializeBox();
            var boxB = InitializeBox();
            boxA.Body.Position = new Vector3(-1.5f, 0, 0);
            boxB.Body.Position = new Vector3(1.5f, 0, 0);
            boxA.Body.Velocity = new Vector3(1, 0, 0);
            boxB.Body.Velocity = new Vector3(-1, 0, 0);
            boxA.Body.Integrate(1.1f);
            boxB.Body.Integrate(1.1f);

            Dictionary<int, IBoxable> bodies = new()
            {
                { 0, boxA },
                { 1, boxB }
            };
            BVH bvh = BVH.Build(bodies);
            List<(int, int)> potentialContacts = [];
            BVH.GetPotentialContacts(ref potentialContacts, bvh.root);
            Assert.That(potentialContacts.Count, Is.EqualTo(1));
            Assert.That(
                (potentialContacts[0].Item1 == 0 && potentialContacts[0].Item2 == 1) ||
                (potentialContacts[0].Item1 == 1 && potentialContacts[0].Item2 == 0)
            );
        }
    }
}
