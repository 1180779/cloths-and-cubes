using Engine;
using Engine.Collision.Bounding_Volume_Hierarchy;
using Engine.Rays;

using ImGuiNET;

using Visualisation.Core.Display.Cameras;
using Visualisation.Core.Display.Materials;
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
    private object? _selectedObject;
    private readonly IInputProvider _inputProvider = inputProvider;
    private readonly Func<CameraBase> _cameraProvider = cameraProvider;
    private readonly Func<BVH> _bvhProvider = bvhProvider;
    private readonly Func<Ray, int, (bool, Real, object?)> _testBvhIndexRayIntersection = testBvhIndexRayIntersection;
    private bool _debugRayDraw;
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

    public bool DrawInvisibleObjects;
    public bool DrawSelectedObjectWithoutDepthTesting = true;
    
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

        var viewProj = view * projection;
        var inverse = Matrix4.Invert(viewProj);
        vec = Vector4.TransformRow(vec, inverse);
        if (vec.W > 1e-6 || vec.W < -1e-6)
        {
            vec.X /= vec.W;
            vec.Y /= vec.W;
            vec.Z /= vec.W;
            vec.W = 1;
        }

        return new Vector3(vec.X, vec.Y, vec.Z);
    }

    /// <summary>
    /// Performs ray casting from the mouse position to detect object selection.
    /// </summary>
    private void PerformSelection(Vector2 mousePos, int screenWidth, int screenHeight)
    {
        var camera = _cameraProvider();
        var near = UnProject(new Vector3(mousePos.X, mousePos.Y, 0.0f), camera.ProjectionMatrix, camera.ViewMatrix,
            screenWidth, screenHeight);
        var far = UnProject(new Vector3(mousePos.X, mousePos.Y, 1.0f), camera.ProjectionMatrix, camera.ViewMatrix,
            screenWidth, screenHeight);
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

        SelectedObject = closestObject;
        SelectedObjectDistance = closestDistance;
    }

    public void DebugRenderInScene(Shader shader)
    {
        if (!_debugRayDraw)
            return;
        if (LastRay is not null && _debugRayRecreate)
        {
            _debugRayRecreate = false;
            var ray = LastRay.Value;
            var rayDirection = new Vector3(ray.Direction.X, ray.Direction.Y, ray.Direction.Z);
            var rayOriginInWorld = new Vector3(ray.Origin.X, ray.Origin.Y, ray.Origin.Z);
            Vector3 end = rayOriginInWorld + rayDirection * (float)SelectedObjectDistance;

            _debugRay?.Dispose();
            _debugRay = new Line(rayOriginInWorld, end);
        }

        shader.SetMatrix4("model", Matrix4.Identity);
        _debugRay?.Render();
    }

    public void DrawWindow()
    {
        ImGui.Begin("Selected Object");
        ImGui.Checkbox("Draw selection ray", ref _debugRayDraw);
        ImGui.Checkbox("Draw invisible objects", ref DrawInvisibleObjects);
        ImGui.Checkbox("Draw selected object even behind other objects", ref DrawSelectedObjectWithoutDepthTesting);
        ImGui.Separator();
        ImGui.Spacing();
        if (_selectedObject is GameObject gameObject)
        {
            ImGui.Checkbox("Invisible", ref gameObject.Invisible);
            DrawMaterialSelectors(gameObject);
        }

        switch (_selectedObject)
        {
            case Box box:
                DrawBox(box);
                break;
            case Ball ball:
                DrawSphere(ball);
                break;
            case Particle particle:
                DrawParticle(particle);
                break;
        }

        ImGui.End();
    }
    
    private void DrawMaterialSelectors(GameObject gameObject)
    {
        if (ImGui.CollapsingHeader("Constant Materials"))
        {
            var materials = MaterialsHelper.AllConstMaterials;
            foreach (var material in materials)
            {
                if (ImGui.Button(material.Name))
                {
                    gameObject.Material.Dispose();
                    gameObject.Material = material.TypedClone();
                }
            }
        }

        if (ImGui.CollapsingHeader("Textured Materials"))
        {
            var materials = MaterialsHelper.AllTexturedMaterials;
            foreach (var material in materials)
            {
                if (ImGui.Button(material.Name))
                {
                    gameObject.Material.Dispose();
                    gameObject.Material = material.TypedClone();
                }
            }
        }
    }

    private void DrawVector3(ref Engine.Vector3 vec, String label, float step = 0.1f)
    {
        var tempVec = new System.Numerics.Vector3(vec.X, vec.Y, vec.Z);
        if (ImGui.DragFloat3(label, ref tempVec, step))
        {
            vec.X = tempVec.X;
            vec.Y = tempVec.Y;
            vec.Z = tempVec.Z;
        }
    }

    private void DrawVector4(ref Engine.Quaternion qua, String label, float step = 0.1f)
    {
        var tempVec = new System.Numerics.Vector4(qua.I, qua.J, qua.K, qua.R);
        if (ImGui.DragFloat4(label, ref tempVec, step))
        {
            qua.I = tempVec.X;
            qua.J = tempVec.Y;
            qua.K = tempVec.Z;
            qua.R = tempVec.W;
        }
    }

    private void DrawRigidBody(Engine.RigidBodies.RigidBody body)
    {
        DrawVector3(ref body.Position, "Position");
        DrawVector3(ref body.Velocity, "Velocity");
        DrawVector3(ref body.Rotation, "Rotation");
        DrawVector3(ref body.Acceleration, "Acceleration");
        DrawVector4(ref body.OrientationRef, "Orientation", 0.02f);
    }

    private void DrawBox(Box box)
    {
        DrawRigidBody(box.EngineBox.Body);
    }

    private void DrawSphere(Ball ball)
    {
        DrawRigidBody(ball.EngineBall.Body);
    }

    private void DrawParticle(Particle particle)
    {
        // TODO: implement
    }

    public record State(bool DrawInvisibleObjects, bool DrawDebugRay, bool DrawSelectedObjectWithoutDepthTesting);

    public State SaveState()
    {
        return new State(DrawInvisibleObjects, _debugRayDraw, DrawSelectedObjectWithoutDepthTesting);
    }

    public void RestoreState(State state)
    {
        _debugRayDraw = state.DrawDebugRay;
        DrawInvisibleObjects = state.DrawInvisibleObjects;
        DrawSelectedObjectWithoutDepthTesting = state.DrawSelectedObjectWithoutDepthTesting;
    }
}