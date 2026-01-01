using Engine.Rays;

namespace Visualisation.Core.Display.Gizmos;

public interface IGizmoArrow
{
    public void Render(Shader shader, Vector3 origin, Vector3 direction, Vector4 color, float scaleFactor = 1.0f);
    public bool CheckIntersection(Ray ray, Vector3 origin, Vector3 direction, float handleSize, out float distance);
}