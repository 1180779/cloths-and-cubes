namespace Visualisation.Core.Display.Gizmos.Translation;

public interface ITranslationGizmoTarget : IGizmoTarget
{
    public Vector3 Position { get; set; }
}