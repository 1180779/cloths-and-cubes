using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Engine;
using Engine.Collision;
using Engine.RigidBodies;

namespace EngineTests
{
    public class ContactTests
    {
        private static Box InitializeBox()
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
        public static void TwoBoxes_Intersecting_Detected()
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

           
            Assert.That(Engine.Collision.IntersectionTests.BoxAndBox(boxA, boxB), Is.EqualTo(true));
        }

        [Test]
        public static void TwoBoxes_Disjoint_NotCounted()
        {
            var boxA = InitializeBox();
            var boxB = InitializeBox();
            boxA.Body.Position = new Vector3(-5, 0, 0);
            boxB.Body.Position = new Vector3(5, 0, 0);
            boxA.Body.Velocity = new Vector3(1, 0, 0);
            boxB.Body.Velocity = new Vector3(-1, 0, 0);
            boxA.Body.Integrate(1.1f);
            boxB.Body.Integrate(1.1f);

            Assert.That(Engine.Collision.IntersectionTests.BoxAndBox(boxA, boxB), Is.EqualTo(false));
        }

        [Test]
        public static void TwoBoxes_Contact_Generated()
        {
            var boxA = InitializeBox();
            var boxB = InitializeBox();
            boxA.Body.Position = new Vector3(-1.5f, 0, 0);
            boxB.Body.Position = new Vector3(1.5f, 0, 0);
            boxA.Body.Velocity = new Vector3(1, 0, 0);
            boxB.Body.Velocity = new Vector3(-1, 0, 0);

            boxA.Body.Integrate(1.1f);
            boxB.Body.Integrate(1.1f);

            CollisionData data = new();
            data.ContactsLeft = 2;
            data.ContactList = new Contact[2];
            data.ContactList[0] = new Contact();
            data.ContactList[1] = new Contact();

            CollisionDetector.BoxAndBox(boxA, boxB, data);

            Assert.That(data.ContactCount, Is.EqualTo(1));
        }

        [Test]
        public static void ThreeBoxes_ThreeContacts_Generated()
        {
            var boxA = InitializeBox();
            var boxB = InitializeBox();
            var boxC = InitializeBox();

            boxA.Body.Position = new Vector3(-1.5f, 0, 0);
            boxB.Body.Position = new Vector3(1.5f, 0, 0);
            boxC.Body.Position = new Vector3(0, 1.5f, 0);

            boxA.Body.Velocity = new Vector3(1, 0, 0);
            boxB.Body.Velocity = new Vector3(-1, 0, 0);
            boxC.Body.Velocity = new Vector3(0, -1, 0);

            boxA.Body.Integrate(1.1f);
            boxB.Body.Integrate(1.1f);
            boxC.Body.Integrate(1.1f);

            CollisionData data = new();
            data.ContactsLeft = 3;
            data.ContactList = new Contact[3];
            data.ContactList[0] = new Contact();
            data.ContactList[1] = new Contact();
            data.ContactList[2] = new Contact();
            CollisionDetector.BoxAndBox(boxA, boxB, data);
            CollisionDetector.BoxAndBox(boxA, boxC, data);
            CollisionDetector.BoxAndBox(boxB, boxC, data);
            Assert.That(data.ContactCount, Is.EqualTo(3));

        }

        [Test]
        public static void TwoBoxes_Contact_Resolved()
        {
            var boxA = InitializeBox();
            var boxB = InitializeBox();
            boxA.Body.Position = new Vector3(-1.5f, 0, 0);
            boxB.Body.Position = new Vector3(0.0f, 0, 0);
            boxA.Body.Velocity = new Vector3(1f, 0, 0);
            boxA.Body.Mass = 10.0f;
            boxA.Body.Integrate(1.1f);
            boxB.Body.Integrate(1.1f);
            CollisionData data = new();
            data.ContactsLeft = 2;
            data.ContactList = new Contact[2];
            data.ContactList[0] = new Contact();
            data.ContactList[1] = new Contact();
            CollisionDetector.BoxAndBox(boxA, boxB, data);
            Assert.That(data.ContactCount, Is.EqualTo(1));
            ContactResolver resolver = new ContactResolver(10, 10, 0.01f, 0.01f);
            resolver.ResolveContacts(
                data.ContactList,
                data.ContactCount,
                0.016f
            );
            // After resolution, the boxes should be moving away from each other
            Assert.That(boxA.Body.Velocity.X, Is.LessThan(1));
            Assert.That(boxB.Body.Velocity.X, Is.GreaterThan(0));
        }

        [Test]
        public static void TwoBoxes_Momentum_Kept()
        {
            var boxA = InitializeBox();
            var boxB = InitializeBox();
            boxA.Body.Position = new Vector3(-1.5f, 0, 0);
            boxB.Body.Position = new Vector3(0.0f, 0, 0);
            boxA.Body.Mass = 15.0f;
            boxB.Body.Mass = 5.0f;
            boxA.Body.Velocity = new Vector3(1.0f, 0, 0);
            boxA.Body.Integrate(1.1f);
            boxB.Body.Integrate(1.1f);
            // Initial momentum of the system p = m*v = 15 * 1 = 15
            var momentum = boxA.Body.Mass * boxA.Body.Velocity.X + boxB.Body.Mass * boxB.Body.Velocity.X;
            CollisionData data = new();
            data.ContactsLeft = 2;
            data.ContactList = new Contact[2];
            data.ContactList[0] = new Contact();
            data.ContactList[1] = new Contact();
            CollisionDetector.BoxAndBox(boxA, boxB, data);
            Assert.That(data.ContactCount, Is.EqualTo(1));
            ContactResolver resolver = new ContactResolver(10, 10, 0.01f, 0.01f);
            resolver.ResolveContacts(
                data.ContactList,
                data.ContactCount,
                0.016f
            );
            var momentumAfter = boxA.Body.Mass * boxA.Body.Velocity.X + boxB.Body.Mass * boxB.Body.Velocity.X;
            Assert.That(momentumAfter, Is.EqualTo(momentum).Within(0.1f));
        }
    }
}
