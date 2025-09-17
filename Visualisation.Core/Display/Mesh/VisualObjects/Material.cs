namespace Visualisation.Core.Display.Mesh.VisualObjects;

public sealed class Material(Vector3 ambient, Vector3 diffuse, Vector3 specular, float shininess)
{
    private static readonly string AmbientShaderName = "material.ambient";
    private static readonly string DiffuseShaderName = "material.diffuse";
    private static readonly string SpecularShaderName = "material.specular";
    private static readonly string ShininessShaderName = "material.shininess";

    public override string ToString()
    {
        return $"{{Material: Ambient: {Ambient}, Diffuse: {Diffuse}, Specular: {Specular}, Shininess: {Shininess}}}";
    }

    public void SetForShader(Shader sh)
    {
        sh.SetVector3(AmbientShaderName, Ambient);
        sh.SetVector3(DiffuseShaderName, Diffuse);
        sh.SetVector3(SpecularShaderName, Specular);
        sh.SetFloat(ShininessShaderName, Shininess);
    }

    public Vector3 Ambient = ambient;
    public Vector3 Diffuse = diffuse;
    public Vector3 Specular = specular;
    public float Shininess = shininess;

    public Material() : this(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f),
        new Vector3(0.0f, 0.0f, 0.0f), 1.0f)
    {
    }

    public static readonly Material Emerald = new(
        new Vector3(0.02150000f, 0.17450000f, 0.02150000f),
        new Vector3(0.07568000f, 0.61423999f, 0.07568000f),
        new Vector3(0.63300002f, 0.72781098f, 0.63300002f),
        0.60000002f * 128.0f
    );

    public static readonly Material Jade = new(
        new Vector3(0.13500001f, 0.22250000f, 0.15750000f),
        new Vector3(0.54000002f, 0.88999999f, 0.63000000f),
        new Vector3(0.31622800f, 0.31622800f, 0.31622800f),
        0.10000000f * 128.0f
    );

    public static readonly Material Obsidian = new(
        new Vector3(0.05375000f, 0.05000000f, 0.06625000f),
        new Vector3(0.18275000f, 0.17000000f, 0.22525001f),
        new Vector3(0.33274099f, 0.32863399f, 0.34643501f),
        0.30000001f * 128.0f
    );

    public static readonly Material Pearl = new(
        new Vector3(0.25000000f, 0.20725000f, 0.20725000f),
        new Vector3(1.00000000f, 0.82900000f, 0.82900000f),
        new Vector3(0.29664800f, 0.29664800f, 0.29664800f),
        0.08800000f * 128.0f
    );

    public static readonly Material Ruby = new(
        new Vector3(0.17450000f, 0.01175000f, 0.01175000f),
        new Vector3(0.61423999f, 0.04136000f, 0.04136000f),
        new Vector3(0.72781098f, 0.62695903f, 0.62695903f),
        0.60000002f * 128.0f
    );

    public static readonly Material Turquoise = new(
        new Vector3(0.10000000f, 0.18725000f, 0.17450000f),
        new Vector3(0.39600000f, 0.74150997f, 0.69102001f),
        new Vector3(0.29725400f, 0.30829000f, 0.30667800f),
        0.10000000f * 128.0f
    );

    public static readonly Material Brass = new(
        new Vector3(0.32941201f, 0.22352900f, 0.02745100f),
        new Vector3(0.78039199f, 0.56862700f, 0.11372500f),
        new Vector3(0.99215698f, 0.94117600f, 0.80784303f),
        0.21794872f * 128.0f
    );

    public static readonly Material Bronze = new(
        new Vector3(0.21250001f, 0.12750000f, 0.05400000f),
        new Vector3(0.71399999f, 0.42840001f, 0.18144000f),
        new Vector3(0.39354801f, 0.27190599f, 0.16672100f),
        0.20000000f * 128.0f
    );

    public static readonly Material Chrome = new(
        new Vector3(0.25000000f, 0.25000000f, 0.25000000f),
        new Vector3(0.40000001f, 0.40000001f, 0.40000001f),
        new Vector3(0.77459699f, 0.77459699f, 0.77459699f),
        0.60000002f * 128.0f
    );

    public static readonly Material Copper = new(
        new Vector3(0.19125000f, 0.07350000f, 0.02250000f),
        new Vector3(0.70380002f, 0.27048001f, 0.08280000f),
        new Vector3(0.25677699f, 0.13762200f, 0.08601400f),
        0.10000000f * 128.0f
    );

    public static readonly Material Gold = new(
        new Vector3(0.24725001f, 0.19949999f, 0.07450000f),
        new Vector3(0.75164002f, 0.60648000f, 0.22648001f),
        new Vector3(0.62828100f, 0.55580199f, 0.36606500f),
        0.40000001f * 128.0f
    );

    public static readonly Material Silver = new(
        new Vector3(0.19225000f, 0.19225000f, 0.19225000f),
        new Vector3(0.50753999f, 0.50753999f, 0.50753999f),
        new Vector3(0.50827301f, 0.50827301f, 0.50827301f),
        0.40000001f * 128.0f
    );

    public static readonly Material BlackPlastic = new(
        new Vector3(0.00000000f, 0.00000000f, 0.00000000f),
        new Vector3(0.01000000f, 0.01000000f, 0.01000000f),
        new Vector3(0.50000000f, 0.50000000f, 0.50000000f),
        0.25000000f * 128.0f
    );

    public static readonly Material CyanPlastic = new(
        new Vector3(0.00000000f, 0.10000000f, 0.06000000f),
        new Vector3(0.00000000f, 0.50980389f, 0.50980389f),
        new Vector3(0.50196075f, 0.50196075f, 0.50196075f),
        0.25000000f * 128.0f
    );

    public static readonly Material GreenPlastic = new(
        new Vector3(0.00000000f, 0.00000000f, 0.00000000f),
        new Vector3(0.10000000f, 0.34999999f, 0.10000000f),
        new Vector3(0.44999999f, 0.55000001f, 0.44999999f),
        0.25000000f * 128.0f
    );

    public static readonly Material RedPlastic = new(
        new Vector3(0.00000000f, 0.00000000f, 0.00000000f),
        new Vector3(0.50000000f, 0.00000000f, 0.00000000f),
        new Vector3(0.69999999f, 0.60000002f, 0.60000002f),
        0.25000000f * 128.0f
    );

    public static readonly Material WhitePlastic = new(
        new Vector3(0.00000000f, 0.00000000f, 0.00000000f),
        new Vector3(0.55000001f, 0.55000001f, 0.55000001f),
        new Vector3(0.69999999f, 0.69999999f, 0.69999999f),
        0.25000000f * 128.0f
    );

    public static readonly Material YellowPlastic = new(
        new Vector3(0.00000000f, 0.00000000f, 0.00000000f),
        new Vector3(0.50000000f, 0.50000000f, 0.00000000f),
        new Vector3(0.60000002f, 0.60000002f, 0.50000000f),
        0.25000000f * 128.0f
    );

    public static readonly Material BlackRubber = new(
        new Vector3(0.02000000f, 0.02000000f, 0.02000000f),
        new Vector3(0.01000000f, 0.01000000f, 0.01000000f),
        new Vector3(0.40000001f, 0.40000001f, 0.40000001f),
        0.07812500f * 128.0f
    );

    public static readonly Material CyanRubber = new(
        new Vector3(0.00000000f, 0.05000000f, 0.05000000f),
        new Vector3(0.40000001f, 0.50000000f, 0.50000000f),
        new Vector3(0.04000000f, 0.69999999f, 0.69999999f),
        0.07812500f * 128.0f
    );

    public static readonly Material GreenRubber = new(
        new Vector3(0.00000000f, 0.05000000f, 0.00000000f),
        new Vector3(0.40000001f, 0.50000000f, 0.40000001f),
        new Vector3(0.04000000f, 0.69999999f, 0.04000000f),
        0.07812500f * 128.0f
    );

    public static readonly Material RedRubber = new(
        new Vector3(0.05000000f, 0.00000000f, 0.00000000f),
        new Vector3(0.50000000f, 0.40000001f, 0.40000001f),
        new Vector3(0.69999999f, 0.04000000f, 0.04000000f),
        0.07812500f * 128.0f
    );

    public static readonly Material WhiteRubber = new(
        new Vector3(0.05000000f, 0.05000000f, 0.05000000f),
        new Vector3(0.50000000f, 0.50000000f, 0.50000000f),
        new Vector3(0.69999999f, 0.69999999f, 0.69999999f),
        0.07812500f * 128.0f
    );

    public static readonly Material YellowRubber = new(
        new Vector3(0.05000000f, 0.05000000f, 0.00000000f),
        new Vector3(0.50000000f, 0.50000000f, 0.40000001f),
        new Vector3(0.69999999f, 0.69999999f, 0.04000000f),
        0.07812500f * 128.0f
    );
}