using Engine;
using Engine.Collision;

using OpenTK.Windowing.Common;

using Visualisation.Core.Inputs;

namespace Visualization.UiLayer.Applications;

public abstract class RigidBodyApplication : Application
{
    private static bool s_fpsCappedTo60;
    protected static uint MaxContacts => 2 * 1024;

    protected CollisionData _collisionData = new()
    {
        Friction = (Real)0.9, Restitution = (Real)0.6, Tolerance = (Real)0.1,
    };

    protected ContactResolver _contactResolver = new(MaxContacts * 8);

    protected abstract void GenerateContacts();
    protected abstract void UpdateObjects(float duration);
    protected abstract void Reset();

    protected virtual void OnNoPhysicsUpdate() { }

    protected override void Update(float deltaTime)
    {
        if (!DoUpdate)
        {
            OnNoPhysicsUpdate();
            return;
        }

        switch (deltaTime)
        {
            case <= 0.0f:
                return;
            case > 0.05f:
                deltaTime = 0.05f;
                break;
        }

        UpdateObjects(deltaTime);

        // Perform the contact generation
        GenerateContacts();
        
        // Resolve detected contacts
        _contactResolver.ResolveContacts(
            _collisionData.ContactList,
            _collisionData.ContactCount,
            deltaTime
        );
#if DEBUG

#if FRAMESAVER
        FrameSaver.SaveFrame(Scene);
#endif
#endif
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);

        if (!IsFocused)
        {
            return;
        }

        if (_inputProvider.IsKeyPressed(InputKey.LeftBracket))
        {
            StepsLimit = true;
        }

        if (StepsLimit)
        {
            if (_inputProvider.IsKeyDown(InputKey.D0))
                AvailableSteps = 1;
            if (_inputProvider.IsKeyPressed(InputKey.D1))
                AvailableSteps = 1;
            if (_inputProvider.IsKeyPressed(InputKey.D2))
                AvailableSteps = 2;
            if (_inputProvider.IsKeyPressed(InputKey.D3))
                AvailableSteps = 3;
            if (_inputProvider.IsKeyPressed(InputKey.D4))
                AvailableSteps = 4;
            if (_inputProvider.IsKeyPressed(InputKey.D5))
                AvailableSteps = 5;
            if (_inputProvider.IsKeyPressed(InputKey.D6))
                AvailableSteps = 6;
            if (_inputProvider.IsKeyPressed(InputKey.D7))
                AvailableSteps = 7;
            if (_inputProvider.IsKeyPressed(InputKey.D8))
                AvailableSteps = 8;
            if (_inputProvider.IsKeyPressed(InputKey.D9))
                AvailableSteps = 9;
        }

        if (_inputProvider.IsKeyPressed(InputKey.RightBracket))
        {
            StepsLimit = false;
        }

#if DEBUG
#if FRAMESAVER
        // frame saver
        if (InputProvider.IsKeyDown(InputKey.Right))
        {
            FrameSaver.GoForwardNFrames(1);
            FrameSaver.CurrentFrame?.Restore(Scene);
        }

        if (InputProvider.IsKeyDown(InputKey.Left))
        {
            FrameSaver.GoBackNFrames(1);
            FrameSaver.CurrentFrame?.Restore(Scene);
        }
#endif
#endif

        // reset
        if (_inputProvider.IsKeyPressed(InputKey.R))
        {
            Reset();
        }

        // cap/uncap fps
        if (_inputProvider.IsKeyPressed(InputKey.X))
        {
            UpdateFrequency = UpdateFrequency switch
            {
                0.0 => 60.0,
                _ => 0.0
            };

            s_fpsCappedTo60 = !s_fpsCappedTo60;
        }
    }

    public RigidBodyApplication(
        int width = DefaultWidth,
        int height = DefaultHeight,
        string title = DefaultTitle) : base(width, height, title)
    {
        if (s_fpsCappedTo60) UpdateFrequency = 60.0;
    }
}