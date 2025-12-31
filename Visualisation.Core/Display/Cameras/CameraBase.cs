using Visualisation.Core.Inputs;

namespace Visualisation.Core.Display.Cameras;

public class CameraBase
{
    protected const float CameraSpeed = 15f;
    protected const float Sensitivity = 0.2f;
    public static float AspectRatio { get; set; }

    protected Vector3 _front = -Vector3.UnitZ;
    protected Vector3 _up = Vector3.UnitY;
    protected Vector3 _right = Vector3.UnitX;

    protected float _pitch;
    protected float _yaw = -MathHelper.PiOver2;
    protected float _fov = MathHelper.PiOver2;

    public CameraBase()
    {
    }

    public CameraBase(Vector3 position)
    {
        Position = position;
    }

    public Vector3 Position { get; set; } = new(0, 0, 0);

    public Vector3 Front => _front;
    public Vector3 Up => _up;
    public Vector3 Right => _right;

    public float NearPlane => 0.01f;
    public float FarPlane => 100f;

    public float PitchDegrees
    {
        get => MathHelper.RadiansToDegrees(_pitch);
        set
        {
            var angle = MathHelper.Clamp(value, -89f, 89f);
            _pitch = MathHelper.DegreesToRadians(angle);
            UpdateVectors();
        }
    }

    public float YawDegrees
    {
        get => MathHelper.RadiansToDegrees(_yaw);
        set
        {
            _yaw = MathHelper.DegreesToRadians(value);
            UpdateVectors();
        }
    }

    public float FovRadians
    {
        get => _fov;
        set
        {
            var angleRadians = MathHelper.Clamp(value, 1f, MathHelper.PiOver2);
            _fov = angleRadians;
        }
    }

    public float FovDegrees
    {
        get => MathHelper.RadiansToDegrees(_fov);
        set
        {
            var angle = MathHelper.Clamp(value, 1f, 90f);
            _fov = MathHelper.DegreesToRadians(angle);
        }
    }

    public Matrix4 ViewMatrix => Matrix4.LookAt(Position, Position + _front, _up);

    public Matrix4 ProjectionMatrix => Matrix4.CreatePerspectiveFieldOfView(_fov, AspectRatio, NearPlane, FarPlane);

    public virtual void ProcessInput(IInputProvider input, float dt)
    {
    }

    private void UpdateVectors()
    {
        _front.X = MathF.Cos(_pitch) * MathF.Cos(_yaw);
        _front.Y = MathF.Sin(_pitch);
        _front.Z = MathF.Cos(_pitch) * MathF.Sin(_yaw);

        _front = Vector3.Normalize(_front);

        _right = Vector3.Normalize(Vector3.Cross(_front, Vector3.UnitY));
        _up = Vector3.Normalize(Vector3.Cross(_right, _front));
    }

    public void SetForPbrShader(Shader sh)
    {
        sh.SetVector3("viewPos", Position);
        sh.SetMatrix4("view", ViewMatrix);
        sh.SetMatrix4("projection", ProjectionMatrix);
        sh.SetFloat("farPlane", FarPlane);
    }

    public void SetForSimpleShader(Shader sh)
    {
        sh.SetMatrix4("view", ViewMatrix);
        sh.SetMatrix4("projection", ProjectionMatrix);
    }
}