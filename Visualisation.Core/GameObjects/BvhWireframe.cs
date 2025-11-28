using Engine.Collision.Bounding_Volume_Hierarchy;

using OpenTK.Graphics.OpenGL4;

using Visualisation.Core.Display.Mesh.VisualObjects;

namespace Visualisation.Core.GameObjects;

public class BvhWireframe
{
    public BvhWireframe(BVH bvh)
    {
        this._bvh = bvh;
    }

    private BVH _bvh;
    private static CubeMesh? s_debugMesh;

    public Vector3 LeafColor = new(1, 0, 1);
    public bool RenderLeafs;
    public Vector3[] LevelColors { get; set; } = [new(0, 1, 0)];
    public int[]? LevelsToRender { get; set; }

    public void Render(Shader shader)
    {
        if (this._bvh.root == null) return;

        GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
        s_debugMesh ??= new CubeMesh();
        RenderRecursive(_bvh.root, shader, 0);
        GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
    }

    private void RenderRecursive(BVHNode node, Shader shader, int depth)
    {
        if (node == null) return;

        if (LevelColors != null && RenderLeafs && node.isLeaf)
        {
            var center = node.bounds.center;
            var size = node.bounds.halfSize * 2.0f;

            // Calculate model matrix: Translate to center, then Scale
            var model = Matrix4.CreateScale(new Vector3(size.X, size.Y, size.Z)) *
                Matrix4.CreateTranslation(new Vector3(center.X, center.Y, center.Z));

            shader.SetMatrix4("model", model);
            shader.SetVector3("color", LeafColor);
            s_debugMesh!.Render();
        }

        if (LevelsToRender == null || Array.IndexOf(LevelsToRender, depth) != -1)
        {
            var center = node.bounds.center;
            var size = node.bounds.halfSize * 2.0f;

            // Calculate model matrix: Translate to center, then Scale
            var model = Matrix4.CreateScale(new Vector3(size.X, size.Y, size.Z)) *
                Matrix4.CreateTranslation(new Vector3(center.X, center.Y, center.Z));

            shader.SetMatrix4("model", model);
            shader.SetVector3("color", LevelColors[depth % LevelColors.Length]);
            s_debugMesh!.Render();
        }

        if (!node.isLeaf)
        {
            var internalNode = (BVHInternal)node;
            RenderRecursive(internalNode.left, shader, depth + 1);
            RenderRecursive(internalNode.right, shader, depth + 1);
        }
    }
}