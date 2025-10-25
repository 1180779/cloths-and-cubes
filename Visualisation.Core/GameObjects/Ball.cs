using Visualisation.Core.Display.Mesh.VisualObjects;
using Sphere = Engine.RigidBodies.Sphere;

namespace Visualisation.Core.GameObjects
{
    public class Ball : IVisualObject
    {
        public Sphere EngineBall { get; private set; } = new();
        public Display.Mesh.VisualObjects.Sphere VisualBall { get; private set; } = new();

        public AbstractVisualObject AbstractVisualObject => VisualBall;

        public object PhysicsObject => EngineBall;

        public Guid Id => VisualBall.Id;

        public void Dispose()
        {
            VisualBall.Dispose();
        }

        public void Init()
        {
            VisualBall.Init();
        }

        public void Render()
        {
            VisualBall.Position = new Vector3(EngineBall.Body.Position.X, EngineBall.Body.Position.Y,
                EngineBall.Body.Position.Z);
            VisualBall.Scale = new Vector3(EngineBall.Radius, EngineBall.Radius, EngineBall.Radius);
            var q = new Quaternion(EngineBall.Body.Orientation.I, EngineBall.Body.Orientation.J,
                EngineBall.Body.Orientation.K, EngineBall.Body.Orientation.R);
            // Normalize to guard against drift
            if (MathF.Abs(1f - q.Length) > 1e-3f)
                q = Quaternion.Normalize(q);
            VisualBall.Rotation = q;
            VisualBall.Render();
        }

        public void SetForShader(Shader sh)
        {
            VisualBall.SetForShader(sh);
        }
    }
}