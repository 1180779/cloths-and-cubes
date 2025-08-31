namespace Visualization.Display.Light;

public class LightSpotlight : LightLocatableBase
{
    public LightSpotlight()
    {
    }

    public float Cutoff = 12.5f;
    public float OuterCutoff = 25.0f;
    public Vector3 Direction = Vector3.UnitY;

    public override void SetForShader(Shader sh, string structShName)
    {
        base.SetForShader(sh, structShName);
        sh.SetVector3Member(structShName + ".direction", -Direction);
        sh.SetFloatMember(structShName + ".cutoff", (float)MathHelper.Cos(MathHelper.DegToRad * Cutoff));
        sh.SetFloatMember(structShName + ".outerCutoff", (float)MathHelper.Cos(MathHelper.DegToRad * OuterCutoff));
    }
}