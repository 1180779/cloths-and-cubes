using Engine.Collision.Bounding_Volume_Hierarchy;
using Engine.Rays;

using OpenTK.Mathematics;

using Visualisation.Core.Display.Cameras;
using Visualisation.Core.Inputs;

namespace Visualisation.Core.GameObjects;

/// <summary>
/// Manages object selection via mouse ray-casting.
/// Handles converting mouse coordinates to world-space rays and detecting intersections.
/// </summary>
public sealed class SelectionManager(
    IInputProvider inputProvider,
    Func<CameraBase> cameraProvider,
    Func<BVH> bvhProvider,
    Func<Ray, int, (bool, Real, object?)> testBvhIndexRayIntersection
)
{
    public bool IsEnabled = true;

    private object? _selectedObject;
    private readonly IInputProvider _inputProvider = inputProvider;
    private readonly Func<CameraBase> _cameraProvider = cameraProvider;
    private readonly Func<BVH> _bvhProvider = bvhProvider;
    private readonly Func<Ray, int, (bool, Real, object?)> _testBvhIndexRayIntersection = testBvhIndexRayIntersection;
    public Ray? LastRay { get; private set; }
    public Real SelectedObjectDistance { get; set; }

    /// <summary>
    /// The object currently under the mouse cursor (updated every frame).
    /// </summary>
    public object? HoveredObject { get; private set; }

    /// <summary>
    /// The explicitly selected object (selected for gizmo manipulation).
    /// </summary>
    public object? SelectedObject
    {
        get => _selectedObject;
        set
        {
            if (!_selectionEnabled)
                return;
            if (_selectedObject != value)
            {
                _selectedObject = value;
                OnSelectionChanged?.Invoke(_selectedObject);
            }
        }
    }

    public bool Unselect = true;
    private bool _selectionEnabled = true;

    public bool GizmosEnabled;

    public bool SelectionEnabled
    {
        get => _selectionEnabled;
        set
        {
            if (!value)
            {
                SelectedObject = null;
            }

            _selectionEnabled = value;
        }
    }

    public bool DrawInvisibleObjects;
    public bool DrawSelectedObjectWithoutDepthTesting = true;

    /// <summary>
    /// Event raised when the selected object changes.
    /// </summary>
    public event Action<object?>? OnSelectionChanged;

    /// <summary>
    /// Updates which object is currently hovered by the mouse cursor.
    /// This should be called every frame to keep the hover state current.
    /// </summary>
    /// <param name="viewportMousePos">The mouse position relative to the viewport.</param>
    /// <param name="screenSize">The dimensions of the viewport in framebuffer coordinates</param>
    public void UpdateHover(Vector2 viewportMousePos, Vector2i screenSize)
    {
        if (!IsEnabled)
        {
            HoveredObject = null;
            return;
        }

        PerformHoverDetection(viewportMousePos, screenSize);
    }

    /// <summary>
    /// Selects the currently hovered object (if any).
    /// Returns true if an object was selected.
    /// </summary>
    public bool SelectHoveredObject()
    {
        if (HoveredObject != null)
        {
            SelectedObject = HoveredObject;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Creates a ray in world space based on the supplied mouse position, camera view, and screen dimensions.
    /// The ray originates from the camera and points in the direction corresponding to the mouse position.
    /// The ray direction is calculated based on an assumption that the ray passes though the near and far planes
    /// of the camera's view frustum. 
    /// </summary>
    /// <param name="mousePos">
    /// A <c>Vector2</c> that is the position of the mouse on the screen, where the X and Y
    /// values correspond to the pixel coordinates of the mouse.
    /// </param>
    /// <param name="camera">
    /// A <c>CameraBase</c> used for the calculation. Provides the camera's view matrix and projection matrix,
    /// necessary for conversions.
    /// </param>
    /// <param name="screenSize">
    /// A <c>Vector2i</c> with dimensions of the viewport or screen in pixels.
    /// </param>
    /// <returns>
    /// <c>Ray</c> representing the calculated ray in world space. The ray has an origin and a
    /// direction vector that can be used for intersection tests in 3D space.
    /// </returns>
    public static Ray GetRayFromMouse(Vector2 mousePos, CameraBase camera, Vector2i screenSize)
    {
        // Correct the Y mouse coordinate for openGL
        mousePos.Y = screenSize.Y - mousePos.Y;

        var inverse = Matrix4.Invert(camera.ViewMatrix * camera.ProjectionMatrix);
        Vector3 near = Vector3.Unproject(new Vector3(mousePos.X, mousePos.Y, 0.0f), 0, 0, screenSize.X, screenSize.Y,
            0.0f, 1.0f, inverse);
        Vector3 far = Vector3.Unproject(new Vector3(mousePos.X, mousePos.Y, 1.0f), 0, 0, screenSize.X, screenSize.Y,
            0.0f, 1.0f, inverse);

        var dir = far - near;
        dir.Normalize();

        return new Ray(near.ToEngine(), dir.ToEngine());
    }

    /// <summary>
    /// Performs ray casting from the mouse position to detect which object is under the cursor.
    /// Updates HoveredObject but does not change selection.
    /// </summary>
    private void PerformHoverDetection(Vector2 mousePos, Vector2i screenSize)
    {
        var ray = GetRayFromMouse(mousePos, _cameraProvider(), screenSize);
        LastRay = ray;

        object? closestObject = null;
        var closestDistance = Real.MaxValue;
        var bvh = _bvhProvider();
        var potentialHits = new List<int>();
        RayIntersection.TraverseBVHForRay(ray, bvh.root, ref potentialHits);

        foreach (var hitIndex in potentialHits)
        {
            var (hit, distance, obj) = _testBvhIndexRayIntersection(ray, hitIndex);
            if (hit && distance < closestDistance && distance >= 0)
            {
                closestDistance = distance;
                closestObject = obj;
            }
        }

        var (planeHit, planeDistance, planeObj) = _testBvhIndexRayIntersection(ray, -1);
        if (planeHit && planeDistance < closestDistance && planeDistance >= 0)
        {
            closestDistance = planeDistance;
            closestObject = planeObj;
        }

        HoveredObject = closestObject;
        SelectedObjectDistance = closestDistance;
    }

    /// <summary>
    /// Clears the current selection and resets the selection state.
    /// </summary>
    public void ClearSelection()
    {
        SelectedObject = null;
        LastRay = null;
        SelectedObjectDistance = 0;
    }
}