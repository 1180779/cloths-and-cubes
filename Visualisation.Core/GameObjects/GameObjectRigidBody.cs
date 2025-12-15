namespace Visualisation.Core.GameObjects;

public abstract class GameObjectRigidBody : GameObject
{
    public abstract Engine.RigidBodies.RigidBody EngineRigidBody { get; }
}