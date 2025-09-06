using Visualisation.Core.Inputs;

namespace Visualization.Display.Cameras;

public class CameraBase
{
    protected const float CameraSpeed = 1.5f;
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

    public float Pitch
    {
        get => MathHelper.RadiansToDegrees(pitch);
        set
        {
            var angle = MathHelper.Clamp(value, -89f, 89f);
            pitch = MathHelper.DegreesToRadians(angle);
            UpdateVectors();
        }
    }

    public float Yaw
    {
        get => MathHelper.RadiansToDegrees(yaw);
        set
        {
            yaw = MathHelper.DegreesToRadians(value);
            UpdateVectors();
        }
    }

    public float Fov
    {
        get => MathHelper.RadiansToDegrees(fov);
        set
        {
            var angle = MathHelper.Clamp(value, 1f, 90f);
            fov = MathHelper.DegreesToRadians(angle);
        }
    }

    public Matrix4 GetViewMatrix()
    {
        return Matrix4.LookAt(Position, Position + front, up);
    }

    public Matrix4 GetProjectionMatrix()
    {
        return Matrix4.CreatePerspectiveFieldOfView(fov, AspectRatio, 0.01f, 100f);
    }

    public virtual void ProcessInput(IInputProvider input, float dt)
    {
    }

    public virtual void Update(float dt)
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
        sh.SetMatrix4("view", GetViewMatrix());
        sh.SetMatrix4("projection", GetProjectionMatrix());
    }
}