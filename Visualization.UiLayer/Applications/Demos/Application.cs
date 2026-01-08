using System.Runtime.CompilerServices;

using Engine;
using Engine.Collision;
using Engine.Collision.Bounding_Volume_Hierarchy;
using Engine.Force;
using Engine.RigidBodies;

using ImGuiNET;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

using Visualisation.Core;
using Visualisation.Core.Display.Texture;
using Visualisation.Core.GameObjects;
using Visualisation.Core.GameObjects.Scenes;
using Visualisation.Core.Inputs;

using Visualization.UiLayer.Inputs;
using Visualization.UiLayer.UI;
using Visualization.UiLayer.UI.Windows;

using Box = Visualisation.Core.GameObjects.Box;
using Cloth = Visualisation.Core.GameObjects.Cloth;
using Random = Engine.Random;

namespace Visualization.UiLayer.Applications.Demos;

public class Application : GameWindow
{
    // Fields from Application
    private static bool s_fpsCappedTo60 = true;
    protected readonly ImGuiController _imGuiController;
    protected readonly IInputProvider _inputProvider;
    protected readonly SettingsSaverLoader _settingsSaverLoader = new();
    protected readonly WindowsManager _windowsManager = new();
    protected readonly SceneWindow _sceneWindow;
    protected readonly CascadingShadowMapsWindow _cascadingShadowMapsWindow;

    protected SceneRenderer _sceneRenderer; // initialized in constructor

    // Application properties
    public bool StepsLimit { get; set; }
    protected long AvailableStepsInternal { get; set; }

    public virtual long AvailableSteps
    {
        get => AvailableStepsInternal;
        set
        {
            _forceBVHRebuildOnNoUpdate = true;
            AvailableStepsInternal = value;
        }
    }

    protected bool DoUpdate
    {
        get
        {
            if (!StepsLimit) return true;
            if (AvailableSteps <= 0) return false;
            AvailableSteps--;
            return true;
        }
    }

    // Physics
    protected static uint MaxContacts => 2 * 1024;

    protected CollisionData _collisionData = new()
    {
        Friction = (Real)0.9, Restitution = (Real)0.6, Tolerance = (Real)0.1,
    };

    protected ContactResolver _contactResolver = new(MaxContacts * 8, positionEpsilon: 0.01f);

    // Scene Objects
    protected Plane _plane = null!; // initialized in scene initialization
    protected Box[] _boxes = [];
    protected Ball[] _balls = [];
    protected Cloth[] _cloths = [];

    public IEnumerable<GameObject> GameObjects => [_plane, .._boxes, .._balls, .._cloths];

    // Store initial state for proper reset
    private SceneData? _initialSceneState;
    private bool _sceneWasLoaded;

    private float _lastFrameDuration;

    protected ForceRegistry _forceRegistry = new();

    protected BVH _bvh = BVH.BuildSynchronous([]);
    protected Dictionary<int, IBoxable> _bvhDictionary = [];

    protected SelectionManagerWindow _selectionManagerWindow;
    protected GizmoSettingsWindow _gizmoSettingsWindow;
    protected PhysicsControlWindow _physicsControlWindow;
    protected SceneManagementWindow _sceneManagementWindow;

    protected BvhNodesWindow _bvhNodesWindow = new();

    // Public accessors for scene management
    public SceneRenderer SceneRenderer => _sceneRenderer;
    public CollisionData CollisionData => _collisionData;
    public ForceRegistry ForceRegistry => _forceRegistry;
    protected CollisionParametersWindow _collisionParametersWindow;
    protected ContactsInspectorWindow _contactsInspectorWindow;

    protected BoxesDemoSettingsWindow _boxesDemoSettingsWindow;

    public Application() : base(
        GameWindowSettings.Default,
        new NativeWindowSettings { WindowState = WindowState.Maximized })
    {
        Size = (800, 600);
        Title = "Boxes Demo";

        if (s_fpsCappedTo60) UpdateFrequency = 60.0;

        _imGuiController = new ImGuiController(this);
        _imGuiController.HookToWindow(this);
        _inputProvider = new OpenTKWithImGuiInputProvider(this, _imGuiController);

        // Initialize SceneRenderer with all required providers
        _sceneRenderer = new SceneLightningOnly(
            Size.X / (float)Size.Y,
            _inputProvider,
            () => GameObjects,
            () => _sceneRenderer.CamerasManager.CurrentCamera,
            () => _bvh,
            () => _bvhDictionary,
            () => _cloths.ToDictionary(c => c.EngineCloth, c => c),
            () => _plane,
            () => _contactResolver.PositionEpsilon);

        _sceneWindow = new SceneWindow(_imGuiController, _sceneRenderer, _inputProvider, Size);
        _sceneWindow.DebugRenderInScene += DebugRenderInScene;

        _cascadingShadowMapsWindow =
            new CascadingShadowMapsWindow(_imGuiController, _sceneRenderer.LightsManager, Size);

        // GL setup
        GL.ClearColor(0.2f, 0.3f, 0.5f, 1f);
        GL.Enable(EnableCap.CullFace);
        GL.Enable(EnableCap.DepthTest);
        GL.FrontFace(FrontFaceDirection.Ccw);
        GL.Enable(EnableCap.TextureCubeMapSeamless);

        // Initialize windows
        _windowsManager.Add(new StatsWindow(_sceneRenderer));
        _windowsManager.Add(new HelpWindow());
        _windowsManager.Add(new ObjectInspectorWindow(() => GameObjects));
        _windowsManager.Add(new GraphicsSettingsWindow(
            () => _sceneRenderer.LightsManager.DirectionalLight,
            _sceneRenderer, _sceneWindow));
        _windowsManager.Add(_cascadingShadowMapsWindow);

        _sceneRenderer.PositionEpsilonProvider = () => _contactResolver.PositionEpsilon;
        _sceneRenderer.InteractionManager.StaticDragManager.OnObjectDragged += BvhRebuild;

        // Physics-specific windows
        _windowsManager.Add(_bvhNodesWindow);

        _collisionParametersWindow = new(_collisionData);
        _windowsManager.Add(_collisionParametersWindow);

        _contactsInspectorWindow = new(() => _collisionData.ContactList);
        _windowsManager.Add(_contactsInspectorWindow);

        _boxesDemoSettingsWindow = new(() => _boxes.Length, () => _balls.Length, () => _cloths.Length)
        {
            SetBoxesCount = newCount =>
            {
                Random random = new();
                int length = _boxes.Length;

                var toBeRemoved = _boxes.Skip(newCount).Take(length - newCount).Select(b => (object)b).ToArray();
                _sceneRenderer.InteractionManager.RemoveObjects(toBeRemoved);

                for (int i = newCount; i < length; ++i)
                {
                    _boxes[i].Dispose();
                }

                Array.Resize(ref _boxes, newCount);

                for (int i = length; i < newCount; ++i)
                {
                    _boxes[i] = new Box();
                    _boxes[i].EngineBox.Random(random);
                }

                _forceBVHRebuildOnNoUpdate = true;
            },
            SetSpheresCount = newCount =>
            {
                Random random = new();
                int length = _balls.Length;

                var toBeRemoved = _balls.Skip(newCount).Take(length - newCount).Select(b => (object)b).ToArray();
                _sceneRenderer.InteractionManager.RemoveObjects(toBeRemoved);

                for (int i = newCount; i < length; ++i)
                {
                    _balls[i].Dispose();
                }

                Array.Resize(ref _balls, newCount);
                for (int i = length; i < newCount; ++i)
                {
                    _balls[i] = new Ball();
                    _balls[i].EngineBall.Random(random);
                }

                _forceBVHRebuildOnNoUpdate = true;
            }
        };
        _boxesDemoSettingsWindow.SetClothsCount = newCount =>
        {
            int length = _cloths.Length;

            var toBeRemoved = _cloths.Skip(newCount).Take(length - newCount).Select(b => (object)b).ToArray();
            _sceneRenderer.InteractionManager.RemoveObjects(toBeRemoved);

            for (int i = newCount; i < length; ++i)
            {
                _cloths[i].EngineCloth.RemoveSpringsFromForceRegistry();
                _cloths[i].Dispose();
            }

            Array.Resize(ref _cloths, newCount);

            for (int i = length; i < newCount; ++i)
            {
                _cloths[i] = new Cloth(_forceRegistry, () => _contactResolver.PositionEpsilon,
                    _boxesDemoSettingsWindow.SizeX, _boxesDemoSettingsWindow.SizeY,
                    _boxesDemoSettingsWindow.SpringLength, _boxesDemoSettingsWindow.SpringConstant,
                    _boxesDemoSettingsWindow.ParticleMass);
            }

            _forceBVHRebuildOnNoUpdate = true;
        };
        _windowsManager.Add(_boxesDemoSettingsWindow);

        _selectionManagerWindow = new(_sceneRenderer.InteractionManager);
        _windowsManager.Add(_selectionManagerWindow);

        _gizmoSettingsWindow = new(_sceneRenderer.InteractionManager);
        _windowsManager.Add(_gizmoSettingsWindow);

        _physicsControlWindow = new(this);
        _windowsManager.Add(_physicsControlWindow);

        _sceneManagementWindow = new(this);
        _windowsManager.Add(_sceneManagementWindow);

        _sceneRenderer.PositionEpsilonProvider = () => _contactResolver.PositionEpsilon;

        _sceneRenderer.InteractionManager.StaticDragManager.OnObjectDragged += BvhRebuild;
    }

    protected void InitializeScene()
    {
        // add ground plane to the scene
        _plane = new();

        /* set everything up */
        Reset();
    }

    protected void DebugRenderInScene(Shader sh)
    {
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
                    _bvhDictionary[offset++] = particle;
                }
            }
        }

        _bvh = BVH.BuildSynchronous(_bvhDictionary);
    }

    protected bool _forceBVHRebuildOnNoUpdate;

    protected void OnNoPhysicsUpdate()
    {
        if (_forceBVHRebuildOnNoUpdate)
        {
            BvhRebuild();
            _forceBVHRebuildOnNoUpdate = false;
        }

        ObjectSelectionHandling();
    }

    protected void ObjectSelectionHandling()
    {
        // Keep selection/drag handling active even if hover is lost during active operations
        bool isDragging = _sceneRenderer.InteractionManager.StaticDragManager.IsDragging;
        bool isGizmoActive = _sceneRenderer.InteractionManager.ActiveGizmo?.IsActive ?? false;

        if (_sceneWindow.IsHovered ||
            _inputProvider.GetCursorState() == Visualisation.Core.Inputs.CursorState.Grabbed ||
            isDragging || isGizmoActive)
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
            _sceneRenderer.HandleInput(_inputProvider, viewportMousePos, new(_sceneWindow.Width, _sceneWindow.Height));
        }
    }

    /// <summary>
    /// Processes the contact generation code.
    /// </summary>
    protected void GenerateContacts()
    {
        // Set up the collision data structure
        _collisionData.Reset(MaxContacts);

        BvhRebuild();
        ObjectSelectionHandling();

        // The dragged object can find itself below the plane when set by the user 
        // (in the case of using springs, this would be the anchor point below 
        // the plane). It is necessary to first place the object within the valid scene 
        // and update the bvh to reflect that to allow for proper collision 
        // resolution. If not, the object is only resolved with the plane 
        // and is not resolved with any objects that are within the scene and 
        // above it, which leads to no collision resolution in that case.
        // (In the future spring version this would make the desired position 
        // to be outside the scene and probably cause issues with the spring force.) 
        var draggedObject = _sceneRenderer.InteractionManager.StaticDragManager.DraggedObject;
        if (draggedObject != null)
        {
            _collisionData.Reset(MaxContacts);
            if (draggedObject is Box box)
            {
                CollisionDetector.BoxAndHalfSpace(box.EngineBox, _plane.EnginePlane, _collisionData);
            }
            else if (draggedObject is Ball ball)
            {
                CollisionDetector.SphereAndHalfSpace(ball.EngineBall, _plane.EnginePlane, _collisionData);
            }
            else if (draggedObject is Cloth cloth)
            {
                foreach (var particle in cloth.EngineCloth.Particles)
                {
                    if (!_collisionData.HasMoreContacts()) break;
                    CollisionDetector.ParticleAndHalfSpace(particle, _plane.EnginePlane, _collisionData);
                }
            }

            if (_collisionData.ContactCount > 0)
            {
                _contactResolver.ResolveContacts(_collisionData.ContactList, _collisionData.ContactCount,
                    _lastFrameDuration);
            }

            // Rebuild BVH after resolving dragged object collisions
            // This ensures that the BVH is up to date with the resolved position
            // of the dragged object, so that later collision checks
            // (e.g., between the dragged object and other objects) are correct.
            BvhRebuild();

            _collisionData.Reset(MaxContacts);
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

    protected void Update(float deltaTime)
    {
        // Physics update from RigidBodyApplication
        if (!DoUpdate)
        {
            OnNoPhysicsUpdate();
            return;
        }

        switch (deltaTime)
        {
            case <= 0.0f:
                return;
            case > 0.05f:
                deltaTime = 0.05f;
                break;
        }

        _lastFrameDuration = deltaTime;

        // Update objects
        UpdateObjects(deltaTime);

        // Perform the contact generation
        GenerateContacts();

        // Resolve detected contacts
        _contactResolver.ResolveContacts(
            _collisionData.ContactList,
            _collisionData.ContactCount,
            deltaTime
        );
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);

        // Set the bypass flag BEFORE any keyboard input is processed (from Application)
        if (_inputProvider is OpenTKWithImGuiInputProvider imguiProvider)
        {
            bool viewportIsActive = _sceneWindow.IsHovered ||
                _inputProvider.GetCursorState() == Visualisation.Core.Inputs.CursorState.Grabbed ||
                _sceneRenderer.InteractionManager.StaticDragManager.IsDragging ||
                (_sceneRenderer.InteractionManager.ActiveGizmo?.IsActive ?? false);
            imguiProvider.BypassImGuiKeyboardCapture = viewportIsActive;
        }

        _inputProvider.UpdateMousePosition();
        _windowsManager.HandleInput();

        // cap/uncap fps (from Application)
        if (_inputProvider.IsKeyPressed(InputKey.X))
        {
            UpdateFrequency = UpdateFrequency switch
            {
                0.0 => 60.0,
                _ => 0.0
            };
            s_fpsCappedTo60 = !s_fpsCappedTo60;
        }

        if (!IsFocused)
        {
            return;
        }

        // Step limiting controls (from BoxesDemo)
        if (_inputProvider.IsKeyPressed(InputKey.LeftBracket))
        {
            StepsLimit = true;
        }

        if (StepsLimit)
        {
            if (_inputProvider.IsKeyDown(InputKey.D0))
                AvailableSteps = 1;
            if (_inputProvider.IsKeyPressed(InputKey.D1))
                AvailableSteps = 1;
            if (_inputProvider.IsKeyPressed(InputKey.D2))
                AvailableSteps = 2;
            if (_inputProvider.IsKeyPressed(InputKey.D3))
                AvailableSteps = 3;
            if (_inputProvider.IsKeyPressed(InputKey.D4))
                AvailableSteps = 4;
            if (_inputProvider.IsKeyPressed(InputKey.D5))
                AvailableSteps = 5;
            if (_inputProvider.IsKeyPressed(InputKey.D6))
                AvailableSteps = 6;
            if (_inputProvider.IsKeyPressed(InputKey.D7))
                AvailableSteps = 7;
            if (_inputProvider.IsKeyPressed(InputKey.D8))
                AvailableSteps = 8;
            if (_inputProvider.IsKeyPressed(InputKey.D9))
                AvailableSteps = 9;
        }

        if (_inputProvider.IsKeyPressed(InputKey.RightBracket))
        {
            StepsLimit = false;
        }

        // reset
        if (_inputProvider.IsKeyPressed(InputKey.R))
        {
            Reset();
        }
    }

    /// <summary>
    /// Processes the objects in the simulation forward in time.
    /// </summary>
    protected void UpdateObjects(float duration)
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
    public virtual void Reset()
    {
        _forceBVHRebuildOnNoUpdate = true;

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
        var currentObjects = GameObjects.ToList();
        var currentPlane = _plane;

        var updatedObjects = sceneData.ToGameObjectsWithUpdate(
            _forceRegistry,
            currentObjects,
            () => _contactResolver.PositionEpsilon,
            out var plane,
            out var collisionData,
            out var objectsToRemove);

        _collisionData.Friction = collisionData.Friction;
        _collisionData.Restitution = collisionData.Restitution;
        _collisionData.Tolerance = collisionData.Tolerance;

        // Clear selection and gizmos if the selected object is being removed
        if (_sceneRenderer.SelectionManager.SelectedObject is GameObject selectedObject &&
            objectsToRemove.Contains(selectedObject))
        {
            _sceneRenderer.InteractionManager.ClearSelectionAndGizmos();
        }

        // Remove objects that are no longer in the scene
        foreach (var obj in objectsToRemove)
        {
            if (obj is Cloth cloth)
            {
                cloth.EngineCloth.RemoveSpringsFromForceRegistry();
            }

            obj.Dispose();
        }

        UpdateObjectArrays(updatedObjects);

        if (plane != null && plane != currentPlane)
        {
            UpdatePlane(plane);
        }
    }

    // === Methods from Application ===

    protected override void OnLoad()
    {
        base.OnLoad();

        // Hook up SceneWindow debug rendering
        _sceneWindow.DebugRenderInScene += DebugRenderInScene;

        var state = _settingsSaverLoader.Load();
        if (state is not null)
        {
            LoadState(state);
        }

        InitializeScene();

        // set up internal scene objects after the scene is initialized
        _sceneRenderer.SetUp();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        _imGuiController.Update((float)args.Time);

        Update((float)args.Time); // Update game/app logic before any rendering

        _windowsManager.DrawMenu();

        // --- ImGui Docking Setup ---
        ImGuiWindowFlags dockspaceFlags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse |
            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoBringToFrontOnFocus |
            ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.NoBackground;
        ImGuiViewportPtr viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(viewport.WorkPos);
        ImGui.SetNextWindowSize(viewport.WorkSize);
        ImGui.SetNextWindowViewport(viewport.ID);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(0.0f, 0.0f));
        ImGui.Begin("DockSpace Host", dockspaceFlags);
        ImGui.PopStyleVar(3);

        uint dockspaceId = ImGui.GetID("MyDockSpace");
        ImGui.DockSpace(dockspaceId, new System.Numerics.Vector2(0, 0), ImGuiDockNodeFlags.PassthruCentralNode);

        RenderWindows(args.Time);

        // end dockspace
        ImGui.End();

        // clear the screen underneath the imGui windows (now background only)
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        _imGuiController.Render();

        SwapBuffers();

        // upload any finished texture loads to the openGL
        TexturesManager.ProcessPendingUploads();
    }

    protected override void OnFramebufferResize(FramebufferResizeEventArgs e)
    {
        base.OnFramebufferResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
    }

    protected override void OnUnload()
    {
        var state = SaveState();
        _settingsSaverLoader.Save(state);

        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);
        GL.UseProgram(0);

        _windowsManager.Dispose();
        TexturesManager.AbortAllLoads();
        _imGuiController.UnhookFromWindow(this);
        _sceneRenderer.Dispose();
        _sceneWindow.Dispose();

        base.OnUnload();
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        if (!ImGui.GetIO().WantCaptureMouse && _sceneRenderer.CamerasManager.CameraMode)
        {
            _sceneRenderer.CamerasManager.CurrentCamera.FovDegrees -= _inputProvider.GetMouseScroll();
        }
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, Size.X, Size.Y);
    }

    protected void RenderWindows(double dt)
    {
        _windowsManager.Draw();
        _sceneWindow.Draw(FramebufferSize, (float)dt);
    }

    protected ApplicationState SaveState()
    {
        return new ApplicationState
        {
            WindowsState = _windowsManager.SaveState(),
            GraphicsSettings =
                ((GraphicsSettingsWindow)_windowsManager.GetWindow(GraphicsSettingsWindow.StaticName)).SaveState(),
            CascadingShadowMaps = _cascadingShadowMapsWindow.SaveState(),
            BvhNodes = _bvhNodesWindow.SaveState(),
            CollisionParameters = _collisionParametersWindow.SaveState(),
            ClothSettings = _boxesDemoSettingsWindow.SaveState(),
            SelectionSettings = _selectionManagerWindow.SaveState(),
            GizmoSettings = _gizmoSettingsWindow.SaveState(),
            PhysicsControl = _physicsControlWindow.SaveState(),
            SceneManagement = _sceneManagementWindow.SaveState()
        };
    }

    protected void LoadState(ApplicationState state)
    {
        if (state.WindowsState is not null)
        {
            _windowsManager.RestoreState(state.WindowsState);
        }

        if (state.GraphicsSettings is not null)
        {
            ((GraphicsSettingsWindow)_windowsManager.GetWindow(GraphicsSettingsWindow.StaticName)).RestoreState(
                state.GraphicsSettings);
        }

        if (state.CascadingShadowMaps is not null)
        {
            _cascadingShadowMapsWindow.RestoreState(state.CascadingShadowMaps);
        }

        // Restore BoxesDemo-specific state
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
    /// Update object arrays after scene loading. 
    ///
    /// It is assumed that the previous objects have been removed or disposed of already.
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

        Array.Resize(ref _boxes, boxes.Count);
        Array.Copy(_boxes, boxes.ToArray(), boxes.Count);

        Array.Resize(ref _balls, balls.Count);
        Array.Copy(_balls, balls.ToArray(), balls.Count);

        Array.Resize(ref _cloths, cloths.Count);
        Array.Copy(_cloths, cloths.ToArray(), cloths.Count);

        _forceBVHRebuildOnNoUpdate = true;
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
        _sceneRenderer.InteractionManager.RemoveObject(plane);
        _plane.Dispose();
        _plane = plane;
    }
}