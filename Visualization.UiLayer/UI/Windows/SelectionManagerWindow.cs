using Engine.Rays;
using Engine.RigidBodies;

using ImGuiNET;

using Visualisation.Core;
using Visualisation.Core.Display.Materials;
using Visualisation.Core.GameObjects;

using Box = Visualisation.Core.GameObjects.Box;

namespace Visualization.UiLayer.UI.Windows;

public sealed class SelectionManagerWindow(SelectionManager selectionManager) : IWindow
{
    private SelectionManager _selectionManager = selectionManager;

    private bool _debugRayDraw;
    private Line? _debugRay;
    private Ray? _lastRay;

    public string Name => "Selected Object";

    public void DebugRenderInScene(Shader shader)
    {
        if (!_debugRayDraw)
            return;
        if (_selectionManager.LastRay is not null &&
            (_lastRay is null || _lastRay.Value != _selectionManager.LastRay.Value))
        {
            _lastRay = _selectionManager.LastRay;
            var ray = _selectionManager.LastRay.Value;
            var rayDirection = new Vector3(ray.Direction.X, ray.Direction.Y, ray.Direction.Z);
            var rayOriginInWorld = new Vector3(ray.Origin.X, ray.Origin.Y, ray.Origin.Z);
            Vector3 end = rayOriginInWorld + rayDirection * (float)_selectionManager.SelectedObjectDistance;

            _debugRay?.Dispose();
            _debugRay = new Line(rayOriginInWorld, end);
        }

        shader.SetMatrix4("model", Matrix4.Identity);
        _debugRay?.Render();
    }

    public void Draw(ref bool isOpen)
    {
        if (ImGui.Begin("Selected Object", ref isOpen))
        {
            bool selection = _selectionManager.SelectionEnabled;
            if (ImGui.Checkbox("Enable selection", ref selection))
            {
                _selectionManager.SelectionEnabled = selection;
            }

            ImGui.Checkbox("Draw selection ray", ref _debugRayDraw);
            ImGui.Checkbox("Draw invisible objects", ref _selectionManager.DrawInvisibleObjects);
            ImGui.Checkbox("Draw selected object even behind other objects",
                ref _selectionManager.DrawSelectedObjectWithoutDepthTesting);
            ImGui.Checkbox("Unselect objects", ref _selectionManager.Unselect);
            ImGui.Separator();
            ImGui.Spacing();
            if (_selectionManager.SelectedObject is GameObject gameObject)
            {
                ImGui.Checkbox("Invisible", ref gameObject.Invisible);
                DrawMaterialSelectors(gameObject);
            }

            switch (_selectionManager.SelectedObject)
            {
                case Box box:
                    DrawBox(box);
                    break;
                case Ball ball:
                    DrawSphere(ball);
                    break;
                case Cloth cloth:
                    DrawCloth(cloth);
                    break;
                case RigidParticle particle:
                    DrawParticle(particle);
                    break;
                case Plane plane:
                    DrawPlane(plane);
                    break;
            }
        }

        ImGui.End();
    }

    private void DrawMaterialSelectors(GameObject gameObject)
    {
        if (ImGui.CollapsingHeader("Constant Materials"))
        {
            var materials = MaterialsHelper.AllConstMaterials;
            foreach (var material in materials)
            {
                if (ImGui.Button(material.Name))
                {
                    gameObject.Material.Dispose();
                    gameObject.Material = material.TypedClone();
                }
            }
        }

        if (ImGui.CollapsingHeader("Textured Materials"))
        {
            var materials = MaterialsHelper.AllTexturedMaterials;
            foreach (var material in materials)
            {
                if (ImGui.Button(material.Name))
                {
                    gameObject.Material.Dispose();
                    gameObject.Material = material.TypedClone();
                }
            }
        }
    }

    private void DrawVector3Property(
        Func<Engine.Vector3> get,
        Action<Engine.Vector3> set,
        String label,
        float step = 0.1f)
    {
        var vec = get();
        var tempVec = new System.Numerics.Vector3(vec.X, vec.Y, vec.Z);
        if (ImGui.DragFloat3(label, ref tempVec, step))
        {
            set(new Engine.Vector3(tempVec.X, tempVec.Y, tempVec.Z));
        }
    }

    private void DrawVector3(ref Engine.Vector3 vec, String label, float step = 0.1f)
    {
        var tempVec = new System.Numerics.Vector3(vec.X, vec.Y, vec.Z);
        if (ImGui.DragFloat3(label, ref tempVec, step))
        {
            vec.X = tempVec.X;
            vec.Y = tempVec.Y;
            vec.Z = tempVec.Z;
        }
    }

    private void DrawVector3Text(Engine.Vector3 vec, String label)
    {
        ImGui.BeginDisabled();
        var tempVec = new System.Numerics.Vector3(vec.X, vec.Y, vec.Z);
        ImGui.DragFloat3(label, ref tempVec, 0.0f);
        ImGui.EndDisabled();
    }

    private void DrawVector4(ref Engine.Quaternion qua, String label, float step = 0.1f)
    {
        var tempVec = new System.Numerics.Vector4(qua.I, qua.J, qua.K, qua.R);
        if (ImGui.DragFloat4(label, ref tempVec, step))
        {
            qua.I = tempVec.X;
            qua.J = tempVec.Y;
            qua.K = tempVec.Z;
            qua.R = tempVec.W;
        }
    }

    private void DrawRigidBody(RigidBody body)
    {
        DrawVector3(ref body.Position, "Position");
        DrawVector3(ref body.Velocity, "Velocity");
        DrawVector3(ref body.Rotation, "Rotation");
        DrawVector3(ref body.Acceleration, "Acceleration");
        DrawVector4(ref body.OrientationRef, "Orientation", 0.02f);

        DrawBoolDisabled(ref body.CanSleep, "Can Sleep");
        var isAwake = body.IsAwake;
        DrawBoolDisabled(ref isAwake, "Is Awake");
    }

    private void DrawBox(Box box)
    {
        DrawRigidBody(box.EngineBox.Body);

        DrawVector3(ref box.EngineBox.HalfSize, "Half Size");
        var maxPoint = box.EngineBox.Body.Position + box.EngineBox.HalfSize;
        var minPoint = box.EngineBox.Body.Position - box.EngineBox.HalfSize;
        DrawVector3Text(maxPoint, "Max Point");
        DrawVector3Text(minPoint, "Min Point");
    }

    private void DrawSphere(Ball ball)
    {
        DrawRigidBody(ball.EngineBall.Body);
    }

    private void DrawCloth(Cloth cloth)
    {
        if (ImGui.CollapsingHeader("Particles"))
        {
            for (int i = 0; i < cloth.EngineCloth.SizeX; ++i)
            {
                for (int j = 0; j < cloth.EngineCloth.SizeY; ++j)
                {
                    ImGui.Text($"Particle [{i}, {j}]");
                    DrawParticle(cloth.EngineCloth.Particles[i, j], i, j);
                    ImGui.Separator();
                    ImGui.Spacing();
                }
            }

            foreach (var particle in cloth.EngineCloth.Particles)
            {
                DrawParticle(particle);
                ImGui.Separator();
            }
        }
    }

    private void DrawParticle(RigidParticle particle, int x = 0, int y = 0)
    {
        ImGui.PushID($"Position {x},{y}");
        DrawVector3(ref particle.Body.Position, "Position");
        ImGui.PushID($"Velocity {x},{y}");
        DrawVector3(ref particle.Body.Velocity, "Velocity");
        ImGui.PushID($"Acceleration {x},{y}");
        DrawVector3(ref particle.Body.Acceleration, "Acceleration");
        ImGui.PopID();
        ImGui.PopID();
        ImGui.PopID();

        ImGui.PushID($"Can Sleep {x},{y}");
        DrawBoolDisabled(ref particle.Body.CanSleep, "Can Sleep");
        bool isAwake = particle.Body.IsAwake;
        ImGui.PushID($"Is Awake {x},{y}");
        DrawBoolDisabled(ref isAwake, "Is Awake");
        ImGui.PopID();
        ImGui.PopID();
    }

    private void DrawPlane(Plane plane)
    {
        var collisionPlane = plane.EnginePlane;
        DrawVector3Property(() => collisionPlane.Direction, v => collisionPlane.Direction = v, "Direction");
        var offset = (float)collisionPlane.Offset;
        if (ImGui.DragFloat("Offset", ref offset, 0.1f))
        {
            collisionPlane.Offset = offset;
        }
    }

    private void DrawBoolDisabled(ref bool value, string label)
    {
        ImGui.BeginDisabled();
        ImGui.Checkbox(label, ref value);
        ImGui.EndDisabled();
    }

    public record State(
        bool DrawInvisibleObjects,
        bool DrawDebugRay,
        bool DrawSelectedObjectWithoutDepthTesting,
        bool Unselect,
        bool SelectionEnabled
    );

    public State SaveState()
    {
        return new State(_selectionManager.DrawInvisibleObjects, _debugRayDraw,
            _selectionManager.DrawSelectedObjectWithoutDepthTesting, _selectionManager.Unselect,
            _selectionManager.SelectionEnabled);
    }

    public void RestoreState(State state)
    {
        _debugRayDraw = state.DrawDebugRay;

        _selectionManager.DrawInvisibleObjects = state.DrawInvisibleObjects;
        _selectionManager.DrawSelectedObjectWithoutDepthTesting = state.DrawSelectedObjectWithoutDepthTesting;
        _selectionManager.Unselect = state.Unselect;
        _selectionManager.SelectionEnabled = state.SelectionEnabled;
    }
}