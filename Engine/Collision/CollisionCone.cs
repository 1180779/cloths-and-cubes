namespace Engine.Collision;

/// <summary>
/// The cone is defined by a base radius and a height.
/// The cone is centered at the origin, with the base at Z = -Height/2 and the tip at Z = Height/2.
/// The radius tapers linearly from Radius at the base to 0 at the tip.
/// </summary>
public class CollisionCone : CollisionPrimitive
{
    public Real Radius = 1.0f;
    public Real Height = 1.0f;
}