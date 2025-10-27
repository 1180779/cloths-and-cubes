using Engine.Collision;
using Engine.Collision.Bounding_Volume_Hierarchy;
using OpenTK.Windowing.Common;
using Visualisation.Core.GameObjects;
using IntersectionTests = Engine.Collision.IntersectionTests;
using Random = Engine.Random;

namespace Visualization.UiLayer.Applications.Demos;

public class BoxesFallingDemo : RigidBodyApplication
{
    /* The number of boxes in the simulation. */
    private const uint Boxes = 40;

    private Box[] boxes = new Box[Boxes];
    private Plane plane = null!;

    protected override void InitializeScene()
    {
        /* add the cubes to the scene to be rendered */
        for (var i = 0; i < Boxes; i++)
        {
            boxes[i] = new Box();
            var box = boxes[i];
            Scene.AddGameObject(box);
        }

        /* add ground plane to the scene */
        plane = new();
        Scene.AddGameObject(plane);

        /* set everything up */
        Reset();
    }

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

        Dictionary<int, IBoxable> boxDict = new();
        for(int i = 0; i<boxes.Length; i++)
        {
            boxDict[i] = (IBoxable)boxes[i];
        }
        BVH bvh = BVH.Build(boxDict);

        for(int i = 0; i<boxes.Length; i++)
        {
            var box = boxes[i];
            if (!CollisionData.HasMoreContacts()) return;
            CollisionDetector.BoxAndHalfSpace(box.EngineBox, plane.EnginePlane, CollisionData);

            List<(int, int)> potentialCollisions = new();
            BVH.TraverseRecursive(ref potentialCollisions, ref bvh, box.GetBoundingBox(), i, bvh.root);
            foreach (var other in potentialCollisions)
            {
                if (box == boxes[other.Item1]) continue;
                if (!CollisionData.HasMoreContacts()) return;
                CollisionDetector.BoxAndBox(box.EngineBox, boxes[other.Item1].EngineBox, CollisionData);
                if (IntersectionTests.BoxAndBox(box.EngineBox, boxes[other.Item1].EngineBox))
                {
                    box.EngineBox.IsOverlapping = boxes[other.Item1].EngineBox.IsOverlapping = true;
                }
            }
        }

        //// Perform exhaustive collision detection
        //for (var i = 0; i < Boxes; i++)
        //{
        //    var box = boxes[i];
        //    // Check for collisions with the ground plane
        //    if (!CollisionData.HasMoreContacts()) return;
        //    CollisionDetector.BoxAndHalfSpace(box.EngineBox, plane.EnginePlane, CollisionData);

        //    // Check for collisions with each other box
        //    for (var j = i + 1; j < Boxes; j++)
        //    {
        //        var other = boxes[j];
        //        if (!CollisionData.HasMoreContacts()) return;
        //        CollisionDetector.BoxAndBox(box.EngineBox, other.EngineBox, CollisionData);

        //        if (IntersectionTests.BoxAndBox(box.EngineBox, other.EngineBox))
        //        {
        //            box.EngineBox.IsOverlapping = other.EngineBox.IsOverlapping = true;
        //        }
        //    }
        //}
    }

    /// <summary>
    /// Processes the objects in the simulation forward in time.
    /// </summary>
    protected override void UpdateObjects(float duration)
    {
        // Update the physics of each box in turn
        for (var i = 0; i < Boxes; i++)
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

        if (Boxes > 1)
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
        for (var i = 2; i < Boxes; i++)
        {
            boxes[i].EngineBox.Random(random);
        }

        // Reset the contacts
        CollisionData.ContactCount = 0;
    }


    // TODO: REMOVE
    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);

        if (InputProvider.IsKeyPressed(Visualisation.Core.Inputs.InputKey.Q))
        {
            Engine.Vector3 minV = new(-1000, -1000, -1000);
            Engine.Vector3 maxV = new(1000, 1000, 1000);

            for (var i = 0; i < Boxes; i++)
            {
                if (boxes[i].EngineBox.Body.Position.X < minV.X) minV.X = boxes[i].EngineBox.Body.Position.X;
                if (boxes[i].EngineBox.Body.Position.Y < minV.Y) minV.Y = boxes[i].EngineBox.Body.Position.Y;
                if (boxes[i].EngineBox.Body.Position.Z < minV.Z) minV.Z = boxes[i].EngineBox.Body.Position.Z;
                if (boxes[i].EngineBox.Body.Position.X > maxV.X) maxV.X = boxes[i].EngineBox.Body.Position.X;
                if (boxes[i].EngineBox.Body.Position.Y > maxV.Y) maxV.Y = boxes[i].EngineBox.Body.Position.Y;
                if (boxes[i].EngineBox.Body.Position.Z > maxV.Z) maxV.Z = boxes[i].EngineBox.Body.Position.Z;
            }
                
            foreach(var box in boxes)
            {
                Console.WriteLine($"{box.EngineBox.Body.Position}: {MortonCodes.Encode(box.EngineBox.Body.Position, minV, maxV):B}");
            }
        }

        if(InputProvider.IsKeyPressed(Visualisation.Core.Inputs.InputKey.P))
        {
            Console.WriteLine($"Currently at: {this.Scene.CamerasManager.CurrentCamera.Position}");
            Console.WriteLine($"Looking at: {this.Scene.CamerasManager.CurrentCamera.Front}");
        }
    }
}