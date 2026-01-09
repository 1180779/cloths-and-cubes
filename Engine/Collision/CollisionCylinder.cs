namespace Engine.Collision;

/// <summary>
/// The cylider is defined by the equation for the infinite cylinder
/// X² + Y² <= R²
/// and with the restriction for the Z coordinate
/// -Height/2 <= Z <= Height/2
/// </summary>
public class CollisionCylinder : CollisionPrimitive
{
    public Real Radius = 1.0f;
    public Real Height = 1.0f;
}