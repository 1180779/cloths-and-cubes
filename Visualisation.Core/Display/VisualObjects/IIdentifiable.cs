namespace Visualisation.Core.Display.VisualObjects;

public interface IIdentifiable
{
    /// <summary>
    /// A unique identifier that persists for the lifetime of this object.
    /// This ID is copied during snapshotting to link the copy to the original.
    /// </summary>
    Guid Id { get; }
}