using System.Diagnostics;

using Engine.Collision;
using Engine.Collision.Bounding_Volume_Hierarchy;
using Engine.ContactGenerators;
using Engine.Rays;
using Engine.RigidBodies;

using Visualisation.Core.Display.Cameras;
using Visualisation.Core.Display.Gizmos;
using Visualisation.Core.Display.Gizmos.Adapters;
using Visualisation.Core.Display.Gizmos.Translation;
using Visualisation.Core.Inputs;
using Visualisation.Core.State;

namespace Visualisation.Core.GameObjects;

public sealed class StaticDragManager(
    Func<object?> getHoveredObject,
    Func<CameraBase> cameraProvider,
    Func<IEnumerable<Box>> boxesProvider,
    Func<GlobalJointsList> globalJointsProvider,
    DraggingState draggingState
)
{
    public DraggingState DraggingState { get; } = draggingState;
    public float Sensitivity { get => DraggingState.Sensitivity; set => DraggingState.Sensitivity = value; }
    public bool Enabled { get => DraggingState.IsDraggingEnabled; set => DraggingState.IsDraggingEnabled = value; }
    public MouseButton DragButton { get => DraggingState.DragButton; set => DraggingState.DragButton = value; }
    public float MinOffset { get => DraggingState.MinOffset; set => DraggingState.MinOffset = value; }
    public float MaxOffset { get => DraggingState.MaxOffset; set => DraggingState.MaxOffset = value; }
    public bool IsDragging => DraggingState.IsDragging;

    public Vector4 HoverIndicatorColor
    {
        get => DraggingState.HoverIndicatorColor;
        set => DraggingState.HoverIndicatorColor = value;
    }

    public float HoverIndicatorScale
    {
        get => DraggingState.HoverIndicatorScale;
        set => DraggingState.HoverIndicatorScale = value;
    }

    public bool ShowHoverIndicator
    {
        get => DraggingState.ShowHoverIndicator;
        set => DraggingState.ShowHoverIndicator = value;
    }

    public ITranslationGizmoTarget? DraggedObject
    {
        get => DraggingState.DraggedObject;
        private set => DraggingState.DraggedObject = value;
    }

    /// <summary>
    /// Event fired when an object's position is updated during dragging.
    /// This allows external systems (like BVH) to update accordingly.
    /// </summary>
    public event Action? OnObjectDragged;

    /// <summary>
    /// The object that will be dragged if the user clicks (hover target).
    /// Null if no valid object is under the cursor.
    /// </summary>
    public object? HoverTarget => _getHoveredObject();


    private Func<object?> _getHoveredObject = getHoveredObject;
    private Func<CameraBase> _cameraProvider = cameraProvider;
    private Func<IEnumerable<Box>> _boxesProvider = boxesProvider;

    /// <summary>
    /// Distance from the camera in the camera front direction which determines the target location
    /// </summary>
    private float _planeOffset = 0.1f;

    /// <summary>
    /// Clears the currently dragged object. 
    /// </summary>
    public void ClearDraggedObject()
    {
        DraggedObject = null;
    }

    /// <summary>
    /// Clears the currently dragged object, except for the specified object.
    /// </summary>
    /// <param name="obj">The object to exclude from being cleared from dragging state.</param>
    public void ClearDraggedObjectExcept(object obj)
    {
        if (DraggedObject != obj)
        {
            ClearDraggedObject();
        }
    }

    /// <summary>
    /// Removes the specified object from the selection context, clearing selection and hover status if applicable.
    /// </summary>
    /// <param name="obj">The object to be removed from the interaction context.</param>
    public void RemoveObject(object obj)
    {
        if (DraggedObject == obj)
        {
            ClearDraggedObject();
        }
    }

    /// <summary>
    /// Handle input for dragging. 
    /// </summary>
    /// <param name="inputProvider"></param>
    /// <returns>Returns true if the method captured mouse input. False otherwise. </returns>
    public bool HandleInput(IInputProvider inputProvider)
    {
        if (!Enabled)
            return false;

        bool isButtonDown = inputProvider.IsMouseButtonDown(DragButton);
        if (IsDragging && !isButtonDown)
        {
            // When dragging ends, if we were dragging a cloth particle, reset its mass
            if (DraggedObject is ClothParticleWrapperGizmoAdapter endDragAdapter)
            {
                // Reset mass to the original particle mass
                endDragAdapter.Wrapper.Particle.Body.Mass = endDragAdapter.Wrapper.ParentCloth.EngineCloth.ParticleMass;

                // Clear accumulators to prevent "explosion" from accumulated forces during drag
                endDragAdapter.Wrapper.Particle.Body.Velocity = Engine.Vector3.Zero;
                endDragAdapter.Wrapper.Particle.Body.ClearAccumulators();
            }

            DraggedObject = null;
            return false;
        }

        if (!isButtonDown)
            return false;

        var camera = _cameraProvider();
        if (!IsDragging)
        {
            var hoveredObject = HoverTarget;
            if (hoveredObject != null)
            {
                // Try to create an adapter for the hovered object
                var adapter = GizmoAdapterFactory.CreateAdapter(hoveredObject, GizmoType.Translation);
                if (adapter is ITranslationGizmoTarget translationAdapter)
                {
                    DraggedObject = translationAdapter;
                    _planeOffset = Vector3.Dot(camera.Front, DraggedObject.Position - camera.Position);

                    // If we start dragging a cloth particle, set its mass to infinite
                    // so it acts as a fixed anchor point while dragging
                    if (DraggedObject is ClothParticleWrapperGizmoAdapter startDragAdapter)
                    {
                        startDragAdapter.Wrapper.Particle.Body.InverseMass = 0;
                        startDragAdapter.Wrapper.Particle.Body.Velocity = Engine.Vector3.Zero;
                        startDragAdapter.Wrapper.Particle.Body.ClearAccumulators();
                    }

                    return true;
                }
            }

            DraggedObject = null;
            return false;
        }

        Debug.Assert(DraggedObject != null);

        var scroll = inputProvider.GetMouseScroll();

        _planeOffset += Sensitivity * scroll;
        _planeOffset = Math.Clamp(_planeOffset, Math.Max(camera.NearPlane, MinOffset),
            Math.Min(camera.FarPlane, MaxOffset));
        var targetPosition = camera.Position + camera.Front * _planeOffset;

        // Check if the target position is below the plane
        // If so, adjust it to be above the plane
        // This is a simple fix to prevent objects from being dragged below the plane
        // and causing issues with collision resolution
        var ray = new Ray(camera.Position.ToEngine(), camera.Front.ToEngine());
        // TODO: replace with the actual plane in the scene
        var plane = new CollisionPlane { Direction = Vector3.UnitY.ToEngine(), Offset = 0 };
        if (RayIntersection.IntersectRayPlane(ray, plane, out var distance))
        {
            if (distance < _planeOffset)
            {
                // The target position is below the plane
                // We clamp the position to be above the plane
                // The resolution should take care of further adjustment
                if (targetPosition.Y < 0)
                {
                    targetPosition.Y = 0.01f;
                }
            }
        }

        DraggedObject.Position = targetPosition;

        // Handle automatic pinning for cloth particles
        if (DraggedObject is ClothParticleWrapperGizmoAdapter draggingAdapter)
        {
            // While dragging, ensure velocity and forces are zeroed out to prevent fighting
            draggingAdapter.Wrapper.Particle.Body.Velocity = Engine.Vector3.Zero;
            draggingAdapter.Wrapper.Particle.Body.ClearAccumulators();

            HandleClothParticlePinning(draggingAdapter.Wrapper);
        }

        OnObjectDragged?.Invoke();
        return true;
    }

    /// <summary>
    /// Handles automatic pinning/unpinning of cloth particles to box corners during dragging.
    /// </summary>
    private void HandleClothParticlePinning(ClothParticleWrapper particleWrapper)
    {
        var boxes = _boxesProvider().ToList(); // Materialize to avoid multiple enumeration

        // TODO: optimize to only the boxes that are close enough i.e. potentially in range of the particle?
        Dictionary<int, IBoxable> bvhDictionary = new();
        int idCounter = 0;
        foreach (var box in boxes)
        {
            var localCornerPositions = box.EngineBox.GetCornersInLocalSpace();
            var cornerPositions = box.EngineBox.GetCornersInWorldSpace();
            for (int i = 0; i < cornerPositions.Length; i++)
            {
                bvhDictionary.Add(idCounter + i,
                    new BoxCorner(localCornerPositions[i], cornerPositions[i], box.EngineBox, i));
            }

            idCounter += 8;
        }

        BVH bvh = BVH.BuildSynchronous(bvhDictionary);
        int? firstContact = BVH.GetFirstContact(particleWrapper, bvh.root);

        var collidingCorner = firstContact.HasValue ? (BoxCorner?)bvhDictionary[firstContact.Value] : null;

        if (collidingCorner != null)
        {
            // Collision detected - add joint between particle and box corner
            // check if particle already has a joint
            if (particleWrapper.Particle.ConnectedJoint.IsSet)
            {
                return;
            }

            var joint = new Joint(
                particleWrapper.Particle,
                new(),
                collidingCorner.Box,
                collidingCorner.LocalPosition
            );
            var globalJoints = globalJointsProvider();

            int jointIndex = globalJoints.AddJoint(joint);
            var jointData = new ConnectedJointData(joint, jointIndex);

            particleWrapper.Particle.ConnectedJoint = jointData;
            collidingCorner.Box.ConnectedJoints.Add(jointData);
        }
        else
        {
            // No collision - unpin if currently pinned
            // It is assumed that both ends are correctly tracked, so only one side check is needed
            if (!particleWrapper.Particle.ConnectedJoint.IsSet)
                return;

            var globalJoints = globalJointsProvider();
            var jointToRemove = particleWrapper.Particle.ConnectedJoint;
            jointToRemove.Joint?.RemoveFromTrackables();
            globalJoints.RemoveJoint(jointToRemove);
        }
    }
}