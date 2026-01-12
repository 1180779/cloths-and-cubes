using Visualisation.Core.Display.Gizmos.Rotation;
using Visualisation.Core.Display.Gizmos.Translation;
using Visualisation.Core.GameObjects;

namespace Visualisation.Core.Display.Gizmos.Adapters;

public class GameObjectCollisionPrimitiveGizmoAdapter : ITranslationGizmoTarget, IRotationGizmoTarget
{
    private readonly GameObjectCollisionPrimitive _target;

    public GameObjectCollisionPrimitiveGizmoAdapter(GameObjectCollisionPrimitive target)
    {
        _target = target;
    }

    public Vector3 AxisPosition => _target.EngineCollisionPrimitive.Body.Position.ToOpenTK();
    public Quaternion AxisOrientation => _target.EngineCollisionPrimitive.Body.Orientation.ToOpenTK();

    public Vector3 Position
    {
        get => _target.EngineCollisionPrimitive.Body.Position.ToOpenTK();
        set
        {
            _target.EngineCollisionPrimitive.Body.Position = value.ToEngine();
            _target.EngineCollisionPrimitive.Body.Velocity = Engine.Vector3.Zero;
            _target.EngineCollisionPrimitive.Body.CalculateDerivedData();
            _target.EngineCollisionPrimitive.Body.SetAwake();
            _target.EngineCollisionPrimitive.CalculateInternals();
        }
    }

    public Quaternion Orientation
    {
        get => _target.EngineCollisionPrimitive.Body.Orientation.ToOpenTK();
        set
        {
            _target.EngineCollisionPrimitive.Body.Orientation = value.ToEngine();
            _target.EngineCollisionPrimitive.Body.Rotation = Engine.Vector3.Zero;
            _target.EngineCollisionPrimitive.Body.CalculateDerivedData();
            _target.EngineCollisionPrimitive.Body.SetAwake();
            _target.EngineCollisionPrimitive.CalculateInternals();
        }
    }
}