using Engine.Collision;

namespace Visualisation.Core.GameObjects;

public abstract class GameObjectCollisionPrimitive : GameObject
{
    public abstract CollisionPrimitive EngineCollisionPrimitive { get; }
}