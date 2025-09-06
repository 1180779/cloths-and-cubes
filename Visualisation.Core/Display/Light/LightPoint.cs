namespace Visualization.Display.Light;

public class LightPoint : LightLocatableBase
{
    public float Constant = 1.0f;
    public float Linear = 0.09f;
    public float Quadratic = 0.032f;

    public override void SetForShader(Shader sh, string structShName)
    {
        base.SetForShader(sh, structShName);
        sh.SetFloatMember(structShName + ".constant", Constant);
        sh.SetFloatMember(structShName + ".linear", Linear);
        sh.SetFloatMember(structShName + ".quadratic", Quadratic);
    }
}