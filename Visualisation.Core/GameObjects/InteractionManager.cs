using Engine.Collision.Bounding_Volume_Hierarchy;

using OpenTK.Mathematics;

using Visualisation.Core.Display.Cameras;
using Visualisation.Core.Display.Gizmos;
using Visualisation.Core.Display.Gizmos.Rotation;
using Visualisation.Core.Display.Gizmos.Scale;
using Visualisation.Core.Display.Gizmos.Translation;
using Visualisation.Core.Inputs;

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
    private IGizmo? _activeGizmo;

    private Func<CameraBase> _cameraProvider;

    public InteractionManager(
        Shader shader,
        IInputProvider inputProvider,
        Func<CameraBase> cameraProvider,
        Func<BVH> bvhProvider,
        Func<Dictionary<int, IBoxable>> bvhDictionaryProvider,
        Func<Dictionary<Engine.Cloth, Cloth>> clothsProvider,
        Func<Plane> planeProvider,
        Func<float> positionEpsilonProvider)
    {
        _translationGizmo = new TranslationGizmo(shader);
        _scaleGizmo = new ScaleGizmo(shader);
        _rotationGizmo = new RotationGizmo(shader);

        _cameraProvider = cameraProvider;

        StaticDragManager = new(() => SelectionManager?.HoveredObject ?? null, cameraProvider);
        SelectionManager = new(
            inputProvider,
            cameraProvider,
            bvhProvider,
            bvhDictionaryProvider,
            clothsProvider,
            planeProvider,
            positionEpsilonProvider);
    }

    public SelectionManager SelectionManager { get; set; }
    public StaticDragManager StaticDragManager { get; }

    public IGizmo? ActiveGizmo => _activeGizmo;

    public GizmoType ActiveGizmoType
    {
        get
        {
            return ActiveGizmo switch
            {
                Display.Gizmos.Translation.TranslationGizmo => GizmoType.Translation,
                Display.Gizmos.Rotation.RotationGizmo => GizmoType.Rotation,
                Display.Gizmos.Scale.ScaleGizmo => GizmoType.Scale,
                _ => GizmoType.None
            };
        }
    }

    public TranslationGizmo TranslationGizmo => _translationGizmo;
    public RotationGizmo RotationGizmo => _rotationGizmo;
    public ScaleGizmo ScaleGizmo => _scaleGizmo;

    /// <summary>
    /// Sets the active gizmo type based on enum value.
    /// </summary>
    public void SetActiveGizmoType(GizmoType gizmoType)
    {
        _activeGizmo = gizmoType switch
        {
            GizmoType.None => null,
            GizmoType.Translation => _translationGizmo,
            GizmoType.Rotation => _rotationGizmo,
            GizmoType.Scale => _scaleGizmo,
            _ => null
        };

        if (_activeGizmo is not null && SelectionManager?.SelectedObject is IGizmoTarget gizmoTarget)
        {
            _activeGizmo.Target = gizmoTarget;
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
        if (ActiveGizmo?.Target == obj)
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
        // Clear selection first (this will also trigger OnSelectionChanged event)
        SelectionManager.ClearSelection();

        // Deactivate any active gizmo
        _activeGizmo = null;
    }

    public void HandleInput(IInputProvider input, Vector2 viewportMousePos, Vector2i screenSize)
    {
        var camera = _cameraProvider();

        // TODO: refactor this check
        //  Only the drag or the selection should be available and not both at the same time
        //  The gizmos should only be available when selection is enabled

        // When camera mode is active (cursor grabbed), use center of screen for raycasting
        // since the mouse position doesn't move
        Vector2 raycastPos = input.GetCursorState() == CursorState.Grabbed
            ? new Vector2(screenSize.X / 2f, screenSize.Y / 2f)
            : viewportMousePos;

        // Update hover detection with the appropriate position
        SelectionManager?.UpdateHover(raycastPos, screenSize);

        // Priority system: Once an input handler starts an operation (gizmo drag, mouse drag),
        // it maintains priority until the operation completes (mouse button released)

        // Check if gizmo is already active (the highest priority when active)
        if (!(SelectionManager?.GizmosEnabled ?? false) && ActiveGizmo?.Target is not null)
        {
            ActiveGizmo.Target = null;
            SetActiveGizmoType(GizmoType.None);
        }

        if (((SelectionManager?.GizmosEnabled ?? false) && (ActiveGizmo?.IsActive ?? false)) &&
            ActiveGizmo is not null)
        {
            ActiveGizmo.HandleInput(input, raycastPos, camera, screenSize);
            return;
        }

        if (StaticDragManager.Enabled == true &&
            StaticDragManager.ShowHoverIndicator &&
            SelectionManager?.HoveredObject is not null &&
            ActiveGizmo is not null &&
            (ActiveGizmo.Target is not null || SelectionManager.SelectedObject is not null))
        {
            SelectionManager.ClearSelection();
            ActiveGizmo.Target = null;
        }

        // Check if the drag manager is already active
        if (StaticDragManager.IsDragging == true)
        {
            StaticDragManager.HandleInput(input);
            return;
        }

        // Check gizmo first (the highest priority for new operations)
        bool gizmoTookMouseInput = false;
        if ((SelectionManager?.GizmosEnabled ?? false) && ActiveGizmo is not null)
        {
            ActiveGizmo.Target = (IGizmoTarget?)SelectionManager?.SelectedObject;
            gizmoTookMouseInput = ActiveGizmo?.HandleInput(input, raycastPos, camera, screenSize) ?? false;
        }

        // Attempt to select hovered object if gizmo didn't take input
        _ = !gizmoTookMouseInput && input.IsMouseButtonPressed(MouseButton.Left) &&
            (SelectionManager?.SelectHoveredObject() ?? false);

        // Check if the drag manager can use the hovered object
        if (!gizmoTookMouseInput && StaticDragManager.Enabled == true)
        {
            bool dragTookMouseInput = StaticDragManager.HandleInput(input);
            if (dragTookMouseInput)
            {
                SelectionManager?.ClearSelection();
                if (ActiveGizmo is not null)
                {
                    ActiveGizmo.Target = null;
                }

                return;
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