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

public sealed class SelectionManagerWindow(InteractionManager interactionManager) : IWindow
{
    private InteractionManager _interactionManager = interactionManager;

    private bool _debugRayDraw;
    private LineMesh? _selectionRayLineMesh;
    private Ray? _previousSelectionRay;

    // Cloth spring settings
    private bool _lsctpm; // linear spring constant to particle mass
    private Real _lsctpmScale = (Real)1.0;
    private Real _lsctpmBias = (Real)0.0;

    private bool _lsctsl; // linear spring constant to spring length
    private Real _lsctslScale = (Real)1.0;
    private Real _lsctslBias = (Real)0.0;

    // Track the current cloth and its initial parameters
    private Cloth? _currentCloth;
    private System.Numerics.Vector3 _accumulatedRotation;

    public const string StaticName = "Interaction and selected object";
    public string Name => StaticName;

    public void DebugRenderInScene(Shader shader)
    {
        if (!_debugRayDraw)
            return;
        if (_interactionManager.SelectionManager.SelectionRay is not null &&
            (_previousSelectionRay is null ||
                _previousSelectionRay.Value != _interactionManager.SelectionManager.SelectionRay.Value))
        {
            _previousSelectionRay = _interactionManager.SelectionManager.SelectionRay;
            var ray = _interactionManager.SelectionManager.SelectionRay.Value;
            var rayDirection = new Vector3(ray.Direction.X, ray.Direction.Y, ray.Direction.Z);
            var rayOriginInWorld = new Vector3(ray.Origin.X, ray.Origin.Y, ray.Origin.Z);
            Vector3 end = rayOriginInWorld + rayDirection * _interactionManager.SelectionManager.SelectedObjectDistance;

            _selectionRayLineMesh?.Dispose();
            _selectionRayLineMesh = new LineMesh(rayOriginInWorld, end);
        }

        shader.SetMatrix4("model", Matrix4.Identity);
        _selectionRayLineMesh?.Render();
    }

    public void Draw(ref bool isOpen)
    {
        if (ImGui.Begin(Name, ref isOpen))
        {
            ImGui.Checkbox("Enable mouse drag", ref _interactionManager.EditorState.DraggingState.IsDraggingEnabled);
            ImGui.SliderFloat("Mouse drag zoom sensitivity",
                ref _interactionManager.EditorState.DraggingState.Sensitivity,
                0.1f, 10.0f);

            bool selection = _interactionManager.SelectionManager.SelectionEnabled;
            if (ImGui.Checkbox("Enable selection", ref selection))
            {
                _interactionManager.SelectionManager.SelectionEnabled = selection;
            }

            ImGui.Checkbox("Draw selection ray", ref _debugRayDraw);
            var unselectOnSelectedObjectClick = _interactionManager.EditorState.Selection.UnselectOnSelectedObjectClick;
            if (ImGui.Checkbox("Unselect objects",
                ref unselectOnSelectedObjectClick))
            {
                _interactionManager.EditorState.Selection.UnselectOnSelectedObjectClick = unselectOnSelectedObjectClick;
            }

            ImGui.Separator();
            ImGui.Spacing();

            if (ImGui.CollapsingHeader("Selected Object Properties", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Indent();
                DrawGameObjectProperties(_interactionManager.SelectionManager.SelectedObject, "SelectedObject");
                ImGui.Unindent();
            }
        }

        ImGui.End();
    }

    private void DrawGameObjectProperties(object? targetObject, string id)
    {
        ImGui.PushID(id);
        if (targetObject is GameObject gameObject)
        {
            if (ImGui.CollapsingHeader("Common Properties", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Indent();
                ImGui.Spacing();
                ImGui.Text($"Current Material: {gameObject.Material.Name}");

                ImGui.Text("Change Material:");
                DrawMaterialSelectors(gameObject);
                ImGui.Unindent();
            }
        }
        else
        {
            ImGui.BeginDisabled();
            ImGui.CollapsingHeader("Game Object Properties");
            ImGui.EndDisabled();
        }

        ImGui.PopID();

        switch (targetObject)
        {
            case ClothParticleWrapper particleWrapper:
                DrawClothParticleWrapper(particleWrapper);
                break;
            case Box box:
                DrawBox(box);
                break;
            case Ball ball:
                DrawSphere(ball);
                break;
            case Cloth cloth:
                DrawCloth(cloth);
                break;
            case ClothRigidParticleInCorner particleInCorner:
                DrawParticle(particleInCorner);
                break;
            case ClothRigidParticle clothParticle:
                DrawParticle(clothParticle);
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

    private void DrawMaterialSelectors(GameObject gameObject)
    {
        if (ImGui.CollapsingHeader("Constant Materials"))
        {
            ImGui.Indent();
            var materials = MaterialsAndEnvironmentMapsHelper.AllConstMaterials;
            foreach (var material in materials)
            {
                var materialName = material.Name;
                if (ImGui.Button(materialName, UiControls.Style.ButtonSizes.SmallFillX(materialName)))
                {
                    gameObject.Material.Dispose();
                    gameObject.Material = material.TypedClone();
                }
            }

            ImGui.Unindent();
        }

        if (ImGui.CollapsingHeader("Textured Materials"))
        {
            ImGui.Indent();
            var materials = MaterialsAndEnvironmentMapsHelper.AllTexturedMaterials;
            foreach (var material in materials)
            {
                var materialName = material.Name;
                if (ImGui.Button(materialName, UiControls.Style.ButtonSizes.SmallFillX(materialName)))
                {
                    gameObject.Material.Dispose();
                    gameObject.Material = material.TypedClone();
                }
            }

            ImGui.Unindent();
        }
    }

    private bool DrawVector3Property(
        Func<Engine.Vector3> get,
        Action<Engine.Vector3> set,
        String label,
        float step = 0.1f)
    {
        var vec = get();
        var tempVec = vec.ToNumerics();
        if (ImGui.DragFloat3(label, ref tempVec, step))
        {
            set(tempVec.ToEngine());
            return true;
        }

        return false;
    }

    private bool DrawVector3Positive(ref Engine.Vector3 vec, String label, float step = 0.1f, float minValue = 0f)
    {
        var tempVec = vec.ToNumerics();
        if (ImGui.DragFloat3(label, ref tempVec, step, minValue, float.PositiveInfinity))
        {
            vec = tempVec.ToEngine();
            return true;
        }

        return false;
    }

    private bool DrawVector3(ref Engine.Vector3 vec, String label, float step = 0.1f)
    {
        var tempVec = vec.ToNumerics();
        if (ImGui.DragFloat3(label, ref tempVec, step))
        {
            vec = tempVec.ToEngine();
            return true;
        }

        return false;
    }

    private bool DrawQuaternion(ref Engine.Quaternion qua, String label, float step = 0.1f)
    {
        var tempVec = qua.ToNumericsV4();
        if (ImGui.DragFloat4(label, ref tempVec, step))
        {
            qua = tempVec.ToEngineQuaternion();
            // qua.Normalise();
            return true;
        }

        return false;
    }

    private bool DrawQuaternionProperty(
        Func<Engine.Quaternion> get,
        Action<Engine.Quaternion> set,
        String label,
        float step = 0.1f)
    {
        var vec = get();
        var tempVec = vec.ToNumericsV4();
        if (ImGui.DragFloat4(label, ref tempVec, step))
        {
            set(tempVec.ToEngineQuaternion());
            return true;
        }

        return false;
    }

    private bool DrawRigidBody(RigidBody body)
    {
        if (ImGui.CollapsingHeader("Transformation", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();

            // TODO: fix the static bodies handling in the engine code first.
            //  Currently making a body static can result in Nans and program crash.
            // if (body.InverseMass == 0)
            // {
            //     ImGui.Text("This body is static and immovable by physic interactions.");
            //     ImGui.BeginDisabled();
            //     ImGui.Button("Make static", UiControls.Style.ButtonSizes.Small("Make static"));
            //     ImGui.EndDisabled();
            // }
            // else
            // {
            //     ImGui.Text("This body is dynamic.");
            //     if (ImGui.Button("Make static", UiControls.Style.ButtonSizes.Small("Make static")))
            //     {
            //         body.MakeStatic();
            //     }
            // }

            bool returnValue = false;
            returnValue |= DrawVector3(ref body.Position, "Position");

            Quaternion.ToEulerAngles(body.Orientation.ToOpenTK(), out Vector3 eulerAngles);
            var tempEulerAngles = eulerAngles.ToNumerics();
            if (ImGui.DragFloat3("Rotation", ref tempEulerAngles, 0.01f))
            {
                body.Orientation = Quaternion.FromEulerAngles(tempEulerAngles.ToOpenTK()).ToEngine();
                returnValue = true;
            }

            const string resetOrientationText = "Reset Orientation";
            if (ImGui.Button(resetOrientationText, UiControls.Style.ButtonSizes.Small(resetOrientationText)))
            {
                body.Orientation = Engine.Quaternion.Identity;
                returnValue = true;
            }

            const string zeroVelocityText = "Reset Velocity and Angular Velocity";
            if (ImGui.Button(zeroVelocityText, UiControls.Style.ButtonSizes.Small(zeroVelocityText)))
            {
                body.Velocity = Engine.Vector3.Zero;
                body.Rotation = Engine.Vector3.Zero;
                body.ClearAccumulators();
                returnValue = true;
            }

            ImGui.BeginDisabled();
            DrawVector3(ref body.Velocity, "Velocity");
            DrawVector3(ref body.Rotation, "Angular Velocity");
            DrawVector3(ref body.Acceleration, "Acceleration");
            ImGui.EndDisabled();

            // if any property changed recalculate derived data and set awake
            if (returnValue)
            {
                body.CalculateDerivedData();
                body.SetAwake();
            }

            DrawBoolDisabled(ref body.CanSleep, "Can Sleep");
            var isAwake = body.IsAwake;
            DrawBoolDisabled(ref isAwake, "Is Awake");

            ImGui.Unindent();
            return returnValue;
        }

        return false;
    }

    private void DrawBox(Box box)
    {
        DrawRigidBody(box.EngineBox.Body);
        DrawVector3Positive(ref box.EngineBox.HalfSize, "Half Size", minValue: 0.05f);
    }

    private void DrawSphere(Ball ball)
    {
        DrawRigidBody(ball.EngineBall.Body);
        UiControls.DragFloatPropertyPositive(() => ball.EngineBall.Radius, r => ball.EngineBall.Radius = r, "Radius",
            minValue: 0.05f);
    }

    private void DrawCloth(Cloth cloth)
    {
        // Initialize parameters when cloth changes
        if (_currentCloth != cloth)
        {
            _currentCloth = cloth;
            _accumulatedRotation = System.Numerics.Vector3.Zero;
        }

        if (ImGui.CollapsingHeader("Transformation", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();

            var editCenter = cloth.EngineCloth.Center.ToNumerics();
            if (ImGui.DragFloat3("Position of Center", ref editCenter, 0.1f))
            {
                cloth.EngineCloth.Center = editCenter.ToEngine();
            }

            var lastAccumulatedRotation = _accumulatedRotation;
            if (ImGui.DragFloat3("Rotation change", ref _accumulatedRotation, 0.01f))
            {
                var rotationChange = _accumulatedRotation - lastAccumulatedRotation;
                cloth.EngineCloth.RotateAroundCenter(rotationChange.ToEngine());
            }

            UiControls.SetTooltip("Rotates cloth around its center. Displays cumulative rotation.");

            const string zeroVelocityText = "Reset Velocity and Angular Velocity";
            if (ImGui.Button(zeroVelocityText, UiControls.Style.ButtonSizes.Small(zeroVelocityText)))
            {
                foreach (var clothParticle in cloth.EngineCloth.Particles)
                {
                    clothParticle.Body.Velocity = Engine.Vector3.Zero;
                    clothParticle.Body.Rotation = Engine.Vector3.Zero;
                    clothParticle.Body.ClearAccumulators();
                }
            }

            ImGui.Unindent();
        }

        bool needsRegeneration = false;
        var editSizeX = cloth.EngineCloth.SizeX;
        var editSizeY = cloth.EngineCloth.SizeY;
        var editSpringLength = cloth.EngineCloth.SpringLength;
        var editSpringConstant = cloth.EngineCloth.SpringConstant;
        var editParticleMass = cloth.EngineCloth.ParticleMass;
        const string regenNote =
            "Note: Changing these parameters will regenerate the cloth and reset particle positions.";
        if (ImGui.CollapsingHeader("Spring Parameters", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.TextWrapped(regenNote);
            ImGui.Indent();

            ImGui.Text($"Spring Length: {editSpringLength:F6}");
            if (ImGui.SliderFloat("Spring Length", ref editSpringLength, 0.000_01f, 1.0f))
            {
                if (_lsctsl)
                {
                    editSpringConstant = _lsctslScale * editSpringLength + _lsctslBias;
                }

                needsRegeneration = true;
            }

            ImGui.Text($"Spring Constant: {editSpringConstant:F6}");
            if (ImGui.DragFloat("Spring Constant", ref editSpringConstant, 0.005f, 0.0f, 100.0f))
            {
                if (_lsctpm)
                {
                    editParticleMass = (Real)Math.Max((editSpringConstant - _lsctpmBias), 0.0) / _lsctpmScale;
                }
                else if (_lsctsl)
                {
                    editSpringLength = (editSpringConstant - _lsctslBias) / _lsctslScale;
                }

                needsRegeneration = true;
            }

            ImGui.Text($"Particle Mass: {editParticleMass:F6}");
            if (ImGui.DragFloat("Particle Mass", ref editParticleMass, 0.000_01f, 0.000_01f, 10.0f))
            {
                if (_lsctpm)
                {
                    editSpringConstant = editParticleMass * _lsctpmScale + _lsctpmBias;
                }

                needsRegeneration = true;
            }

            // Linear Spring Constant to Particle Mass
            if (DrawClothLsctpm(ref editSpringConstant, ref editParticleMass))
            {
                needsRegeneration = true;
            }

            // Linear Spring Constant to Spring Length
            if (DrawClothLsctsl(ref editSpringConstant, ref editSpringLength))
            {
                needsRegeneration = true;
            }

            ImGui.Unindent();
        }

        // Resize Section
        if (ImGui.CollapsingHeader("Resize Cloth"))
        {
            ImGui.TextWrapped(regenNote);
            needsRegeneration |= ImGui.DragInt("New Size X", ref editSizeX, 1, 2, 100);
            needsRegeneration |= ImGui.DragInt("New Size Y", ref editSizeY, 1, 2, 100);
        }

        if (needsRegeneration)
        {
            editParticleMass = Math.Max(editParticleMass, 0.000_01f); // prevent zero or negative mass
            editSpringLength = Math.Max(editSpringLength, 0.000_01f); // prevent zero or negative length
            editSpringConstant = Math.Max(editSpringConstant, 0.000_01f); // prevent zero or negative constant

            _interactionManager.ClearExcept(cloth);
            cloth.RegenerateClothPreservingTheCenter(editSizeX, editSizeY, editSpringLength, editSpringConstant,
                editParticleMass);
        }
    }

    private void DrawClothParticleWrapper(ClothParticleWrapper wrapper)
    {
        ImGui.Text($"Cloth Particle [{wrapper.ParticleX}, {wrapper.ParticleY}]");
        ImGui.Separator();

        var particle = wrapper.Particle;
        var body = particle.Body;

        ImGui.Text($"Position: ({body.Position.X:F2}, {body.Position.Y:F2}, {body.Position.Z:F2})");
        ImGui.Text($"Velocity: ({body.Velocity.X:F2}, {body.Velocity.Y:F2}, {body.Velocity.Z:F2})");
        ImGui.Text($"Rotation: ({body.Rotation.X:F2}, {body.Rotation.Y:F2}, {body.Rotation.Z:F2})");
        ImGui.Text($"Acceleration: ({body.Acceleration.X:F2}, {body.Acceleration.Y:F2}, {body.Acceleration.Z:F2})");
        ImGui.Text($"Mass: {body.Mass:F4}");
        ImGui.Text($"Inverse mass: {body.InverseMass:F4}");

        bool isAnchor = body.InverseMass == 0;
        ImGui.Text($"Is Anchor: {(isAnchor ? "Yes" : "No")}");

        ImGui.Spacing();

        ImGui.SeparatorText("Automatic Pinning to Box Corners");
        const string autoPinningText =
            "When dragging this particle, it will automatically pin to nearby box corners. " +
            "Move the particle close to a box corner to pin it. Move it away to unpin.";
        ImGui.TextWrapped(autoPinningText);

        ImGui.Spacing();

        DisplayParentClothProperties(wrapper);
    }


    private const int InfoNameWidth = 30;
    private readonly string _clothSizeText = "Cloth Size".PadRight(InfoNameWidth);
    private readonly string _springConstantText = "Spring Constant".PadRight(InfoNameWidth);
    private readonly string _springLengthText = "Spring Length".PadRight(InfoNameWidth);
    private readonly string _particleMassText = "Particle Mass".PadRight(InfoNameWidth);

    private void DisplayParentClothProperties(ClothParticleWrapper wrapper)
    {
        if (ImGui.CollapsingHeader("Parent Cloth Info"))
        {
            var cloth = wrapper.ParentCloth;
            ImGui.Text($"{_clothSizeText} {cloth.EngineCloth.SizeX,2} x {cloth.EngineCloth.SizeY,2}");
            ImGui.Text($"{_springConstantText} {cloth.EngineCloth.SpringConstant:F2}");
            ImGui.Text($"{_springLengthText} {cloth.EngineCloth.SpringLength:F2}");
            ImGui.Text($"{_particleMassText} {cloth.EngineCloth.ParticleMass:F4}");

            const string selectParentClothText = "Select Parent Cloth";
            if (ImGui.Button(selectParentClothText, UiControls.Style.ButtonSizes.Medium(selectParentClothText)))
            {
                _interactionManager.EditorState.Selection.SelectedObject = cloth;
            }
        }
    }

    private bool DrawClothLsctpm(ref Real springConstant, ref Real particleMass)
    {
        bool changed = false;

        if (_lsctsl)
        {
            ImGui.BeginDisabled();
        }

        if (ImGui.Checkbox("Linear Spring Constant to Particle Mass", ref _lsctpm) && _lsctpm)
        {
            springConstant = _lsctpmScale * particleMass + _lsctpmBias;
            changed = true;
        }

        if (_lsctsl)
        {
            ImGui.EndDisabled();
        }

        if (!_lsctpm || _lsctsl)
        {
            ImGui.BeginDisabled();
        }

        ImGui.PushID("Scale (particle mass)");
        if (ImGui.DragFloat("Scale", ref _lsctpmScale, 0.5f, 0.0f, 10.0f))
        {
            springConstant = _lsctpmScale * particleMass + _lsctpmBias;
            changed = true;
        }

        ImGui.PopID();

        ImGui.PushID("Bias (particle mass)");
        if (ImGui.DragFloat("Bias", ref _lsctpmBias, 0.5f, 0.0f, 10.0f))
        {
            springConstant = _lsctpmScale * particleMass + _lsctpmBias;
            changed = true;
        }

        ImGui.PopID();

        ImGui.PushID("Reset Scale (particle mass)");
        if (ImGui.Button("Reset Scale"))
        {
            _lsctpmScale = (Real)1.0;
            springConstant = _lsctpmScale * particleMass + _lsctpmBias;
            changed = true;
        }

        ImGui.PopID();

        ImGui.SameLine();
        ImGui.PushID("Reset Bias (particle mass)");
        if (ImGui.Button("Reset Bias"))
        {
            _lsctpmBias = (Real)0.0;
            springConstant = _lsctpmScale * particleMass + _lsctpmBias;
            changed = true;
        }

        ImGui.PopID();

        if (!_lsctpm || _lsctsl)
        {
            ImGui.EndDisabled();
        }

        return changed;
    }

    private bool DrawClothLsctsl(ref Real springConstant, ref Real springLength)
    {
        bool changed = false;

        if (_lsctpm)
        {
            ImGui.BeginDisabled();
        }

        if (ImGui.Checkbox("Linear Spring Constant to Spring Length", ref _lsctsl) && _lsctsl)
        {
            springConstant = _lsctslScale * springLength + _lsctslBias;
            changed = true;
        }

        if (_lsctpm)
        {
            ImGui.EndDisabled();
        }

        if (!_lsctsl || _lsctpm)
        {
            ImGui.BeginDisabled();
        }

        ImGui.PushID("Scale (spring length)");
        if (ImGui.DragFloat("Scale", ref _lsctslScale, 0.5f, 0.0f, 10.0f))
        {
            springConstant = _lsctslScale * springLength + _lsctslBias;
            changed = true;
        }

        ImGui.PopID();

        ImGui.PushID("Bias (spring length)");
        if (ImGui.DragFloat("Bias", ref _lsctslBias, 0.5f, 0.0f, 10.0f))
        {
            springConstant = _lsctslScale * springLength + _lsctslBias;
            changed = true;
        }

        ImGui.PopID();

        ImGui.PushID("Reset Scale (spring length)");
        if (ImGui.Button("Reset Scale"))
        {
            _lsctslScale = (Real)1.0;
            springConstant = _lsctslScale * springLength + _lsctslBias;
            changed = true;
        }

        ImGui.PopID();

        ImGui.SameLine();
        ImGui.PushID("Reset Bias (spring length)");
        if (ImGui.Button("Reset Bias"))
        {
            _lsctslBias = (Real)0.0;
            springConstant = _lsctslScale * springLength + _lsctslBias;
            changed = true;
        }

        ImGui.PopID();

        if (!_lsctsl || _lsctpm)
        {
            ImGui.EndDisabled();
        }

        return changed;
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
        if (ImGui.CollapsingHeader("Transformation", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();

            ImGui.PushID($"Position {x},{y}");
            DrawVector3(ref particle.Body.Position, "Position");
            ImGui.PopID();
            ImGui.BeginDisabled();
            ImGui.PushID($"Velocity {x},{y}");
            DrawVector3(ref particle.Body.Velocity, "Velocity");
            ImGui.PopID();
            ImGui.PushID($"Acceleration {x},{y}");
            DrawVector3(ref particle.Body.Acceleration, "Acceleration");
            ImGui.PopID();
            ImGui.EndDisabled();

            ImGui.PushID($"Can Sleep {x},{y}");
            DrawBoolDisabled(ref particle.Body.CanSleep, "Can Sleep");
            ImGui.PopID();
            ImGui.PushID($"Is Awake {x},{y}");
            bool isAwake = particle.Body.IsAwake;
            DrawBoolDisabled(ref isAwake, "Is Awake");
            ImGui.PopID();

            ImGui.Unindent();
        }
    }

    private void DrawPlane(Plane plane)
    {
        if (ImGui.CollapsingHeader("Transformation", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();
            var collisionPlane = plane.EnginePlane;
            DrawVector3Property(() => collisionPlane.Direction, v => collisionPlane.Direction = v, "Direction");
            ImGui.DragFloat("Offset", ref collisionPlane.Offset, 0.1f);
            ImGui.Unindent();
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
        public bool DrawDebugRay { get; init; }
        public bool Unselect { get; init; }
        public bool DraggingEnabled { get; init; }
        public bool SelectionEnabled { get; init; }
        public bool Lsctpm { get; init; }
        public Real LsctpmScale { get; init; }
        public Real LsctpmBias { get; init; }
        public bool Lsctsl { get; init; }
        public Real LsctslScale { get; init; }
        public Real LsctslBias { get; init; }
    }

    public State SaveState()
    {
        return new State
        {
            DrawDebugRay = _debugRayDraw,
            Unselect = _interactionManager.EditorState.Selection.UnselectOnSelectedObjectClick,
            DraggingEnabled = _interactionManager.StaticDragManager.Enabled,
            SelectionEnabled = _interactionManager.SelectionManager.SelectionEnabled,
            Lsctpm = _lsctpm,
            LsctpmScale = _lsctpmScale,
            LsctpmBias = _lsctpmBias,
            Lsctsl = _lsctsl,
            LsctslScale = _lsctslScale,
            LsctslBias = _lsctslBias
        };
    }

    public void RestoreState(State state)
    {
        _debugRayDraw = state.DrawDebugRay;

        _interactionManager.EditorState.Selection.UnselectOnSelectedObjectClick = state.Unselect;
        _interactionManager.SelectionManager.SelectionEnabled = state.SelectionEnabled;
        _interactionManager.StaticDragManager.Enabled = state.DraggingEnabled;

        _lsctpm = state.Lsctpm;
        _lsctpmScale = state.LsctpmScale;
        _lsctpmBias = state.LsctpmBias;

        _lsctsl = state.Lsctsl;
        _lsctslScale = state.LsctslScale;
        _lsctslBias = state.LsctslBias;
    }
}