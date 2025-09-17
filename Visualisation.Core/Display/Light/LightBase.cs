namespace Visualisation.Core.Display.Light;

public class LightBase
{
    protected LightBase()
    {
    }

    public Vector3 Diffuse = new(1.0f, 1.0f, 1.0f);
    public Vector3 Specular = new(1.0f, 1.0f, 1.0f);

    public virtual void SetForShader(Shader sh, string structShName)
    {
        sh.SetVector3Member(structShName + ".diffuse", Diffuse);
        sh.SetVector3Member(structShName + ".specular", Specular);
    }
}