using Engine;
using Engine.Collision;
using Engine.Rays;
using Engine.RigidBodies;

// Simple test to debug cylinder intersection
var cylinder = new Cylinder { Height = 2.0f, Radius = 1.0f };
cylinder.Body.Position = Vector3.Zero;

// Test case 1: Direct hit from the side
var ray1 = new Ray(new Vector3(0, 0, -5), new Vector3(0, 0, 1));
var hit1 = RayIntersection.IntersectionRayCylinder(ray1, cylinder, out var dist1);
Console.WriteLine($"Test 1 - Side hit: {hit1}, distance: {dist1} (expected: true, 4.0)");

// Test case 2: Should miss (too high)
var ray2 = new Ray(new Vector3(0, 2, -5), new Vector3(0, 0, 1));
var hit2 = RayIntersection.IntersectionRayCylinder(ray2, cylinder, out var dist2);
Console.WriteLine($"Test 2 - Miss high: {hit2} (expected: false)");

// Test case 3: Grazing tangent
var ray3 = new Ray(new Vector3(1, 0, -5), new Vector3(0, 0, 1));
var hit3 = RayIntersection.IntersectionRayCylinder(ray3, cylinder, out var dist3);
Console.WriteLine($"Test 3 - Tangent: {hit3}, distance: {dist3} (expected: true, 5.0)");

// Test case 4: Top lid hit
var ray4 = new Ray(new Vector3(0, 5, 0), new Vector3(0, -1, 0));
var hit4 = RayIntersection.IntersectionRayCylinder(ray4, cylinder, out var dist4);
Console.WriteLine($"Test 4 - Top lid: {hit4}, distance: {dist4} (expected: true, 4.0)");

// Test case 5: Parallel to axis, inside radius
var ray5 = new Ray(new Vector3(0.5f, -5, 0), new Vector3(0, 1, 0));
var hit5 = RayIntersection.IntersectionRayCylinder(ray5, cylinder, out var dist5);
Console.WriteLine($"Test 5 - Parallel inside: {hit5}, distance: {dist5} (expected: true, 4.0)");

// Test case 6: Rotated cylinder
var rotCylinder = new Cylinder { Height = 2.0f, Radius = 1.0f };
rotCylinder.Body.Orientation = Quaternion.FromAxisAngle(Vector3.UnitX, MathF.PI / 2);
rotCylinder.Body.Position = Vector3.Zero;
var ray6 = new Ray(new Vector3(0, 0, -5), new Vector3(0, 0, 1));
var hit6 = RayIntersection.IntersectionRayCylinder(ray6, rotCylinder, out var dist6);
Console.WriteLine($"Test 6 - Rotated 90deg: {hit6}, distance: {dist6} (expected: true, 4.0)");

