namespace Visualisation.Core.Display.Cameras;

/// <summary>
/// An orbit camera for the direction control that orbits around the origin.
/// </summary>
public sealed class OrbitCamera : CameraBase
{
    private readonly float _distance;
    private readonly float _aspectRatio;

    public OrbitCamera(float distance, float aspectRatio)
    {
        _distance = distance;
        _aspectRatio = aspectRatio;
        YawDegrees = 0;
    }

    public override Matrix4 ProjectionMatrix =>
        Matrix4.CreatePerspectiveFieldOfView(_fovRadians, _aspectRatio, NearPlane, FarPlane);

    protected override void UpdateVectors()
    {
        Position = new Vector3(
            _distance * MathF.Cos(_pitchRadians) * MathF.Sin(_yawRadians),
            _distance * MathF.Sin(_pitchRadians),
            _distance * MathF.Cos(_pitchRadians) * MathF.Cos(_yawRadians)
        );

        _front = (-Position).Normalized();
        _right = Vector3.Cross(_front, Vector3.UnitY).Normalized();
        _up = Vector3.Cross(_right, _front).Normalized();
    }
}