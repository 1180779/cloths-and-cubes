using Engine.RigidBodies;

namespace Visualisation.Core.GameObjects;

public abstract class GameObjectRigidBody : GameObject
{
    public abstract RigidBody EngineRigidBody { get; }
}