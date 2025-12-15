using Engine.Collision.Bounding_Volume_Hierarchy;
using Engine.Rays;

using Visualisation.Core.Display.Cameras;
using Visualisation.Core.Inputs;

namespace Visualisation.Core.GameObjects;

/// <summary>
/// Manages object selection via mouse raycasting.
/// Handles converting mouse coordinates to world-space rays and detecting intersections.
/// </summary>
public sealed class SelectionManager(IInputProvider inputProvider, Func<CameraBase> cameraProvider, Func<BVH> bvhProvider, Func<Ray, int, (bool, Real, object?)> testBvhIndexRayIntersection)
{
    private object? _selectedObject;
    private readonly IInputProvider _inputProvider = inputProvider;
    private readonly Func<CameraBase> _cameraProvider = cameraProvider;
    private readonly Func<BVH> _bvhProvider = bvhProvider;
    private readonly Func<Ray, int, (bool, Real, object?)> _testBvhIndexRayIntersection = testBvhIndexRayIntersection;
    
    private Line? _debugRay;
    private bool _debugRayRecreate;
    public Ray? LastRay { get; private set; }

    public Real SelectedObjectDistance { get; set; }
    public object? SelectedObject
    {
        get
        {
            return _selectedObject;
        }

        private set
        {
            if (_selectedObject != value)
            {
                _selectedObject = value;
                OnSelectionChanged?.Invoke(_selectedObject);
            }
        }
    }


    /// <summary>
    /// Event raised when the selected object changes.
    /// </summary>
    public event Action<object?>? OnSelectionChanged;

    /// <summary>
    /// Updates selection based on mouse input. 
    /// </summary>
    /// <param name="viewportMousePos">The mouse position relative to the viewport.</param>
    /// <param name="viewportWidth">The width of the viewport in framebuffer coordinates.</param>
    /// <param name="viewportHeight">The height of the viewport in framebuffer coordinates.</param>
    public void HandleInput(Vector2 viewportMousePos, int viewportWidth, int viewportHeight)
    {
        if (_inputProvider.IsMouseButtonPressed(MouseButton.Left))
        {
            PerformSelection(viewportMousePos, viewportWidth, viewportHeight);
        }
    }

    private static Vector3 UnProject(Vector3 mouse, Matrix4 projection, Matrix4 view, float width, float height)
    {
        Vector4 vec;

        vec.X = 2.0f * mouse.X / width - 1.0f;
        vec.Y = 1.0f - 2.0f * mouse.Y / height;
        vec.Z = mouse.Z;
        vec.W = 1.0f;

        // In OpenTK, the combined matrix for view-projection is view * proj
        var viewProj = view * projection;
        var inverse = Matrix4.Invert(viewProj);

        // Use TransformRow for correct row-major vector-matrix multiplication
        vec = Vector4.TransformRow(vec, inverse);

        if (vec.W > 1e-6 || vec.W < -1e-6)
        {
            vec.X /= vec.W;
            vec.Y /= vec.W;
            vec.Z /= vec.W;
        }

        return new Vector3(vec.X, vec.Y, vec.Z);
    }

    /// <summary>
    /// Performs raycasting from the mouse position to detect object selection.
    /// </summary>
    private void PerformSelection(Vector2 mousePos, int screenWidth, int screenHeight)
    {
        var camera = _cameraProvider();

        var near = UnProject(new Vector3(mousePos.X, mousePos.Y, 0.0f), camera.ProjectionMatrix, camera.ViewMatrix, screenWidth, screenHeight);
        var far = UnProject(new Vector3(mousePos.X, mousePos.Y, 1.0f), camera.ProjectionMatrix, camera.ViewMatrix, screenWidth, screenHeight);

        var engineRayOrigin = new Engine.Vector3(
            (Real)near.X,
            (Real)near.Y,
            (Real)near.Z
        );

        var rayDirection = far - near;
        rayDirection.Normalize();

        var engineRayDirection = new Engine.Vector3(
            (Real)rayDirection.X,
            (Real)rayDirection.Y,
            (Real)rayDirection.Z
        );

        var ray = new Ray(engineRayOrigin, engineRayDirection);
        LastRay = ray;
        _debugRayRecreate = true;

        // Find the closest intersecting object
        object? closestObject = null;
        var closestDistance = Real.MaxValue;

        var bvh = _bvhProvider();

        var potentialHits = new List<int>();
        RayIntersection.TraverseBVHForRay(ray, bvh.root, ref potentialHits);

        // Test detailed intersection with each potential hit
        foreach (var hitIndex in potentialHits)
        {
            var (hit, distance, obj) = _testBvhIndexRayIntersection(ray, hitIndex);

            if (hit && distance < closestDistance && distance >= 0)
            {
                closestDistance = distance;
                closestObject = obj;
            }
        }

        SelectedObject = closestObject;
        SelectedObjectDistance = closestDistance;
    }
    
    public void DebugRenderInScene()
    {
        if (LastRay is not null && _debugRayRecreate)
        {
            _debugRayRecreate = false;
            var ray = LastRay.Value;
            var start = new Vector3(ray.Origin.X, ray.Origin.Y, ray.Origin.Z);
            var end = start + new Vector3(ray.Direction.X, ray.Direction.Y, ray.Direction.Z) * SelectedObjectDistance;
            _debugRay?.Dispose();
            _debugRay = new Line(start, end);
        }
        _debugRay?.Render();
    }
}