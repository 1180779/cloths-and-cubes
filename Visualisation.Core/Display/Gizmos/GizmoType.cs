using System.Runtime.Serialization;

namespace Visualisation.Core.Display.Gizmos;

[Serializable]
public enum GizmoType
{
    [EnumMember(Value = "None")]
    None,

    [EnumMember(Value = "Translation")]
    Translation,

    [EnumMember(Value = "Rotation")]
    Rotation,

    [EnumMember(Value = "Scale")]
    Scale
}