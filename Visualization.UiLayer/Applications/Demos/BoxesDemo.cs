using System.Runtime.CompilerServices;

using Engine.Collision;
using Engine.Collision.Bounding_Volume_Hierarchy;
using Engine.Force;
using Engine.Rays;
using Engine.RigidBodies;

using ImGuiNET;

using Visualisation.Core;
using Visualisation.Core.GameObjects;
using Visualisation.Core.GameObjects.Scenes;

using Visualization.UiLayer.UI.Windows;

using Box = Visualisation.Core.GameObjects.Box;
using Cloth = Visualisation.Core.GameObjects.Cloth;
using Cone = Visualisation.Core.GameObjects.Cone;
using Cylinder = Visualisation.Core.GameObjects.Cylinder;
using Random = Engine.Random;

namespace Visualization.UiLayer.Applications.Demos;

public class BoxesDemo : RigidBodyApplication
{
    protected Plane _plane = null!; // initialized in scene initialization
    public Plane Plane => _plane;

    protected Box[] _boxes = [];
    protected Ball[] _balls = [];
    protected Cloth[] _cloths = [];

    // Store initial state for proper reset
    private SceneData? _initialSceneState;
    private bool _sceneWasLoaded;

    protected ForceRegistry _forceRegistry = new();

    protected BVH _bvh = BVH.Build([]);
    protected Dictionary<int, IBoxable> _bvhDictionary = [];

    protected SelectionManager _selectionManager;
    protected SelectionManagerWindow _selectionManagerWindow;
    protected GizmoSettingsWindow _gizmoSettingsWindow;
    protected PhysicsControlWindow _physicsControlWindow;
    protected SceneManagementWindow _sceneManagementWindow;

    protected BvhNodesWindow _bvhNodesWindow = new();

    // Public accessors for scene management
    public SceneManager SceneManager => _sceneManager;
    public CollisionData CollisionData => _collisionData;
    public ForceRegistry ForceRegistry => _forceRegistry;
    protected CollisionParametersWindow _collisionParametersWindow;
    protected ContactsInspectorWindow _contactsInspectorWindow;

    protected BoxesDemoSettingsWindow _boxesDemoSettingsWindow;
    public object? SelectedObject => _selectionManager.SelectedObject;

    public BoxesDemo()
    {
        _windowsManager.Add(_bvhNodesWindow);

        _collisionParametersWindow = new(_collisionData);
        _windowsManager.Add(_collisionParametersWindow);

        _contactsInspectorWindow = new(() => _collisionData.ContactList);
        _windowsManager.Add(_contactsInspectorWindow);

        _boxesDemoSettingsWindow = new(() => _boxes.Length, () => _balls.Length, () => _cloths.Length)
        {
            SetBoxesCount = count =>
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

                _forcebvhRebuildOnNoUpdate = true;
            },
            SetSpheresCount = count =>
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

                _forcebvhRebuildOnNoUpdate = true;
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

            _forcebvhRebuildOnNoUpdate = true;
        };
        _windowsManager.Add(_boxesDemoSettingsWindow);


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
                    case Cylinder cylinder:
                        if (RayIntersection.IntersectionRayCylinder(ray, cylinder.EngineCylinder, out distance))
                            return (true, distance, cylinder);
                        break;
                    case Cone cone:
                        if (RayIntersection.IntersectionRayCone(ray, cone.EngineCone, out distance))
                            return (true, distance, cone);
                        break;
                }

                return (false, 0, null);
            });

        _selectionManagerWindow = new(_selectionManager);
        _windowsManager.Add(_selectionManagerWindow);

        _gizmoSettingsWindow = new(_sceneManager);
        _windowsManager.Add(_gizmoSettingsWindow);

        _physicsControlWindow = new(this);
        _windowsManager.Add(_physicsControlWindow);

        _sceneManagementWindow = new(this);
        _windowsManager.Add(_sceneManagementWindow);
    }

    protected override void InitializeScene()
    {
        base.InitializeScene();

        _sceneManager.SelectionManager = _selectionManager;

        // add ground plane to the scene
        _plane = new();
        _sceneManager.AddGameObject(_plane);

        /* set everything up */
        Reset();
    }

    protected override void DebugRenderInScene(Shader sh)
    {
        base.DebugRenderInScene(sh);

        // no need to rebuild?
        // BvhRebuild();
        _bvhNodesWindow.DebugRenderInScene(sh, _bvh);
        _selectionManagerWindow.DebugRenderInScene(sh);
    }

    protected void BvhRebuild()
    {
        _bvhDictionary.Clear();
        int offset = 0;

        foreach (var t in _boxes)
        {
            _bvhDictionary[offset++] = t;
        }

        foreach (var t in _balls)
        {
            _bvhDictionary[offset++] = t;
        }

        foreach (var t in _cloths)
        {
            _bvhDictionary[offset++] = t;
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

    public override long AvailableSteps
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

            // Pass input to SceneManager, which handles Gizmos and Selection
            _sceneManager.HandleInput(_inputProvider, viewportMousePos, new(_sceneWindow.Width, _sceneWindow.Height));
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
        if(SelectedObject is not null)
        {
            var collisionObject = SelectedObject as CollisionPrimitive;
        }

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
    /// Resets the position of all the objects.
    /// </summary>
    public override void Reset()
    {
        _forcebvhRebuildOnNoUpdate = true;

        // If a scene was loaded, restore it instead of using hardcoded values
        if (_sceneWasLoaded && _initialSceneState != null)
        {
            RestoreSceneState(_initialSceneState);
            return;
        }

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

    /// <summary>
    /// Restore the scene from stored state - preserves object identity when possible to maintain selection/gizmos
    /// </summary>
    private void RestoreSceneState(SceneData sceneData)
    {
        ApplySceneData(sceneData);
        _collisionData.ContactCount = 0;
    }

    /// <summary>
    /// Apply scene data to the current scene.
    /// Preserves object identity when possible to maintain selection/gizmos.
    /// </summary>
    public void ApplySceneData(SceneData sceneData)
    {
        var currentObjects = _sceneManager.GameObjects.ToList();
        var currentPlane = _plane;

        var updatedObjects = sceneData.ToGameObjectsWithUpdate(
            _forceRegistry,
            currentObjects,
            out var plane,
            out var collisionData,
            out var objectsToRemove);

        _collisionData.Friction = collisionData.Friction;
        _collisionData.Restitution = collisionData.Restitution;
        _collisionData.Tolerance = collisionData.Tolerance;

        // Clear selection and gizmos if the selected object is being removed
        if (_sceneManager.SelectionManager?.SelectedObject is GameObject selectedObject &&
            objectsToRemove.Contains(selectedObject))
        {
            _sceneManager.ClearSelectionAndGizmos();
        }

        // Remove objects that are no longer in the scene
        foreach (var obj in objectsToRemove)
        {
            if (obj is Cloth cloth)
            {
                cloth.EngineCloth.RemoveSpringsFromForceRegistry();
            }

            _sceneManager.RemoveGameObject(obj);
            obj.Dispose();
        }

        // Add newly created objects to the scene
        foreach (var obj in updatedObjects)
        {
            if (obj is not Visualisation.Core.GameObjects.Plane && !currentObjects.Contains(obj))
            {
                _sceneManager.AddGameObject(obj);
            }
        }

        UpdateObjectArrays(updatedObjects);

        if (plane != null && plane != currentPlane)
        {
            UpdatePlane(plane);
        }
    }

    protected override ApplicationState SaveState()
    {
        var state = base.SaveState();
        state.BvhNodes = _bvhNodesWindow.SaveState();
        state.CollisionParameters = _collisionParametersWindow.SaveState();
        state.ClothSettings = _boxesDemoSettingsWindow.SaveState();
        state.SelectionSettings = _selectionManagerWindow.SaveState();
        state.GizmoSettings = _gizmoSettingsWindow.SaveState();
        state.PhysicsControl = _physicsControlWindow.SaveState();
        state.SceneManagement = _sceneManagementWindow.SaveState();
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
            _selectionManagerWindow.RestoreState(state.SelectionSettings);
        }

        if (state.GizmoSettings is not null)
        {
            _gizmoSettingsWindow.RestoreState(state.GizmoSettings);
        }

        if (state.PhysicsControl is not null)
        {
            _physicsControlWindow.RestoreState(state.PhysicsControl);
        }

        if (state.SceneManagement is not null)
        {
            _sceneManagementWindow.RestoreState(state.SceneManagement);
        }
    }

    /// <summary>
    /// Update object arrays after scene loading
    /// </summary>
    public void UpdateObjectArrays(List<GameObject> gameObjects)
    {
        var boxes = new List<Box>();
        var balls = new List<Ball>();
        var cloths = new List<Cloth>();

        foreach (var obj in gameObjects)
        {
            switch (obj)
            {
                case Box box:
                    boxes.Add(box);
                    break;
                case Ball ball:
                    balls.Add(ball);
                    break;
                case Cloth cloth:
                    cloths.Add(cloth);
                    break;
            }
        }

        _boxes = boxes.ToArray();
        _balls = balls.ToArray();
        _cloths = cloths.ToArray();

        _forcebvhRebuildOnNoUpdate = true;
    }

    /// <summary>
    /// Store the initial scene state for reset functionality
    /// </summary>
    public void StoreInitialSceneState(SceneData sceneData)
    {
        _initialSceneState = sceneData;
        _sceneWasLoaded = true;
    }

    /// <summary>
    /// Update plane reference after scene loading
    /// </summary>
    public void UpdatePlane(Plane plane)
    {
        _sceneManager.RemoveGameObject(_plane);
        _plane.Dispose();

        _plane = plane;
        _sceneManager.AddGameObject(_plane);
    }
}