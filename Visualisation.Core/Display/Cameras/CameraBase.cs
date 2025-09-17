using Visualisation.Core.Inputs;

namespace Visualisation.Core.Display.Cameras;

public class CameraBase
{
    protected const float CameraSpeed = 15f;
    protected const float Sensitivity = 0.2f;

    protected Vector3 front = -Vector3.UnitZ;
    protected Vector3 up = Vector3.UnitY;
    protected Vector3 right = Vector3.UnitX;

    protected float pitch;
    protected float yaw = -MathHelper.PiOver2;
    protected float fov = MathHelper.PiOver2;

    public CameraBase(float aspectRatio)
    {
        AspectRatio = aspectRatio;
    }

    public CameraBase(Vector3 position, float aspectRatio)
    {
        Position = position;
        AspectRatio = aspectRatio;
    }

    public Vector3 Position { get; set; } = new Vector3(0, 0, 0);
    public float AspectRatio { get; set; }

    public Vector3 Front => front;
    public Vector3 Up => up;
    public Vector3 Right => right;

    public float NearPlane => 0.01f;
    public float FarPlane => 100f;

    public float PitchDegrees
    {
        get => MathHelper.RadiansToDegrees(pitch);
        set
        {
            var angle = MathHelper.Clamp(value, -89f, 89f);
            pitch = MathHelper.DegreesToRadians(angle);
            UpdateVectors();
        }
    }

    public float YawDegrees
    {
        get => MathHelper.RadiansToDegrees(yaw);
        set
        {
            yaw = MathHelper.DegreesToRadians(value);
            UpdateVectors();
        }
    }

    public float FovRadians
    {
        get => fov;
        set
        {
            var angleRadians = MathHelper.Clamp(value, 1f, MathHelper.PiOver2);
            fov = angleRadians;
        }
    }

    public float FovDegrees
    {
        get => MathHelper.RadiansToDegrees(fov);
        set
        {
            var angle = MathHelper.Clamp(value, 1f, 90f);
            fov = MathHelper.DegreesToRadians(angle);
        }
    }

    public Matrix4 ViewMatrix => Matrix4.LookAt(Position, Position + front, up);

    public Matrix4 ProjectionMatrix => Matrix4.CreatePerspectiveFieldOfView(fov, AspectRatio, NearPlane, FarPlane);

    public virtual void ProcessInput(IInputProvider input, float dt)
    {
    }

    private void UpdateVectors()
    {
        front.X = MathF.Cos(pitch) * MathF.Cos(yaw);
        front.Y = MathF.Sin(pitch);
        front.Z = MathF.Cos(pitch) * MathF.Sin(yaw);

        front = Vector3.Normalize(front);

        right = Vector3.Normalize(Vector3.Cross(front, Vector3.UnitY));
        up = Vector3.Normalize(Vector3.Cross(right, front));
    }

    public void SetForShader(Shader sh)
    {
        sh.SetVector3("viewPos", Position);
        sh.SetMatrix4("view", ViewMatrix);
        sh.SetMatrix4("projection", ProjectionMatrix);
        sh.SetFloat("farPlane", FarPlane);
    }
}