using Engine.Collision;
using Engine.Collision.Bounding_Volume_Hierarchy;
using Engine.Force;

using ImGuiNET;

using Visualisation.Core;
using Visualisation.Core.GameObjects;

using Random = Engine.Random;

namespace Visualization.UiLayer.Applications.Demos;

public class BoxesDemo : RigidBodyApplication
{
    protected Box[] _boxes = [];
    protected Ball[] _balls = [];
    protected Plane _plane;

    protected ForceRegistry _forceRegistry = new();
    protected Cloth _cloth;

    private bool[] _bvhLevelsToRender = Enumerable.Repeat(false, 10).ToArray();

    private readonly Vector3[] _levelColors =
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
        _cloth = new Cloth(_forceRegistry);
        _plane = new();
    }

    protected override void RenderWindows(double dt)
    {
        base.RenderWindows(dt);
        ImGui.Begin("Bvh Nodes to render");

        if (ImGui.Button("Select All"))
        {
            _bvhLevelsToRender = Enumerable.Repeat(true, _bvhLevelsToRender.Length).ToArray();
        }

        ImGui.SameLine();
        if (ImGui.Button("Deselect All"))
        {
            _bvhLevelsToRender = Enumerable.Repeat(false, _bvhLevelsToRender.Length).ToArray();
        }

        for (var i = 0; i < _bvhLevelsToRender.Length; i++)
        {
            var color = _levelColors[i % _levelColors.Length];
            ImGui.PushStyleColor(ImGuiCol.Text,
                new System.Numerics.Vector4(new System.Numerics.Vector3(color.X, color.Y, color.Z), 1.0f));
            ImGui.Checkbox($"Level {i}", ref _bvhLevelsToRender[i]);
            ImGui.PopStyleColor();
        }

        ImGui.End();
    }

    protected override void DebugRenderInScene(Shader sh)
    {
        base.DebugRenderInScene(sh);
        Dictionary<int, IBoxable> boxDict = new();
        for (int i = 0; i < _boxes.Length + _balls.Length; i++)
        {
            boxDict[i] = i < _boxes.Length ? _boxes[i] : _balls[i - _boxes.Length];
        }

        BVH bvh = BVH.Build(boxDict);

        BvhWireframe bvhWireframe = new(bvh)
        {
            LevelColors = _levelColors,
            LevelsToRender = _bvhLevelsToRender
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
        _collisionData.Reset(MaxContacts);
        _collisionData.Friction = (Real)0.9;
        _collisionData.Restitution = (Real)0.6;
        _collisionData.Tolerance = (Real)0.1;

        Dictionary<int, IBoxable> boxDict = new();
        for (int i = 0; i < _boxes.Length + _balls.Length; i++)
        {
            boxDict[i] = i < _boxes.Length ? _boxes[i] : _balls[i - _boxes.Length];
        }

        BVH bvh = BVH.Build(boxDict);

        // Process box-plane collisions
        foreach (var box in _boxes)
        {
            if (!_collisionData.HasMoreContacts()) return;
            CollisionDetector.BoxAndHalfSpace(box.EngineBox, _plane.EnginePlane, _collisionData);
        }

        // Process sphere-plane collisions
        foreach (var ball in _balls)
        {
            if (!_collisionData.HasMoreContacts()) return;
            CollisionDetector.SphereAndHalfSpace(ball.EngineBall, _plane.EnginePlane, _collisionData);
        }

        List<(int, int)> potentialCollisions = new();
        BVH.GetPotentialContacts(ref potentialCollisions, bvh.root);

        foreach (var pair in potentialCollisions)
        {
            if (!_collisionData.HasMoreContacts()) return;

            // TODO: refactor this check
            if (pair.Item1 < _boxes.Length)
            {
                if (pair.Item2 < _boxes.Length)
                {
                    var box1 = _boxes[pair.Item1];
                    var box2 = _boxes[pair.Item2];

                    var contacts = CollisionDetector.BoxAndBox(box1.EngineBox, box2.EngineBox, _collisionData);
                    if (contacts > 0)
                    {
                        box1.EngineBox.IsOverlapping = box2.EngineBox.IsOverlapping = true;
                    }
                }
                else
                {
                    var box1 = _boxes[pair.Item1];
                    var ball2 = _balls[pair.Item2 - _boxes.Length];

                    var contacts = CollisionDetector.BoxAndSphere(box1.EngineBox, ball2.EngineBall, _collisionData);
                    if (contacts)
                    {
                        box1.EngineBox.IsOverlapping = ball2.EngineBall.IsOverlapping = true;
                    }
                }
            }
            else
            {
                if (pair.Item2 < _boxes.Length)
                {
                    var ball1 = _balls[pair.Item1 - _boxes.Length];
                    var box2 = _boxes[pair.Item2];

                    var contacts = CollisionDetector.BoxAndSphere(box2.EngineBox, ball1.EngineBall, _collisionData);
                    if (contacts)
                    {
                        box2.EngineBox.IsOverlapping = ball1.EngineBall.IsOverlapping = true;
                    }
                }
                else
                {
                    var ball1 = _balls[pair.Item1 - _boxes.Length];
                    var ball2 = _balls[pair.Item2 - _boxes.Length];

                    var contacts =
                        CollisionDetector.SphereAndSphere(ball1.EngineBall, ball2.EngineBall, _collisionData);
                    if (contacts)
                    {
                        ball1.EngineBall.IsOverlapping = ball2.EngineBall.IsOverlapping = true;
                    }
                }
            }
        }

        // cloth contacts
        var engCloth = _cloth.EngineCloth;
        for (int x = 0; x < engCloth.sizeX; x++)
        {
            for (int y = 0; y < engCloth.sizeY; y++)
            {
                if (!_collisionData.HasMoreContacts()) return;

                var rigidParticle = engCloth.particles[x, y];
                if (rigidParticle == null || rigidParticle.Body == null) continue;

                var collParticle = new CollisionParticle();
                collParticle.Body = rigidParticle.Body;
                collParticle.CalculateInternals();


                if (!_collisionData.HasMoreContacts()) return;
                CollisionDetector.ParticleAndHalfSpace(collParticle, _plane.EnginePlane, _collisionData);


                for (var b = 0; b < _boxes.Length; b++)
                {
                    if (!_collisionData.HasMoreContacts()) return;
                    CollisionDetector.BoxAndParticle(_boxes[b].EngineBox, collParticle, _collisionData);
                }
            }
        }
    }

    /// <summary>
    /// Processes the objects in the simulation forward in time.
    /// </summary>
    protected override void UpdateObjects(float duration)
    {
        _forceRegistry.updateForces(duration);
        _cloth.EngineCloth.Update(duration);

        // Update the physics of each box in turn
        foreach (var box in _boxes)
        {
            box.EngineBox.Body.Integrate(duration);
            box.EngineBox.CalculateInternals();
            box.EngineBox.IsOverlapping = false;
        }

        foreach (var ball in _balls)
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
        _cloth.EngineCloth = new Engine.Cloth(_forceRegistry);

        // reset boxes; some in preconfigured positions
        if (_boxes.Length > 0)
        {
            _boxes[0].EngineBox.SetState(
                position: new Engine.Vector3(0, 3, 0),
                orientation: new Engine.Quaternion(),
                extents: new Engine.Vector3(4, 1, 1),
                velocity: new Engine.Vector3(0, 0, 0)
            );
        }

        if (_boxes.Length > 1)
        {
            _boxes[1].EngineBox.SetState(
                position: new Engine.Vector3(0, 4.75f, 2),
                orientation: new Engine.Quaternion(1.0f, 0.1f, 0.05f, 0.01f),
                extents: new Engine.Vector3(1, 1, 4),
                velocity: new Engine.Vector3(0, 0, 0)
            );
        }

        Random random = new();
        for (var i = 2; i < _boxes.Length; i++)
        {
            _boxes[i].EngineBox.Random(random);
        }

        // reset spheres; some in preconfigured positions
        if (_balls.Length > 0)
        {
            _balls[0].EngineBall.SetState(
                position: new Engine.Vector3(0, 10, 0),
                orientation: new Engine.Quaternion(),
                radius: 1.0f,
                velocity: new Engine.Vector3(0, 0, 0));
        }

        if (_balls.Length > 1)
        {
            _balls[1].EngineBall.SetState(
                position: new Engine.Vector3(3, 4.75f, 2),
                orientation: new Engine.Quaternion(),
                radius: 1.0f,
                velocity: new Engine.Vector3(0, 0, 0));
        }

        for (var i = 2; i < _balls.Length; i++)
        {
            _balls[i].EngineBall.Random(random);
        }

        // Reset the contacts
        _collisionData.ContactCount = 0;
    }
}