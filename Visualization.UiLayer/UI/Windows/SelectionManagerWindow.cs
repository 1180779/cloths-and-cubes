using Engine.Rays;
using Engine.RigidBodies;

using ImGuiNET;

using Visualisation.Core;
using Visualisation.Core.Display.Materials;
using Visualisation.Core.GameObjects;

using Box = Visualisation.Core.GameObjects.Box;
using Cone = Visualisation.Core.GameObjects.Cone;
using Cylinder = Visualisation.Core.GameObjects.Cylinder;

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
                case Cylinder cylinder:
                    DrawCylinder(cylinder);
                    break;
                case Cone cone:
                    DrawCone(cone);
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

    private bool DrawVector3(ref Engine.Vector3 vec, String label, float step = 0.1f)
    {
        var tempVec = new System.Numerics.Vector3(vec.X, vec.Y, vec.Z);
        if (ImGui.DragFloat3(label, ref tempVec, step))
        {
            vec.X = tempVec.X;
            vec.Y = tempVec.Y;
            vec.Z = tempVec.Z;
            return true;
        }

        return false;
    }

    private bool DrawVector3Text(Engine.Vector3 vec, String label)
    {
        ImGui.BeginDisabled();
        var tempVec = new System.Numerics.Vector3(vec.X, vec.Y, vec.Z);
        if (ImGui.DragFloat3(label, ref tempVec, 0.0f))
        {
            ImGui.EndDisabled();
            return true;
        }

        ImGui.EndDisabled();
        return false;
    }

    private bool DrawVector4(ref Engine.Quaternion qua, String label, float step = 0.1f)
    {
        var tempVec = new System.Numerics.Vector4(qua.I, qua.J, qua.K, qua.R);
        if (ImGui.DragFloat4(label, ref tempVec, step))
        {
            qua.I = tempVec.X;
            qua.J = tempVec.Y;
            qua.K = tempVec.Z;
            qua.R = tempVec.W;
            qua.Normalise();
            return true;
        }

        return false;
    }

    private bool DrawRigidBody(RigidBody body)
    {
        bool returnValue =
            DrawVector3(ref body.Position, "Position") ||
            DrawVector3(ref body.Velocity, "Velocity") ||
            DrawVector3(ref body.Rotation, "Rotation") ||
            DrawVector3(ref body.Acceleration, "Acceleration") ||
            DrawVector4(ref body.OrientationRef, "Orientation", 0.02f);

        // if any property changed, wake up the body
        if (returnValue)
        {
            body.SetAwake();
        }

        DrawBoolDisabled(ref body.CanSleep, "Can Sleep");
        var isAwake = body.IsAwake;
        DrawBoolDisabled(ref isAwake, "Is Awake");
        return returnValue;
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
        ImGui.SeparatorText("Cloth Properties");

        // Display current size
        ImGui.Text($"Current Size: {cloth.EngineCloth.SizeX} x {cloth.EngineCloth.SizeY}");
        ImGui.Text($"Total Particles: {cloth.EngineCloth.SizeX * cloth.EngineCloth.SizeY}");
        ImGui.Spacing();

        // Cloth Transformation Section
        if (ImGui.CollapsingHeader("Transformation", ImGuiTreeNodeFlags.DefaultOpen))
        {
            // Translation
            ImGui.Text("Move Cloth:");
            var moveOffset = System.Numerics.Vector3.Zero;
            if (ImGui.DragFloat3("Offset##Move", ref moveOffset, 0.1f))
            {
                // Applied when drag stops
            }

            ImGui.SameLine();
            if (ImGui.Button("Apply##Move"))
            {
                var engineMove = new Engine.Vector3(moveOffset.X, moveOffset.Y, moveOffset.Z);
                cloth.EngineCloth.Move(engineMove);
            }

            ImGui.Spacing();

            // Rotation
            ImGui.Text("Rotate Cloth:");
            var rotationAngles = System.Numerics.Vector3.Zero;
            if (ImGui.DragFloat3("Rotation (radians)##Rotate", ref rotationAngles, 0.01f))
            {
                // Applied when drag stops
            }

            ImGui.SameLine();
            if (ImGui.Button("Apply##Rotate"))
            {
                var engineRot = new Engine.Vector3(rotationAngles.X, rotationAngles.Y, rotationAngles.Z);
                cloth.EngineCloth.Rotate(engineRot);
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Rotates cloth around its initial corner position");
            }
        }

        // Spring Parameters Section
        if (ImGui.CollapsingHeader("Spring Parameters"))
        {
            var springConstant = cloth.EngineCloth.SpringConstant;
            var springLength = cloth.EngineCloth.SpringLength;
            var particleMass = cloth.EngineCloth.ParticleMass;

            ImGui.DragFloat("Spring Constant", ref springConstant, 0.1f, 0.1f, 100.0f);
            ImGui.DragFloat("Spring Length", ref springLength, 0.01f, 0.01f, 5.0f);
            ImGui.DragFloat("Particle Mass", ref particleMass, 0.01f, 0.01f, 10.0f);

            if (ImGui.Button("Apply Spring Parameters"))
            {
                cloth.RegenerateCloth(
                    cloth.EngineCloth.SizeX,
                    cloth.EngineCloth.SizeY,
                    springLength,
                    springConstant,
                    particleMass);
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Warning: This will reset all particle velocities!");
            }
        }

        // Resize Section
        if (ImGui.CollapsingHeader("Resize Cloth"))
        {
            var newSizeX = cloth.EngineCloth.SizeX;
            var newSizeY = cloth.EngineCloth.SizeY;

            ImGui.DragInt("New Size X", ref newSizeX, 1, 2, 100);
            ImGui.DragInt("New Size Y", ref newSizeY, 1, 2, 100);

            ImGui.TextColored(new System.Numerics.Vector4(1.0f, 0.5f, 0.0f, 1.0f),
                "Warning: Resizing will reset the cloth!");

            if (ImGui.Button("Regenerate Cloth"))
            {
                cloth.RegenerateCloth(
                    newSizeX,
                    newSizeY,
                    cloth.EngineCloth.SpringLength,
                    cloth.EngineCloth.SpringConstant,
                    cloth.EngineCloth.ParticleMass);
            }
        }

        // Debug Info Section
        if (ImGui.CollapsingHeader("Debug Info"))
        {
            var particle0Pos = cloth.EngineCloth.Particle0Pos;
            ImGui.Text($"Origin Position: ({particle0Pos.X:F2}, {particle0Pos.Y:F2}, {particle0Pos.Z:F2})");
            ImGui.Text($"Spring Constant: {cloth.EngineCloth.SpringConstant}");
            ImGui.Text($"Spring Length: {cloth.EngineCloth.SpringLength}");
            ImGui.Text($"Particle Mass: {cloth.EngineCloth.ParticleMass}");
        }
    }

    private void DrawCylinder(Cylinder cylinder)
    {
        DrawRigidBody(cylinder.EngineCylinder.Body);
    }

    private void DrawCone(Cone cone)
    {
        DrawRigidBody(cone.EngineCone.Body);
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

    public sealed record State
    {
        public bool DrawInvisibleObjects;
        public bool DrawDebugRay;
        public bool DrawSelectedObjectWithoutDepthTesting;
        public bool Unselect;
        public bool SelectionEnabled;
    }

    public State SaveState()
    {
        return new State
        {
            DrawInvisibleObjects = _selectionManager.DrawInvisibleObjects,
            DrawDebugRay = _debugRayDraw,
            DrawSelectedObjectWithoutDepthTesting = _selectionManager.DrawSelectedObjectWithoutDepthTesting,
            Unselect = _selectionManager.Unselect,
            SelectionEnabled = _selectionManager.SelectionEnabled
        };
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