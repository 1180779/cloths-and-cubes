using Engine.Collision;
using Engine.Collision.Bounding_Volume_Hierarchy;

using ImGuiNET;

using Visualisation.Core;
using Visualisation.Core.GameObjects;

using Random = Engine.Random;

namespace Visualization.UiLayer.Applications.Demos;

public class BoxesDemo : RigidBodyApplication
{
    protected Box[] Boxes = [];
    protected Ball[] Balls = [];
    protected Plane Plane;

    private bool[] bvhLevelsToRender = Enumerable.Repeat(false, 10).ToArray();

    private readonly Vector3[] levelColors =
    [
        new(1.0f, 0.0f, 0.0f), // Red
        new(0.0f, 1.0f, 0.0f), // Green
        new(0.0f, 0.0f, 1.0f), // Blue
        new(1.0f, 1.0f, 0.0f), // Yellow
        new(0.0f, 1.0f, 1.0f), // Cyan
        new(1.0f, 0.0f, 1.0f) // Magenta
    ];

    protected BoxesDemo()
    {
        Plane = new();
    }

    protected override void RenderWindows(double dt)
    {
        base.RenderWindows(dt);
        ImGui.Begin("Bvh Nodes to render");

        if (ImGui.Button("Select All"))
        {
            bvhLevelsToRender = Enumerable.Repeat(true, bvhLevelsToRender.Length).ToArray();
        }

        ImGui.SameLine();
        if (ImGui.Button("Deselect All"))
        {
            bvhLevelsToRender = Enumerable.Repeat(false, bvhLevelsToRender.Length).ToArray();
        }

        for (var i = 0; i < bvhLevelsToRender.Length; i++)
        {
            var color = levelColors[i % levelColors.Length];
            ImGui.PushStyleColor(ImGuiCol.Text,
                new System.Numerics.Vector4(new System.Numerics.Vector3(color.X, color.Y, color.Z), 1.0f));
            ImGui.Checkbox($"Level {i}", ref bvhLevelsToRender[i]);
            ImGui.PopStyleColor();
        }

        ImGui.End();
    }

    protected override void DebugRenderInScene(Shader sh)
    {
        base.DebugRenderInScene(sh);
        Dictionary<int, IBoxable> boxDict = new();
        for (int i = 0; i < Boxes.Length + Balls.Length; i++)
        {
            boxDict[i] = i < Boxes.Length ? Boxes[i] : Balls[i - Boxes.Length];
        }

        BVH bvh = BVH.Build(boxDict);

        BvhWireframe bvhWireframe = new(bvh)
        {
            LevelColors = levelColors,
            LevelsToRender = bvhLevelsToRender
                .Select((enabled, index) => new { enabled, index })
                .Where(x => x.enabled)
                .Select(x => x.index)
                .ToArray()
        };
        bvhWireframe.Render(sh);
    }

    /// <summary>
    /// Processes the contact generation code.
    /// </summary>
    protected override void GenerateContacts()
    {
        // Set up the collision data structure
        CollisionData.Reset(MaxContacts);
        CollisionData.Friction = (Real)0.9;
        CollisionData.Restitution = (Real)0.6;
        CollisionData.Tolerance = (Real)0.1;

        Dictionary<int, IBoxable> boxDict = new();
        for (int i = 0; i < Boxes.Length + Balls.Length; i++)
        {
            boxDict[i] = i < Boxes.Length ? Boxes[i] : Balls[i - Boxes.Length];
        }

        BVH bvh = BVH.Build(boxDict);

        // Process box-plane collisions
        foreach (var box in Boxes)
        {
            if (!CollisionData.HasMoreContacts()) return;
            CollisionDetector.BoxAndHalfSpace(box.EngineBox, Plane.EnginePlane, CollisionData);
        }

        // Process sphere-plane collisions
        foreach (var ball in Balls)
        {
            if (!CollisionData.HasMoreContacts()) return;
            CollisionDetector.SphereAndHalfSpace(ball.EngineBall, Plane.EnginePlane, CollisionData);
        }

        List<(int, int)> potentialCollisions = new();
        BVH.GetPotentialContacts(ref potentialCollisions, bvh.root);

        foreach (var pair in potentialCollisions)
        {
            if (!CollisionData.HasMoreContacts()) return;

            // TODO: refactor this check
            if (pair.Item1 < Boxes.Length)
            {
                if (pair.Item2 < Boxes.Length)
                {
                    var box1 = Boxes[pair.Item1];
                    var box2 = Boxes[pair.Item2];

                    var contacts = CollisionDetector.BoxAndBox(box1.EngineBox, box2.EngineBox, CollisionData);
                    if (contacts > 0)
                    {
                        box1.EngineBox.IsOverlapping = box2.EngineBox.IsOverlapping = true;
                    }
                }
                else
                {
                    var box1 = Boxes[pair.Item1];
                    var ball2 = Balls[pair.Item2 - Boxes.Length];

                    var contacts = CollisionDetector.BoxAndSphere(box1.EngineBox, ball2.EngineBall, CollisionData);
                    if (contacts)
                    {
                        box1.EngineBox.IsOverlapping = ball2.EngineBall.IsOverlapping = true;
                    }
                }
            }
            else
            {
                if (pair.Item2 < Boxes.Length)
                {
                    var ball1 = Balls[pair.Item1 - Boxes.Length];
                    var box2 = Boxes[pair.Item2];

                    var contacts = CollisionDetector.BoxAndSphere(box2.EngineBox, ball1.EngineBall, CollisionData);
                    if (contacts)
                    {
                        box2.EngineBox.IsOverlapping = ball1.EngineBall.IsOverlapping = true;
                    }
                }
                else
                {
                    var ball1 = Balls[pair.Item1 - Boxes.Length];
                    var ball2 = Balls[pair.Item2 - Boxes.Length];

                    var contacts = CollisionDetector.SphereAndSphere(ball1.EngineBall, ball2.EngineBall, CollisionData);
                    if (contacts)
                    {
                        ball1.EngineBall.IsOverlapping = ball2.EngineBall.IsOverlapping = true;
                    }
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
        foreach (var box in Boxes)
        {
            box.EngineBox.Body.Integrate(duration);
            box.EngineBox.CalculateInternals();
            box.EngineBox.IsOverlapping = false;
        }

        foreach (var ball in Balls)
        {
            ball.EngineBall.Body.Integrate(duration);
            ball.EngineBall.CalculateInternals();
            ball.EngineBall.IsOverlapping = false;
        }
    }

    /// <summary>
    /// Resets the position of all the boxes and primes the explosion.
    /// </summary>
    protected override void Reset()
    {
        // reset boxes; some in preconfigured positions
        if (Boxes.Length > 0)
        {
            Boxes[0].EngineBox.SetState(
                position: new Engine.Vector3(0, 3, 0),
                orientation: new Engine.Quaternion(),
                extents: new Engine.Vector3(4, 1, 1),
                velocity: new Engine.Vector3(0, 0, 0)
            );
        }

        if (Boxes.Length > 1)
        {
            Boxes[1].EngineBox.SetState(
                position: new Engine.Vector3(0, 4.75f, 2),
                orientation: new Engine.Quaternion(1.0f, 0.1f, 0.05f, 0.01f),
                extents: new Engine.Vector3(1, 1, 4),
                velocity: new Engine.Vector3(0, 0, 0)
            );
        }

        Random random = new();
        for (var i = 2; i < Boxes.Length; i++)
        {
            Boxes[i].EngineBox.Random(random);
        }

        // reset spheres; some in preconfigured positions
        if (Balls.Length > 0)
        {
            Balls[0].EngineBall.SetState(
                position: new Engine.Vector3(0, 10, 0),
                orientation: new Engine.Quaternion(),
                radius: 1.0f,
                velocity: new Engine.Vector3(0, 0, 0));
        }

        if (Balls.Length > 1)
        {
            Balls[1].EngineBall.SetState(
                position: new Engine.Vector3(3, 4.75f, 2),
                orientation: new Engine.Quaternion(),
                radius: 1.0f,
                velocity: new Engine.Vector3(0, 0, 0));
        }

        for (var i = 2; i < Balls.Length; i++)
        {
            Balls[i].EngineBall.Random(random);
        }

        // Reset the contacts
        CollisionData.ContactCount = 0;
    }
}