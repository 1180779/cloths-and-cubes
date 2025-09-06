namespace Visualization.Display.Light;

public class LightBase
{
    public LightBase()
    {
    }

    public Vector3 Ambient = new(0.2f, 0.2f, 0.2f);
    public Vector3 Diffuse = new(1.0f, 1.0f, 1.0f);
    public Vector3 Specular = new(1.0f, 1.0f, 1.0f);

    public static float BackgroundAmbient = 0.2f;

    public virtual void SetForShader(Shader sh, string structShName)
    {
        sh.SetVector3Member(structShName + ".ambient", Ambient);
        sh.SetVector3Member(structShName + ".diffuse", Diffuse);
        sh.SetVector3Member(structShName + ".specular", Specular);
    }
}