using Engine.Force;
using Visualisation.Core.Display.Mesh.VisualObjects;

namespace Visualisation.Core.GameObjects
{
    public class Cloth : IVisualObject
    {
        public Engine.Cloth EngineCloth { get; set; }
        public SpringMesh VisualCloth { get; set; }
        public ForceRegistry ForceRegistry { get; set; }

        public Cloth(ForceRegistry registry)
        {
            ForceRegistry = registry;
        }

        public void Init()
        {
            EngineCloth = new Engine.Cloth(ForceRegistry);

            Vector3[,] pts = ConvertToOpenTk(EngineCloth.Points());
            VisualCloth?.Dispose();
            VisualCloth = new SpringMesh(pts);
            VisualCloth.Init();
        }

        public void Render()
        {
            Vector3[,] pts;
            if (EngineCloth == null) // no engine cloth! use placeholder instead
            {
                pts = new Vector3[2, 2];
                pts[0, 0] = new(0, 10, 0);
                pts[1, 0] = new(1, 10, 0);
                pts[0, 1] = new(0, 10, 1);
                pts[1, 1] = new(1, 9, 1);
            }
            else
            {
                // use the engine cloth
                pts = ConvertToOpenTk(EngineCloth.Points());
            }

            if (VisualCloth == null)
            {
                VisualCloth = new SpringMesh(pts);
                VisualCloth.Init();
            }
            else
            {
                //TODO add update mesh here
                VisualCloth.Dispose();
                VisualCloth = new SpringMesh(pts);
                VisualCloth.Init();
            }

            VisualCloth.Render();
        }

        public void Dispose()
        {
            VisualCloth.Dispose();
        }

        public void SetForShader(Shader sh)
        {
            VisualCloth?.SetForShader(sh);
        }


        public Guid Id => VisualCloth.Id;
        public AbstractVisualObject AbstractVisualObject => VisualCloth;
        public object PhysicsObject => EngineCloth;

        private static Vector3[,] ConvertToOpenTk(Engine.Vector3[,] enginePoints)
        {
            if (enginePoints == null) return null!;

            int sx = enginePoints.GetLength(0);
            int sy = enginePoints.GetLength(1);
            var result = new Vector3[sx, sy];

            for (int x = 0; x < sx; x++)
            {
                for (int y = 0; y < sy; y++)
                {
                    var e = enginePoints[x, y];
                    result[x, y] = new Vector3((float)e.X, (float)e.Y, (float)e.Z);
                }
            }

            return result;
        }
    }
}