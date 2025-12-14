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
    /// <param name="viewportMousePos">The mouse position relative to the viewport (not the window).
    /// This should be calculated by subtracting the viewport's top-left position from the global mouse position,
    /// then scaled by the framebuffer scale factor.</param>
    /// <param name="viewportWidth">The width of the viewport in framebuffer coordinates.</param>
    /// <param name="viewportHeight">The height of the viewport in framebuffer coordinates.</param>
    /// <remarks>
    /// IMPORTANT: viewportMousePos must be relative to the viewport window, not the application window.
    /// See Application.cs RenderSceneWindow() for the correct calculation using ImGui.GetCursorScreenPos().
    /// </remarks>
    public void HandleInput(Vector2 viewportMousePos, int viewportWidth, int viewportHeight)
    {
        if (_inputProvider.IsMouseButtonPressed(MouseButton.Left))
        {
            PerformSelection(viewportMousePos, viewportWidth, viewportHeight);
        }
    }

    /// <summary>
    /// Performs raycasting from the mouse position to detect object selection.
    /// </summary>
    private void PerformSelection(Vector2 mousePos, int screenWidth, int screenHeight)
    {
        // Convert mouse coordinates to normalized device coordinates (NDC)
        // Mouse coords are typically top-left origin, OpenGL is bottom-left
        float x = (2.0f * mousePos.X) / screenWidth - 1.0f;
        float y = 1.0f - (2.0f * mousePos.Y) / screenHeight;

        // Create ray in clip space
        Vector4 rayClip = new(x, y, -1.0f, 1.0f);

        var camera = _cameraProvider();
        
        // Transform to view space
        Matrix4 invProjection = Matrix4.Invert(camera.ProjectionMatrix);
        Vector4 rayEye = rayClip * invProjection;
        rayEye = new Vector4(rayEye.X, rayEye.Y, -1.0f, 0.0f);

        // Transform to world space
        Matrix4 invView = Matrix4.Invert(camera.ViewMatrix);
        Vector4 rayWorld4 = rayEye * invView;
        Vector3 rayDirection = new Vector3(rayWorld4.X, rayWorld4.Y, rayWorld4.Z);
        rayDirection = Vector3.Normalize(rayDirection);

        // Create the ray
        var engineRayOrigin = new Engine.Vector3(
            (Real)camera.Position.X,
            (Real)camera.Position.Y,
            (Real)camera.Position.Z
        );

        var engineRayDirection = new Engine.Vector3(
            (Real)rayDirection.X,
            (Real)rayDirection.Y,
            (Real)rayDirection.Z
        );

        Ray ray = new Ray(engineRayOrigin, engineRayDirection);

        // Find the closest intersecting object
        object? closestObject = null;
        Real closestDistance = Real.MaxValue;

        BVH bvh = _bvhProvider();

        List<int> potentialHits = new();
        RayIntersection.TraverseBVHForRay(ray, bvh.root, ref potentialHits);

        // Test detailed intersection with each potential hit
        foreach (int hitIndex in potentialHits)
        {
            var (hit, distance, obj) = _testBvhIndexRayIntersection(ray, hitIndex);
            
            if (hit && distance < closestDistance && distance >= 0)
            {
                closestDistance = distance;
                closestObject = obj;
            }
        }

        SelectedObject = closestObject;
    }
}