using Visualisation.Core.Display.Gizmos.Rotation;
using Visualisation.Core.Display.Gizmos.Translation;
using Visualisation.Core.GameObjects;

namespace Visualisation.Core.Display.Gizmos.Adapters;

public class ClothGizmoAdapter : ITranslationGizmoTarget, IRotationGizmoTarget
{
    private readonly Cloth _cloth;
    private Quaternion _previousOrientation = Quaternion.Identity;

    public ClothGizmoAdapter(Cloth cloth)
    {
        _cloth = cloth;
    }

    public Vector3 AxisPosition => _cloth.EngineCloth.Center.ToOpenTK();
    public Quaternion AxisOrientation => Quaternion.Identity;

    public Vector3 Position
    {
        get => _cloth.EngineCloth.Center.ToOpenTK();
        set
        {
            _cloth.EngineCloth.Center = value.ToEngine();
            _cloth.EngineCloth.ClearAccumulators();
        }
    }

    public Quaternion Orientation
    {
        get => _previousOrientation;
        set
        {
            var deltaRotation = value * Quaternion.Invert(_previousOrientation);
            _previousOrientation = value;

            var axisAngle = deltaRotation.ToAxisAngle();
            var axis = axisAngle.Xyz;
            var angle = axisAngle.W;

            var rotationVector = axis * angle;

            _cloth.EngineCloth.RotateAroundCenter(rotationVector.ToEngine());
            _cloth.EngineCloth.ClearAccumulators();
        }
    }
}