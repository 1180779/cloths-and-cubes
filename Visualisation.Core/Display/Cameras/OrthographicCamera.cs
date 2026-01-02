namespace Visualisation.Core.Display.Cameras;

/// <summary>
/// An orthographic camera for planar views.
///
/// Looks in the specified forward direction from the given position.
/// </summary>
public sealed class OrthographicCamera : CameraBase
{
    private readonly Vector3 _lookAt;
    public new float AspectRatio { get; init; }
    public float Size { get; init; } = 3.5f;

    public OrthographicCamera(Vector3 position, Vector3 forward, Vector3 up, float aspectRatio)
    {
        Position = position;
        _lookAt = position + forward;
        _up = up;
        AspectRatio = aspectRatio;

        _front = forward.Normalized();
        _right = Vector3.Cross(_front, up).Normalized();
        // Recalculate up to ensure orthogonality
        _up = Vector3.Cross(_right, _front).Normalized();
    }

    public override Matrix4 ProjectionMatrix =>
        Matrix4.CreateOrthographic(Size * AspectRatio, Size, NearPlane, FarPlane);
}