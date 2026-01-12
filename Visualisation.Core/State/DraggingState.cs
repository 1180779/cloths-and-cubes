using Visualisation.Core.Display.Gizmos.Translation;
using Visualisation.Core.Display.Mesh;
using Visualisation.Core.Display.Mesh.VisualObjects;
using Visualisation.Core.Inputs;

namespace Visualisation.Core.State;

public sealed class DraggingState : IDisposable
{
    public bool IsDraggingEnabled = false;
    public bool IsDragging => DraggedObject != null;
    public float Sensitivity = 1.0f;
    public MouseButton DragButton = MouseButton.Left;
    public float MinOffset = 1.0f;
    public float MaxOffset = 1_000_000.0f;
    public ITranslationGizmoTarget? DraggedObject;
    public float PlaneOffset = 0.1f;

    /// <summary>
    /// Color for the hover indicator. Default is semi-transparent cyan.
    /// </summary>
    public Vector4 HoverIndicatorColor = new(0.0f, 1.0f, 1.0f, 0.1f);

    /// <summary>
    /// Size of the hover indicator relative to the object. Default is 1.02 (2% larger).
    /// </summary>
    public float HoverIndicatorScale = 0.02f;

    /// <summary>
    /// Whether to show the hover indicator when enabled and a valid object is under the cursor.
    /// </summary>
    public bool ShowHoverIndicator = true;

    // Crosshair fields
    // Use the simple shader to render the crosshair
    // Should be rendered in the center of the screen
    // And very small, use the size modifier as the default size
    public Vector4 CrosshairColor = new(0.0f, 1.0f, 0.0f, 1.0f); // green
    public float CrosshairSize = 1.0f;
    public const float CrosshairSizeModifier = 0.005f;
    public IMesh CrosshairMesh = new SphereMesh(); // a dot by default

    public void Dispose()
    {
        CrosshairMesh.Dispose();
    }
}