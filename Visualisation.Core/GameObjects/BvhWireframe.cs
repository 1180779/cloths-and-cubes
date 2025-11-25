using Engine.Collision.Bounding_Volume_Hierarchy;
using OpenTK.Graphics.OpenGL4;
using Visualisation.Core.Display.Mesh.VisualObjects;

namespace Visualisation.Core.GameObjects;

public class BvhWireframe
{
	public BvhWireframe(BVH bvh)
	{
		this.bvh = bvh;
	}

	private BVH bvh;
	private static CubeMesh? _debugMesh;

	public Vector3[]? LevelColors { get; set; }
	public int[]? LevelsToRender { get; set; }

	public void Render(Shader shader)
	{
		GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
		_debugMesh ??= new CubeMesh();
		RenderRecursive(bvh.root, shader, 0);
		GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
	}

	private void RenderRecursive(BVHNode node, Shader shader, int depth)
	{
		if (node == null) return;

		if (LevelsToRender == null || Array.IndexOf(LevelsToRender, depth) != -1)
		{
			var center = node.bounds.center;
			var size = node.bounds.halfSize * 2.0f;

			// Calculate model matrix: Translate to center, then Scale
			var model = Matrix4.CreateScale(new Vector3(size.X, size.Y, size.Z)) *
				Matrix4.CreateTranslation(new Vector3(center.X, center.Y, center.Z));

			shader.SetMatrix4("model", model);
			shader.SetVector3("color", LevelColors[depth % LevelColors.Length]);
			_debugMesh!.Render();
		}

		if (!node.isLeaf)
		{
			var internalNode = (BVHInternal)node;
			RenderRecursive(internalNode.left, shader, depth + 1);
			RenderRecursive(internalNode.right, shader, depth + 1);
		}
	}
}