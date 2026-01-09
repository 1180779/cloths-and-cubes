using System.Runtime.CompilerServices;

using Engine.Collision;
using Engine.Collision.Bounding_Volume_Hierarchy;
using Engine.Force;
using Engine.Rays;
using Engine.RigidBodies;

using ImGuiNET;

using Visualisation.Core;
using Visualisation.Core.GameObjects;

using Visualization.UiLayer.UI.Windows;

using Box = Visualisation.Core.GameObjects.Box;
using Cloth = Visualisation.Core.GameObjects.Cloth;
using Random = Engine.Random;

namespace Visualization.UiLayer.Applications.Demos;

public class BoxesDemo : RigidBodyApplication
{
    protected Plane _plane = null!; // initialized in scene initialization
    protected Box[] _boxes = [];
    protected Ball[] _balls = [];
    protected Cloth[] _cloths = [];
    protected ForceRegistry _forceRegistry = new();
    
    protected BVH _bvh = BVH.Build([]);
    protected Dictionary<int, IBoxable> _bvhDictionary = [];
    protected Dictionary<RigidParticle,Box> corrections = [];
    protected SelectionManager _selectionManager;
    protected BvhNodesWindow _bvhNodesWindow = new();
    protected CollisionParametersWindow _collisionParametersWindow;

    protected BoxesDemoSettingsWindow
        _boxesDemoSettingsWindow = new(1, 0, 1); // the delegates need to be initialized in the constructor

    public BoxesDemo()
    {
        _collisionParametersWindow = new(_collisionData);

        _boxesDemoSettingsWindow.SetBoxesCount = count =>
        {
            Random random = new();
            int length = _boxes.Length;
            for (int i = count; i < length; ++i)
            {
                _sceneManager.RemoveGameObject(_boxes[i]);
                _boxes[i].Dispose();
            }

            Array.Resize(ref _boxes, count);
            for (int i = length; i < count; ++i)
            {
                _boxes[i] = new Box();
                _boxes[i].EngineBox.Random(random);
                _sceneManager.AddGameObject(_boxes[i]);
            }
        };
        _boxesDemoSettingsWindow.SetSpheresCount = count =>
        {
            Random random = new();
            int length = _balls.Length;
            for (int i = count; i < length; ++i)
            {
                _sceneManager.RemoveGameObject(_balls[i]);
                _balls[i].Dispose();
            }

            Array.Resize(ref _balls, count);
            for (int i = length; i < count; ++i)
            {
                _balls[i] = new Ball();
                _balls[i].EngineBall.Random(random);
                _sceneManager.AddGameObject(_balls[i]);
            }
        };
        _boxesDemoSettingsWindow.SetClothsCount = count =>
        {
            int length = _cloths.Length;
            for (int i = count; i < length; ++i)
            {
                _cloths[i].EngineCloth.RemoveSpringsFromForceRegistry();
                _sceneManager.RemoveGameObject(_cloths[i]);
                _cloths[i].Dispose();
            }

            Array.Resize(ref _cloths, count);
            for (int i = length; i < count; ++i)
            {
                _cloths[i] = new Cloth(_forceRegistry, _boxesDemoSettingsWindow.SizeX, _boxesDemoSettingsWindow.SizeY,
                    _boxesDemoSettingsWindow.SpringLength, _boxesDemoSettingsWindow.SpringConstant,
                    _boxesDemoSettingsWindow.ParticleMass);
                _sceneManager.AddGameObject(_cloths[i]);
            }
        };

        _selectionManager = new(_inputProvider, () => _sceneManager.CamerasManager.CurrentCamera, () => _bvh,
            (ray, index) =>
            {
                if (!_bvhDictionary.TryGetValue(index, out var item))
                {
                    if (index == -1)
                    {
                        if (RayIntersection.IntersectRayPlane(ray, _plane.EnginePlane, out var planeDistance))
                        {
                            return (true, planeDistance, _plane);
                        }
                    }

                    return (false, 0, null);
                }

                Real distance;
                switch (item)
                {
                    case Box box:
                        if (RayIntersection.IntersectRayOBB(ray, box.EngineBox, out distance))
                            return (true, distance, box);
                        break;
                    case Ball ball:
                        if (RayIntersection.IntersectRaySphere(ray, ball.EngineBall, out distance))
                            return (true, distance, ball);
                        break;
                    case Cloth cloth:
                        var triangles = cloth.VisualCloth.GetTriangles();
                        if (RayIntersection.IntersectRayCloth(ray, triangles, out distance))
                            return (true, distance, cloth);
                        break;
                    case RigidParticle particle:
                        if (RayIntersection.IntersectRayAABB(ray, particle.GetBoundingBox(), out distance))
                            return (true, distance, particle);
                        break;
                }

                return (false, 0, null);
            });
    }

    protected override void InitializeScene()
    {
        base.InitializeScene();

        _sceneManager.SelectionManager = _selectionManager;

        /* add ground plane to the scene */
        _plane = new();
        _plane.Invisible = true; // start invisible by default
        _sceneManager.AddGameObject(_plane);

        /* set everything up */
        Reset();
    }

    protected override void RenderWindows(double dt)
    {
        base.RenderWindows(dt);
        _collisionParametersWindow.Draw();
        _selectionManager.DrawWindow();
        _bvhNodesWindow.Draw();
#if DEBUG
        ContactsInspectorWindow.Draw(_collisionData.ContactList);
#endif
        _boxesDemoSettingsWindow.Draw();
    }

    protected override void DebugRenderInScene(Shader sh)
    {
        base.DebugRenderInScene(sh);

        // no need to rebuild?
        // BvhRebuild();
        _bvhNodesWindow.DebugRenderInScene(sh, _bvh);
        _selectionManager.DebugRenderInScene(sh);
    }

    protected void BvhRebuild()
    {
        _bvhDictionary.Clear();
        int offset = 0;

        for (int i = 0; i < _boxes.Length; i++)
        {
            _bvhDictionary[offset++] = _boxes[i];
        }

        for (int i = 0; i < _balls.Length; i++)
        {
            _bvhDictionary[offset++] = _balls[i];
        }

        for (int i = 0; i < _cloths.Length; i++)
        {
            _bvhDictionary[offset++] = _cloths[i];
        }

        foreach (var cloth in _cloths)
        {
            for (int i = 0; i < cloth.EngineCloth.SizeX; i++)
            {
                for (int j = 0; j < cloth.EngineCloth.SizeY; j++)
                {
                    var particle = cloth.EngineCloth.Particles[i, j];
                    if (particle != null && particle.Body != null)
                    {
                        _bvhDictionary[offset++] = particle;
                    }
                }
            }
        }

        _bvh = BVH.Build(_bvhDictionary);
    }

    protected override long AvailableSteps
    {
        get
        {
            return base.AvailableSteps;
        }
        set
        {
            _forcebvhRebuildOnNoUpdate = true;
            base.AvailableSteps = value;
        }
    }

    protected bool _forcebvhRebuildOnNoUpdate;

    protected override void OnNoPhysicsUpdate()
    {
        base.OnNoPhysicsUpdate();

        if (_forcebvhRebuildOnNoUpdate)
        {
            BvhRebuild();
            _forcebvhRebuildOnNoUpdate = false;
        }

        ObjectSelectionHandling();
    }

    protected void ObjectSelectionHandling()
    {
        if (_sceneWindow.IsHovered && !_sceneManager.CamerasManager.CameraMode)
        {
            var mousePos = ImGui.GetMousePos();
            var imageTopLeft = _sceneWindow.ImageTopLeft;

            float relativeX = mousePos.X - imageTopLeft.X;
            float relativeY = mousePos.Y - imageTopLeft.Y;

            var fbScale = _imGuiController.ScaleFactor;
            float scaledX = relativeX * fbScale.X;
            float scaledY = relativeY * fbScale.Y;
            var viewportMousePos = new Vector2(scaledX, scaledY);
            _selectionManager.HandleInput(viewportMousePos, _sceneWindow.Width, _sceneWindow.Height);
        }
    }

    /// <summary>
    /// Processes the contact generation code.
    /// </summary>
    protected override void GenerateContacts()
    {
        // Set up the collision data structure
        _collisionData.Reset(MaxContacts);

        BvhRebuild();
        ObjectSelectionHandling();


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

        // Process particle-plane collisions
        foreach (var cloth in _cloths)
        {
            foreach (var particle in cloth.EngineCloth.Particles)
            {
                if (particle == null || particle.Body == null) continue;
                if (!_collisionData.HasMoreContacts()) return;
                CollisionDetector.ParticleAndHalfSpace(particle, _plane.EnginePlane, _collisionData);
            }
        }
        corrections.Clear();
        List<(int, int)> potentialCollisions = new();
        BVH.GetPotentialContacts(ref potentialCollisions, _bvh.root);
        
        foreach (var pair in potentialCollisions)
        {
            if (!_collisionData.HasMoreContacts()) return;

            var obj1 = _bvhDictionary[pair.Item1];
            var obj2 = _bvhDictionary[pair.Item2];

            switch (obj1, obj2)
            {
                case (Box b1, Box b2):
                    var contacts = CollisionDetector.BoxAndBox(b1.EngineBox, b2.EngineBox, _collisionData);
                    if (contacts > 0)
                        b1.EngineBox.IsOverlapping = b2.EngineBox.IsOverlapping = true;
                    break;

                case (Box b, Ball s):
                    HandleBoxBall(b, s);
                    break;
                case (Ball s, Box b):
                    HandleBoxBall(b, s);
                    break;

                case (Ball s1, Ball s2):
                    var hit2 = CollisionDetector.SphereAndSphere(s1.EngineBall, s2.EngineBall, _collisionData);
                    if (hit2)
                        s1.EngineBall.IsOverlapping = s2.EngineBall.IsOverlapping = true;
                    break;

                case (Box b, RigidParticle p):
                    HandleBoxParticle(b, p);
                    break;
                case (RigidParticle p, Box b):
                    HandleBoxParticle(b, p);
                    break;
            }
        }

        return;

        // helper handling functions to remove code duplication

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void HandleBoxBall(Box b, Ball s)
        {
            var hit = CollisionDetector.BoxAndSphere(b.EngineBox, s.EngineBall, _collisionData);
            if (hit)
                b.EngineBox.IsOverlapping = s.EngineBall.IsOverlapping = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void HandleBoxParticle(Box b, RigidParticle p)
        {
            var contacts = CollisionDetector.BoxAndParticle(b.EngineBox, p, _collisionData);
            if (contacts > 0)
                b.EngineBox.IsOverlapping = true;
            if(! corrections.ContainsKey(p)) corrections.Add(p,b);

        }
    }

    /// <summary>
    /// Processes the objects in the simulation forward in time.
    /// </summary>
    protected override void UpdateObjects(float duration)
    {
        _forceRegistry.updateForces(duration);
        foreach (Cloth cloth in _cloths)
        {
            cloth.EngineCloth.Update(duration);
        }

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
    /// Trying to correct the look of the cloths
    /// </summary>
    protected override void CorrectCloths()
    {
        
    }
    /// <summary>
    /// Resets the position of all the objects.
    /// </summary>
    protected override void Reset()
    {
        _forcebvhRebuildOnNoUpdate = true;

        _forceRegistry.Clear();
        foreach (Cloth cloth in _cloths)
        {
            cloth.EngineCloth = new Engine.Cloth(_forceRegistry, _boxesDemoSettingsWindow.SizeX,
                _boxesDemoSettingsWindow.SizeY,
                _boxesDemoSettingsWindow.SpringLength, _boxesDemoSettingsWindow.SpringConstant,
                _boxesDemoSettingsWindow.ParticleMass);
        }

        // reset plane
        _plane.EnginePlane.Direction = Engine.Vector3.Up;
        _plane.EnginePlane.Offset = 0f;

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

    protected override ApplicationState SaveState()
    {
        var state = base.SaveState();
        state.BvhNodes = _bvhNodesWindow.SaveState();
        state.CollisionParameters = _collisionParametersWindow.SaveState();
        state.ClothSettings = _boxesDemoSettingsWindow.SaveState();
        state.SelectionSettings = _selectionManager.SaveState();
        return state;
    }

    protected override void LoadState(ApplicationState state)
    {
        base.LoadState(state);
        if (state.BvhNodes is not null)
        {
            _bvhNodesWindow.RestoreState(state.BvhNodes);
        }

        if (state.CollisionParameters is not null)
        {
            _collisionParametersWindow.RestoreState(state.CollisionParameters);
        }

        if (state.ClothSettings is not null)
        {
            _boxesDemoSettingsWindow.RestoreState(state.ClothSettings);
        }

        if (state.SelectionSettings is not null)
        {
            _selectionManager.RestoreState(state.SelectionSettings);
        }
    }
}