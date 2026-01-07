using Engine.Collision;

using Visualisation.Core.Display.Gizmos.Rotation;
using Visualisation.Core.Display.Gizmos.Translation;

namespace Visualisation.Core.GameObjects;

public abstract class GameObjectCollisionPrimitive : GameObject, ITranslationGizmoTarget, IRotationGizmoTarget
{
    public abstract CollisionPrimitive EngineCollisionPrimitive { get; }

    public Vector3 AxisPosition => EngineCollisionPrimitive.Body.Position.ToOpenTK();
    public Quaternion AxisOrientation => EngineCollisionPrimitive.Body.Orientation.ToOpenTK();

    Vector3 ITranslationGizmoTarget.Position
    {
        get => EngineCollisionPrimitive.Body.Position.ToOpenTK();
        set
        {
            EngineCollisionPrimitive.Body.Position = value.ToEngine();
            // EngineCollisionPrimitive.Body.Velocity = Engine.Vector3.Zero; // Zero velocity
            EngineCollisionPrimitive.Body.CalculateDerivedData();
            EngineCollisionPrimitive.Body.SetAwake();
            EngineCollisionPrimitive.CalculateInternals();
        }
    }

    public Quaternion Orientation
    {
        get => EngineCollisionPrimitive.Body.Orientation.ToOpenTK();
        set
        {
            EngineCollisionPrimitive.Body.Orientation = value.ToEngine();
            EngineCollisionPrimitive.Body.Rotation = Engine.Vector3.Zero; // Zero angular velocity
            EngineCollisionPrimitive.Body.CalculateDerivedData();
            EngineCollisionPrimitive.Body.SetAwake();
            EngineCollisionPrimitive.CalculateInternals();
        }
    }
}