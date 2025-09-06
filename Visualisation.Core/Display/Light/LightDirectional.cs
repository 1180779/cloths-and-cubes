namespace Visualization.Display.Light;

public class LightDirectional : LightPoint
{
    public Vector3 Direction = new(-0.2f, -0.2f, -0.2f);

    public override void SetForShader(Shader sh, string structShName)
    {
        base.SetForShader(sh, structShName);
        sh.SetVector3Member(structShName + ".direction", -Direction);
    }
}