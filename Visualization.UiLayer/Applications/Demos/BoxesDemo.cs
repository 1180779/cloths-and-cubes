using Engine.Collision;
using Engine.Collision.Bounding_Volume_Hierarchy;
using ImGuiNET;
using Visualisation.Core;
using Visualisation.Core.GameObjects;
using IntersectionTests = Engine.Collision.IntersectionTests;
using Random = Engine.Random;

namespace Visualization.UiLayer.Applications.Demos;

public class BoxesDemo : RigidBodyApplication
{
	protected Box[] Boxes = [];
	protected Ball[] Balls = [];
	protected Plane Plane;

	private bool[] bvhLevelsToRender = Enumerable.Repeat(true, 10).ToArray();

	private readonly Vector3[] levelColors =
	[
		new(1.0f, 0.0f, 0.0f), // Red
		new(0.0f, 1.0f, 0.0f), // Green
		new(0.0f, 0.0f, 1.0f), // Blue
		new(1.0f, 1.0f, 0.0f), // Yellow
		new(0.0f, 1.0f, 1.0f), // Cyan
		new(1.0f, 0.0f, 1.0f) // Magenta
	];

	protected BoxesDemo()
	{
		Plane = new();
	}

	protected override void RenderWindows(double dt)
	{
		base.RenderWindows(dt);
		ImGui.Begin("Bvh Nodes to render");

		for (var i = 0; i < bvhLevelsToRender.Length; i++)
		{
			var color = levelColors[i % levelColors.Length];
			ImGui.PushStyleColor(ImGuiCol.Text,
				new System.Numerics.Vector4(new System.Numerics.Vector3(color.X, color.Y, color.Z), 1.0f));
			ImGui.Checkbox($"Level {i}", ref bvhLevelsToRender[i]);
			ImGui.PopStyleColor();
		}

		ImGui.End();
	}

	protected override void DebugRenderInScene(Shader sh)
	{
		base.DebugRenderInScene(sh);
		Dictionary<int, IBoxable> boxDict = new();
		for (int i = 0; i < Boxes.Length; i++)
		{
			boxDict[i] = (IBoxable)Boxes[i];
		}

		BVH bvh = BVH.Build(boxDict);

		BvhWireframe bvhWireframe = new(bvh)
		{
			LevelColors = levelColors,
			LevelsToRender = bvhLevelsToRender
				.Select((enabled, index) => new { enabled, index })
				.Where(x => x.enabled)
				.Select(x => x.index)
				.ToArray()
		};
		bvhWireframe.Render(sh);
		Console.WriteLine("Rendering BvhWireframe");
	}

	/// <summary>
	/// Processes the contact generation code.
	/// </summary>
	protected override void GenerateContacts()
	{
		// Set up the collision data structure
		CollisionData.Reset(MaxContacts);
		CollisionData.Friction = (Real)0.9;
		CollisionData.Restitution = (Real)0.6;
		CollisionData.Tolerance = (Real)0.1;

		Dictionary<int, IBoxable> boxDict = new();
		for (int i = 0; i < Boxes.Length; i++)
		{
			boxDict[i] = (IBoxable)Boxes[i];
		}

		BVH bvh = BVH.Build(boxDict);

		for (int i = 0; i < Boxes.Length; i++)
		{
			var box = Boxes[i];
			if (!CollisionData.HasMoreContacts()) return;
			CollisionDetector.BoxAndHalfSpace(box.EngineBox, Plane.EnginePlane, CollisionData);

			List<(int, int)> potentialCollisions = new();
			BVH.TraverseRecursive(ref potentialCollisions, ref bvh, box.GetBoundingBox(), i, bvh.root);
			foreach (var other in potentialCollisions)
			{
				if (box == Boxes[other.Item1]) continue;
				if (!CollisionData.HasMoreContacts()) return;
				if (IntersectionTests.BoxAndBox(box.EngineBox, Boxes[other.Item1].EngineBox))
				{
					CollisionDetector.BoxAndBox(box.EngineBox, Boxes[other.Item1].EngineBox, CollisionData);
					box.EngineBox.IsOverlapping = Boxes[other.Item1].EngineBox.IsOverlapping = true;
				}
			}
		}

		// TODO: add spheres here as well
		// Perform exhaustive collision detection
		// for (var i = 0; i < boxes.Length; i++)
		// {
		//     // var box = boxes[i];
		//     // // Check for collisions with the ground plane
		//     // if (!CollisionData.HasMoreContacts()) return;
		//     // CollisionDetector.BoxAndHalfSpace(box.EngineBox, plane.EnginePlane, CollisionData);
		//     //
		//     // // Check for collisions with each other box
		//     // for (var j = i + 1; j < boxes.Length; j++)
		//     // {
		//     //     var other = boxes[j];
		//     //     if (!CollisionData.HasMoreContacts()) return;
		//     //     CollisionDetector.BoxAndBox(box.EngineBox, other.EngineBox, CollisionData);
		//     //
		//     //     if (IntersectionTests.BoxAndBox(box.EngineBox, other.EngineBox))
		//     //     {
		//     //         box.EngineBox.IsOverlapping = other.EngineBox.IsOverlapping = true;
		//     //     }
		//     // }
		//
		//     // Check for collisions with each ball
		//     for (var j = 0; j < balls.Length; j++)
		//     {
		//         var other = balls[j];
		//         if (!CollisionData.HasMoreContacts()) return;
		//         CollisionDetector.BoxAndSphere(box.EngineBox, other.EngineBall, CollisionData);
		//         //if (IntersectionTests.BoxAndSphere(box.EngineBox, other.EngineBall))
		//         //{
		//         //    box.EngineBox.IsOverlapping = true;
		//         //    other.EngineBall.IsOverlapping = true;
		//         //}
		//     }
		// }

		// for (var j = 0; j < balls.Length; ++j)
		// {
		//     var ball = balls[j];
		//     if (!CollisionData.HasMoreContacts()) return;
		//     CollisionDetector.SphereAndHalfSpace(ball.EngineBall, plane.EnginePlane, CollisionData);
		//
		//     for (var k = j + 1; k < balls.Length; ++k)
		//     {
		//         var other = balls[k];
		//         if (!CollisionData.HasMoreContacts()) return;
		//         CollisionDetector.SphereAndSphere(ball.EngineBall, other.EngineBall, CollisionData);
		//         if (IntersectionTests.SphereAndSphere(ball.EngineBall, other.EngineBall))
		//         {
		//             ball.EngineBall.IsOverlapping = other.EngineBall.IsOverlapping = true;
		//         }
		//     }
		// }
	}

	/// <summary>
	/// Processes the objects in the simulation forward in time.
	/// </summary>
	protected override void UpdateObjects(float duration)
	{
		// Update the physics of each box in turn
		foreach (var box in Boxes)
		{
			box.EngineBox.Body.Integrate(duration);
			box.EngineBox.CalculateInternals();
			box.EngineBox.IsOverlapping = false;
		}

		foreach (var ball in Balls)
		{
			ball.EngineBall.Body.Integrate(duration);
			ball.EngineBall.CalculateInternals();
			ball.EngineBall.IsOverlapping = false;
		}
	}

	/// <summary>
	/// Resets the position of all the boxes and primes the explosion.
	/// </summary>
	protected override void Reset()
	{
		// reset boxes; some in preconfigured positions
		if (Boxes.Length > 0)
		{
			Boxes[0].EngineBox.SetState(
				position: new Engine.Vector3(0, 3, 0),
				orientation: new Engine.Quaternion(),
				extents: new Engine.Vector3(4, 1, 1),
				velocity: new Engine.Vector3(0, 0, 0)
			);
		}

		if (Boxes.Length > 1)
		{
			Boxes[1].EngineBox.SetState(
				position: new Engine.Vector3(0, 4.75f, 2),
				orientation: new Engine.Quaternion(1.0f, 0.1f, 0.05f, 0.01f),
				extents: new Engine.Vector3(1, 1, 4),
				velocity: new Engine.Vector3(0, 0, 0)
			);
		}

		Random random = new();
		for (var i = 2; i < Boxes.Length; i++)
		{
			Boxes[i].EngineBox.Random(random);
		}

		// reset spheres; some in preconfigured positions
		if (Balls.Length > 0)
		{
			Balls[0].EngineBall.SetState(
				position: new Engine.Vector3(0, 10, 0),
				orientation: new Engine.Quaternion(),
				radius: 1.0f,
				velocity: new Engine.Vector3(0, 0, 0));
		}

		if (Balls.Length > 1)
		{
			Balls[1].EngineBall.SetState(
				position: new Engine.Vector3(3, 4.75f, 2),
				orientation: new Engine.Quaternion(),
				radius: 1.0f,
				velocity: new Engine.Vector3(0, 0, 0));
		}

		for (var i = 2; i < Balls.Length; i++)
		{
			Balls[i].EngineBall.Random(random);
		}

		// Reset the contacts
		CollisionData.ContactCount = 0;
	}
}