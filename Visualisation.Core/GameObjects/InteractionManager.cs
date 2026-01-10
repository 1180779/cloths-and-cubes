using Engine.Collision.Bounding_Volume_Hierarchy;
using Engine.ContactGenerators;

using OpenTK.Mathematics;

using Visualisation.Core.Display.Cameras;
using Visualisation.Core.Display.Gizmos;
using Visualisation.Core.Display.Gizmos.Rotation;
using Visualisation.Core.Display.Gizmos.Scale;
using Visualisation.Core.Display.Gizmos.Translation;
using Visualisation.Core.Inputs;
using Visualisation.Core.State;

namespace Visualisation.Core.GameObjects;

/// <summary>
/// Manages all user interaction with the scene: selection, dragging, and gizmo manipulation.
/// Consolidates SelectionManager, StaticDragManager, and all gizmo instances.
/// </summary>
public sealed class InteractionManager : IDisposable
{
    private readonly TranslationGizmo _translationGizmo;
    private readonly ScaleGizmo _scaleGizmo;
    private readonly RotationGizmo _rotationGizmo;

    private readonly EditorState _editorState = new();

    private Func<CameraBase> _cameraProvider;

    public InteractionManager(
        Shader shader,
        IInputProvider inputProvider,
        Func<CameraBase> cameraProvider,
        Func<BVH> bvhProvider,
        Func<Dictionary<int, IBoxable>> bvhDictionaryProvider,
        Func<Dictionary<Engine.Cloth, Cloth>> clothsProvider,
        Func<Plane> planeProvider,
        Func<float> positionEpsilonProvider,
        Func<IEnumerable<Box>> boxesProvider,
        Func<GlobalJointsList> globalJointsProvider)
    {
        _translationGizmo = new TranslationGizmo(shader);
        _scaleGizmo = new ScaleGizmo(shader);
        _rotationGizmo = new RotationGizmo(shader);

        _cameraProvider = cameraProvider;


        SelectionManager = new(
            inputProvider,
            cameraProvider,
            bvhProvider,
            bvhDictionaryProvider,
            clothsProvider,
            planeProvider,
            positionEpsilonProvider);

        StaticDragManager = new(() => SelectionManager.HoveredObject, cameraProvider, boxesProvider,
            globalJointsProvider);
    }

    public SelectionManager SelectionManager { get; set; }
    public StaticDragManager StaticDragManager { get; }

    public EditorState EditorState => _editorState;

    public IGizmo? ActiveGizmo => _editorState.Gizmo.ActiveGizmo;

    public GizmoType ActiveGizmoType => _editorState.Gizmo.ActiveGizmoType;

    public TranslationGizmo TranslationGizmo => _translationGizmo;
    public RotationGizmo RotationGizmo => _rotationGizmo;
    public ScaleGizmo ScaleGizmo => _scaleGizmo;

    /// <summary>
    /// Sets the active gizmo type based on enum value.
    /// </summary>
    public void SetActiveGizmoType(GizmoType gizmoType)
    {
        _editorState.Gizmo.ActiveGizmoType = gizmoType;
        _editorState.Gizmo.ActiveGizmo = gizmoType switch
        {
            GizmoType.None => null,
            GizmoType.Translation => _translationGizmo,
            GizmoType.Rotation => _rotationGizmo,
            GizmoType.Scale => _scaleGizmo,
            _ => null
        };

        UpdateActiveGizmoTarget();
    }

    private void UpdateActiveGizmoTarget()
    {
        if (_editorState.Gizmo.ActiveGizmo is not null && SelectionManager?.SelectedObject is not null)
        {
            var adapter = GizmoAdapterFactory.CreateAdapter(SelectionManager.SelectedObject, ActiveGizmoType);
            _editorState.Gizmo.ActiveGizmo.Target = adapter ??
                // If we can't adapt the object for this gizmo, clear the target
                null;
        }
        else if (_editorState.Gizmo.ActiveGizmo is not null)
        {
            _editorState.Gizmo.ActiveGizmo.Target = null;
        }
    }

    /// <summary>
    /// Resets all gizmo handle sizes to 1.0.
    /// </summary>
    public void ResetAllGizmoScales()
    {
        _translationGizmo.HandleSize = 1.0f;
        _rotationGizmo.HandleSize = 1.0f;
        _scaleGizmo.HandleSize = 1.0f;
    }

    public void RemoveObject(object obj)
    {
        SelectionManager.ClearSelection();
        SelectionManager.ClearHover();

        if (ActiveGizmo != null)
        {
            ActiveGizmo.Target = null;
            SetActiveGizmoType(GizmoType.None);
        }

        if (StaticDragManager.DraggedObject == obj)
        {
            StaticDragManager.Clear();
        }
    }

    public void RemoveObjects(IEnumerable<object> objs)
    {
        foreach (var obj in objs)
        {
            RemoveObject(obj);
        }
    }

    /// <summary>
    /// Clears the current selection and deactivates any active gizmo.
    /// Should be called when loading or resetting scenes to prevent stale references.
    /// </summary>
    public void ClearSelectionAndGizmos()
    {
        // Clear selection first
        SelectionManager.ClearSelection();

        // Deactivate any active gizmo
        _editorState.Gizmo.ActiveGizmo = null;
        _editorState.Gizmo.ActiveGizmoType = GizmoType.None;
    }

    public void HandleInput(IInputProvider input, Vector2 viewportMousePos, Vector2i screenSize)
    {
        var camera = _cameraProvider();

        // When camera mode is active (cursor grabbed), use center of screen for ray-casting
        // since the mouse position doesn't move
        Vector2 raycastPos = input.GetCursorState() == CursorState.Grabbed
            ? new Vector2(screenSize.X / 2f, screenSize.Y / 2f)
            : viewportMousePos;

        // Update hover detection with the appropriate position
        SelectionManager.UpdateHover(raycastPos, screenSize);

        // The priority order for input handling is as follows:
        // 1. Active Dragging
        // 2. Active Gizmo Manipulation
        // 3. New Gizmo Interaction
        // 4. New Drag Operation
        // 5. Selection

        if (StaticDragManager.IsDragging)
        {
            StaticDragManager.HandleInput(input);
            return;
        }

        if (ActiveGizmo?.IsActive == true)
        {
            ActiveGizmo.HandleInput(input, raycastPos, camera, screenSize);
            return;
        }

        if (ActiveGizmo != null)
        {
            // Ensure the target is set if something is selected
            if (ActiveGizmo.Target == null && SelectionManager.SelectedObject != null)
            {
                UpdateActiveGizmoTarget();
            }

            bool gizmoConsumedInput = ActiveGizmo.HandleInput(input, raycastPos, camera, screenSize);
            if (gizmoConsumedInput)
            {
                return;
            }
        }

        if (StaticDragManager.Enabled)
        {
            bool dragStarted = StaticDragManager.HandleInput(input);
            if (dragStarted)
            {
                // clear selection and gizmo if we started dragging
                SelectionManager.ClearSelection();
                if (ActiveGizmo != null)
                {
                    ActiveGizmo.Target = null;
                }

                return;
            }
        }

        if (input.IsMouseButtonPressed(MouseButton.Left))
        {
            bool selectionChanged = SelectionManager.SelectHoveredObject();
            if (selectionChanged)
            {
                UpdateActiveGizmoTarget();
            }
        }
    }

    public void Dispose()
    {
        _translationGizmo.Dispose();
        _scaleGizmo.Dispose();
        _rotationGizmo.Dispose();
    }
}