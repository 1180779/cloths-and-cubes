using Engine.Collision;
using Visualisation.Core.GameObjects;
using IntersectionTests = Engine.Collision.IntersectionTests;
using Random = Engine.Random;

namespace Visualization.UiLayer.Applications.Demos;

public class BoxesDemo : RigidBodyApplication
{
    protected Box[] boxes = [];
    protected Plane plane = new();

    /// <summary>
    /// Processes the contact generation code.
    /// </summary>
    protected override void GenerateContacts()
    {
        // Note that this method makes a lot of uses of early returns to avoid
        // processing lots of potential contacts that it hasn't got room to
        // store.

        // Set up the collision data structure
        CollisionData.Reset(MaxContacts);
        CollisionData.Friction = (Real)0.9;
        CollisionData.Restitution = (Real)0.6;
        CollisionData.Tolerance = (Real)0.1;

        // Perform exhaustive collision detection
        for (var i = 0; i < boxes.Length; i++)
        {
            var box = boxes[i];
            // Check for collisions with the ground plane
            if (!CollisionData.HasMoreContacts()) return;
            CollisionDetector.BoxAndHalfSpace(box.EngineBox, plane.EnginePlane, CollisionData);

            // Check for collisions with each other box
            for (var j = i + 1; j < boxes.Length; j++)
            {
                var other = boxes[j];
                if (!CollisionData.HasMoreContacts()) return;
                CollisionDetector.BoxAndBox(box.EngineBox, other.EngineBox, CollisionData);

                if (IntersectionTests.BoxAndBox(box.EngineBox, other.EngineBox))
                {
                    box.EngineBox.IsOverlapping = other.EngineBox.IsOverlapping = true;
                }
            }
        }
    }

    /// <summary>
    /// Processes the objects in the simulation forward in time.
    /// </summary>
    protected override void UpdateObjects(float duration)
    {
        // Update the physics of each box in turn
        for (var i = 0; i < boxes.Length; i++)
        {
            var box = boxes[i];

            // Run the physics
            box.EngineBox.Body.Integrate(duration);
            box.EngineBox.CalculateInternals();
            box.EngineBox.IsOverlapping = false;
        }
    }

    /// <summary>
    /// Resets the position of all the boxes and primes the explosion.
    /// </summary>
    protected override void Reset()
    {
        boxes[0].EngineBox.SetState(
            position: new Engine.Vector3(0, 3, 0),
            orientation: new Engine.Quaternion(),
            extents: new Engine.Vector3(4, 1, 1),
            velocity: new Engine.Vector3(0, 0, 0)
        );

        if (boxes.Length > 1)
        {
            boxes[1].EngineBox.SetState(
                position: new Engine.Vector3(0, 4.75f, 2),
                orientation: new Engine.Quaternion(1.0f, 0.1f, 0.05f, 0.01f),
                extents: new Engine.Vector3(1, 1, 4),
                velocity: new Engine.Vector3(0, 0, 0)
            );
        }

        // Create the random objects
        Random random = new();
        for (var i = 2; i < boxes.Length; i++)
        {
            boxes[i].EngineBox.Random(random);
        }

        // Reset the contacts
        CollisionData.ContactCount = 0;
    }
}