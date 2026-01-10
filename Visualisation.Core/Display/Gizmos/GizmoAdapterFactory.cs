using Visualisation.Core.Display.Gizmos.Adapters;
using Visualisation.Core.Display.Gizmos.Rotation;
using Visualisation.Core.Display.Gizmos.Scale;
using Visualisation.Core.Display.Gizmos.Translation;
using Visualisation.Core.GameObjects;

namespace Visualisation.Core.Display.Gizmos;

public static class GizmoAdapterFactory
{
    public static IGizmoTarget? CreateAdapter(object target, GizmoType gizmoType)
    {
        return gizmoType switch
        {
            GizmoType.Translation => CreateTranslationAdapter(target),
            GizmoType.Rotation => CreateRotationAdapter(target),
            GizmoType.Scale => CreateScaleAdapter(target),
            _ => null
        };
    }

    private static ITranslationGizmoTarget? CreateTranslationAdapter(object target)
    {
        return target switch
        {
            GameObjectCollisionPrimitive primitive => new GameObjectCollisionPrimitiveGizmoAdapter(primitive),
            Cloth cloth => new ClothGizmoAdapter(cloth),
            ClothParticleWrapper wrapper => new ClothParticleWrapperGizmoAdapter(wrapper),
            _ => null
        };
    }

    private static IRotationGizmoTarget? CreateRotationAdapter(object target)
    {
        return target switch
        {
            GameObjectCollisionPrimitive primitive => new GameObjectCollisionPrimitiveGizmoAdapter(primitive),
            Cloth cloth => new ClothGizmoAdapter(cloth),
            _ => null
        };
    }

    private static IScaleGizmoTarget? CreateScaleAdapter(object target)
    {
        return target switch
        {
            Box box => new BoxScaleGizmoAdapter(box),
            Ball ball => new BallScaleGizmoAdapter(ball),
            Cylinder cylinder => new CylinderScaleGizmoAdapter(cylinder),
            Cone cone => new ConeScaleGizmoAdapter(cone),
            _ => null
        };
    }
}