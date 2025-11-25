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

	public void Render(Shader shader)
	{
		GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
		_debugMesh ??= new CubeMesh();
		RenderRecursive(bvh.root, shader);
		GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
	}

	private void RenderRecursive(BVHNode node, Shader shader)
	{
		if (node == null) return;

		var center = node.bounds.center;
		var size = node.bounds.halfSize * 2.0f;

		// Calculate model matrix: Translate to center, then Scale
		var model = Matrix4.CreateScale(new Vector3(size.X, size.Y, size.Z)) *
			Matrix4.CreateTranslation(new Vector3(center.X, center.Y, center.Z));

		shader.SetMatrix4("model", model);
		_debugMesh!.Render();

		if (!node.isLeaf)
		{
			var internalNode = (BVHInternal)node;
			RenderRecursive(internalNode.left, shader);
			RenderRecursive(internalNode.right, shader);
		}
	}
}