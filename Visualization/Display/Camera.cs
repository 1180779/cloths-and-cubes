using Visualization.Display.Inputs;

namespace Visualization.Display
{
    public class Camera
    {
        private const float CameraSpeed = 1.5f;
        private const float Sensitivity = 0.2f;

        private Vector3 _front = -Vector3.UnitZ;
        private Vector3 _up = Vector3.UnitY;
        private Vector3 _right = Vector3.UnitX;

        private float _pitch;
        private float _yaw = -MathHelper.PiOver2;
        private float _fov = MathHelper.PiOver2;

        public Camera(float aspectRatio)
        {
            AspectRatio = aspectRatio;
        }

        public Camera(Vector3 position, float aspectRatio)
        {
            Position = position;
            AspectRatio = aspectRatio;
        }

        public Vector3 Position { get; set; } = new Vector3(0, 0, 0);
        public float AspectRatio { private get; set; }

        public Vector3 Front => _front;
        public Vector3 Up => _up;
        public Vector3 Right => _right;

        public float Pitch
        {
            get => MathHelper.RadiansToDegrees(_pitch);
            set
            {
                var angle = MathHelper.Clamp(value, -89f, 89f);
                _pitch = MathHelper.DegreesToRadians(angle);
                UpdateVectors();
            }
        }

        public float Yaw
        {
            get => MathHelper.RadiansToDegrees(_yaw);
            set
            {
                _yaw = MathHelper.DegreesToRadians(value);
                UpdateVectors();
            }
        }

        public float Fov
        {
            get => MathHelper.RadiansToDegrees(_fov);
            set
            {
                var angle = MathHelper.Clamp(value, 1f, 90f);
                _fov = MathHelper.DegreesToRadians(angle);
            }
        }

        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(Position, Position + _front, _up);
        }

        public Matrix4 GetProjectionMatrix()
        {
            return Matrix4.CreatePerspectiveFieldOfView(_fov, AspectRatio, 0.01f, 100f);
        }

        public void ProcessInput(IInputProvider input, float dt)
        {
            if (input.IsKeyDown(InputKey.W))
            {
                Position += Front * CameraSpeed * dt; // Forward
            }

            if (input.IsKeyDown(InputKey.S))
            {
                Position -= Front * CameraSpeed * dt; // Backwards
            }

            if (input.IsKeyDown(InputKey.A))
            {
                Position -= Right * CameraSpeed * dt; // Left
            }

            if (input.IsKeyDown(InputKey.D))
            {
                Position += Right * CameraSpeed * dt; // Right
            }

            if (input.IsKeyDown(InputKey.Space))
            {
                Position += Up * CameraSpeed * dt; // Up
            }

            if (input.IsKeyDown(InputKey.LeftShift))
            {
                Position -= Up * CameraSpeed * dt; // Down
            }

            var mouseDelta = input.GetMouseDelta();
            Yaw += mouseDelta.X * Sensitivity;
            Pitch -= mouseDelta.Y * Sensitivity; // Reversed since y-coordinates range from bottom to top
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

        public void SetForShader(Shader sh)
        {
            sh.SetVector3("viewPos", Position);
            sh.SetMatrix4("view", GetViewMatrix());
            sh.SetMatrix4("projection", GetProjectionMatrix());
        }
    }
}