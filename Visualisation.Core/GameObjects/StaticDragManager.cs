using System.Diagnostics;

using Visualisation.Core.Display.Cameras;
using Visualisation.Core.Display.Gizmos.Translation;
using Visualisation.Core.Inputs;

namespace Visualisation.Core.GameObjects;

public sealed class StaticDragManager(Func<object?> getHoveredObject, Func<CameraBase> cameraProvider)
{
    // public bool UseSpring = false;
    // public Real Stiffness = (Real)50.0;

    public float Sensitivity = 1.0f;
    public bool Enabled = false;
    public MouseButton DragButton = MouseButton.Left;
    public float MinOffset = 1.0f;
    public float MaxOffset = 1000_000.0f;

    public bool IsDragging => DraggedObject != null;

    /// <summary>
    /// Event fired when an object's position is updated during dragging.
    /// This allows external systems (like BVH) to update accordingly.
    /// </summary>
    public event Action? OnObjectDragged;

    /// <summary>
    /// The object that will be dragged if the user clicks (hover target).
    /// Null if no valid object is under the cursor.
    /// </summary>
    public object? HoverTarget => _getHoveredObject();

    /// <summary>
    /// Color for the hover indicator. Default is semi-transparent cyan.
    /// </summary>
    public Vector4 HoverIndicatorColor { get; set; } = new Vector4(0.0f, 1.0f, 1.0f, 0.6f);

    /// <summary>
    /// Size of the hover indicator relative to the object. Default is 1.02 (2% larger).
    /// </summary>
    public float HoverIndicatorScale { get; set; } = 1.02f;

    /// <summary>
    /// Whether to show the hover indicator when enabled and a valid object is under cursor.
    /// </summary>
    public bool ShowHoverIndicator { get; set; } = true;

    public ITranslationGizmoTarget? DraggedObject { get; private set; }

    private Func<object?> _getHoveredObject = getHoveredObject;
    private Func<CameraBase> _cameraProvider = cameraProvider;

    /// <summary>
    /// Distance from the camera in the camera front direction which determines the target location
    /// </summary>
    private float _planeOffset = 0.1f;

    /// <summary>
    /// Handle input for dragging. 
    /// </summary>
    /// <param name="inputProvider"></param>
    /// <returns>Returns true if the method captured mouse input. False otherwise. </returns>
    public bool HandleInput(IInputProvider inputProvider)
    {
        if (!Enabled)
            return false;

        bool isButtonDown = inputProvider.IsMouseButtonDown(DragButton);
        if (IsDragging && !isButtonDown)
        {
            DraggedObject = null;
            return false;
        }

        if (!isButtonDown)
            return false;

        var camera = _cameraProvider();
        if (!IsDragging)
        {
            var hoveredObject = HoverTarget;
            if (hoveredObject is ITranslationGizmoTarget translationGizmoTarget && hoveredObject is not Plane)
            {
                DraggedObject = translationGizmoTarget;
                _planeOffset = Vector3.Dot(camera.Front, DraggedObject.Position - camera.Position);
                return true;
            }

            DraggedObject = null;
            return false;
        }

        Debug.Assert(DraggedObject != null);

        var scroll = inputProvider.GetMouseScroll();

        _planeOffset += Sensitivity * scroll;
        _planeOffset = Math.Clamp(_planeOffset, Math.Max(camera.NearPlane, MinOffset),
            Math.Min(camera.FarPlane, MaxOffset));
        DraggedObject.Position = camera.Position + camera.Front * _planeOffset;
        OnObjectDragged?.Invoke();
        return true;
    }
}