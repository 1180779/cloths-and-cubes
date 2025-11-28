using Engine.Force;

using Visualisation.Core.Display.Materials;
using Visualisation.Core.Display.Mesh;
using Visualisation.Core.Display.Mesh.VisualObjects;

namespace Visualisation.Core.GameObjects;

public sealed class Cloth : GameObject
{
    public Engine.Cloth EngineCloth { get; set; }
    public SpringMesh VisualCloth { get; set; } // borrowed (does not own the data) from Mesh interface here
    public ForceRegistry ForceRegistry { get; set; }

    public Cloth(ForceRegistry registry)
    {
        ForceRegistry = registry;
        EngineCloth = new Engine.Cloth(ForceRegistry);
        Vector3[,] pts = ConvertToOpenTk(EngineCloth.Points());
        VisualCloth = new SpringMesh(pts);
        Mesh = VisualCloth;

        Material = MaterialConstant.BlueRubber;
    }

    protected override void PreRender()
    {
        var pts = ConvertToOpenTk(EngineCloth.Points());
        VisualCloth.UpdatePoints(pts);
    }

    public override Vector3 Position =>
        new(EngineCloth.Particles[0, 0].Body.Position.X, EngineCloth.Particles[0, 0].Body.Position.Y,
            EngineCloth.Particles[0, 0].Body.Position.Z);

    protected override IMesh Mesh { get; set; }
    public override object PhysicsObject => EngineCloth;
    public override Matrix4 Model => Matrix4.Identity;

    private static Vector3[,] ConvertToOpenTk(Engine.Vector3[,] enginePoints)
    {
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