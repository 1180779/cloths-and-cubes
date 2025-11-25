namespace Visualisation.Core.Display;

public static class ProjectionHelper
{
    public static ICollection<Vector4> GetFrustumCornersWorldSpace(Matrix4 proj, Matrix4 view)
    {
        // The combined matrix in OpenTK (row-major) should be view * proj
        // to match the effect of GLM's (column-major) proj * view.
        var viewProj = view * proj;
        var inverse = Matrix4.Invert(viewProj);

        List<Vector4> frustumCorners = new();
        for (int x = 0; x < 2; ++x)
        {
            for (int y = 0; y < 2; ++y)
            {
                for (int z = 0; z < 2; ++z)
                {
                    // Use Vector4.Transform for correct row-major vector-matrix multiplication
                    var pt = Vector4.TransformRow(new Vector4(
                        2.0f * x - 1.0f,
                        2.0f * y - 1.0f,
                        2.0f * z - 1.0f,
                        1.0f), inverse);
                    frustumCorners.Add(pt / pt.W);
                }
            }
        }

        return frustumCorners;
    }
}