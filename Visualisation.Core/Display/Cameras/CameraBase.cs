using Visualisation.Core.Inputs;

namespace Visualisation.Core.Display.Cameras;

public class CameraBase
{
    protected const float CameraSpeed = 15f;
    protected const float Sensitivity = 0.2f;

    private static float _aspectRatio = 16f / 9f;

    public static float AspectRatio
    {
        get => _aspectRatio;
        set
        {
            if (value > 0.0f)
            {
                _aspectRatio = value;
            }
        }
    }

    protected Vector3 _front = -Vector3.UnitZ;
    protected Vector3 _up = Vector3.UnitY;
    protected Vector3 _right = Vector3.UnitX;

    protected float _pitchRadians; // Pitch in radians
    protected float _yawRadians = -MathHelper.PiOver2; // Yaw in radians, initialized to -90 degrees to look along -Z
    protected float _fovRadians = MathHelper.PiOver2; // FOV in radians, initialized to 90 degrees

    public Quaternion GetOrientation() => Quaternion.FromEulerAngles(PitchRadians, YawRadians, 0f);

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

    // Pitch limit in degrees
    protected float _pitchLimitRadians = 80f * MathF.PI / 180f;
    protected float _pitchLimitDegrees = 80f;

    public float PitchLimitDegrees
    {
        get => _pitchLimitDegrees;
        set
        {
            _pitchLimitDegrees = MathHelper.Clamp(value, 0f, 89.9f);
            _pitchLimitRadians = MathHelper.DegreesToRadians(_pitchLimitDegrees);
        }
    }

    public float PitchLimitRadians
    {
        get => _pitchLimitRadians;
        set
        {
            _pitchLimitDegrees = MathHelper.Clamp(MathHelper.RadiansToDegrees(value), 0f, 89.9f);
            _pitchLimitRadians = MathHelper.DegreesToRadians(_pitchLimitDegrees);
        }
    }

    public float PitchRadians
    {
        get => _pitchRadians;
        set
        {
            _pitchRadians = MathHelper.Clamp(value, -PitchLimitRadians, PitchLimitRadians);
            UpdateVectors();
        }
    }

    public float PitchDegrees
    {
        get => MathHelper.RadiansToDegrees(_pitchRadians);
        set
        {
            var angle = MathHelper.Clamp(value, -PitchLimitDegrees, PitchLimitDegrees);
            _pitchRadians = MathHelper.DegreesToRadians(angle);
            UpdateVectors();
        }
    }

    public float YawRadians
    {
        get => _yawRadians;
        set
        {
            _yawRadians = value;
            UpdateVectors();
        }
    }

    public float YawDegrees
    {
        get => MathHelper.RadiansToDegrees(_yawRadians);
        set
        {
            _yawRadians = MathHelper.DegreesToRadians(value);
            UpdateVectors();
        }
    }

    public float FovRadians
    {
        get => _fovRadians;
        set
        {
            var angleRadians = MathHelper.Clamp(value, 1f, MathHelper.PiOver2);
            _fovRadians = angleRadians;
        }
    }

    public float FovDegrees
    {
        get => MathHelper.RadiansToDegrees(_fovRadians);
        set
        {
            var angle = MathHelper.Clamp(value, 1f, 90f);
            _fovRadians = MathHelper.DegreesToRadians(angle);
        }
    }

    public virtual Matrix4 ViewMatrix => Matrix4.LookAt(Position, Position + _front, _up);

    public virtual Matrix4 ProjectionMatrix =>
        Matrix4.CreatePerspectiveFieldOfView(_fovRadians, AspectRatio, NearPlane, FarPlane);

    public virtual void ProcessInput(IInputProvider input, float dt)
    {
    }

    protected virtual void UpdateVectors()
    {
        _front.X = MathF.Cos(_pitchRadians) * MathF.Cos(_yawRadians);
        _front.Y = MathF.Sin(_pitchRadians);
        _front.Z = MathF.Cos(_pitchRadians) * MathF.Sin(_yawRadians);

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