using Engine;
using Engine.Collision;
using OpenTK.Windowing.Common;
using Visualisation.Core.Inputs;

namespace Visualization.UiLayer.Applications;

public abstract class RigidBodyApplication : Application
{
    protected static uint MaxContacts => 256;
    protected CollisionData CollisionData = new();

    protected ContactResolver ContactResolver = new(MaxContacts * 8);

    protected abstract void GenerateContacts();
    protected abstract void UpdateObjects(float duration);
    protected abstract void Reset();

    protected override void Update(float deltaTime)
    {
        if (!DoUpdate)
            return;

        if (deltaTime <= 0.0f)
            return;
        if (deltaTime > 0.05f)
        {
            deltaTime = 0.05f;
        }

        UpdateObjects(deltaTime);

        // Perform the contact generation
        GenerateContacts();

        // Resolve detected contacts
        ContactResolver.ResolveContacts(
            CollisionData.ContactList,
            CollisionData.ContactCount,
            deltaTime
        );

        FrameSaver.SaveFrame(Scene);
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);

        if (!IsFocused)
        {
            return;
        }

        if (InputProvider.IsKeyPressed(InputKey.LeftBracket))
        {
            StepsLimit = true;
        }

        if (StepsLimit)
        {
            if (InputProvider.IsKeyDown(InputKey.D0))
                AvailableSteps = 1;
            if (InputProvider.IsKeyPressed(InputKey.D1))
                AvailableSteps = 1;
            if (InputProvider.IsKeyPressed(InputKey.D2))
                AvailableSteps = 2;
            if (InputProvider.IsKeyPressed(InputKey.D3))
                AvailableSteps = 3;
            if (InputProvider.IsKeyPressed(InputKey.D4))
                AvailableSteps = 4;
            if (InputProvider.IsKeyPressed(InputKey.D5))
                AvailableSteps = 5;
            if (InputProvider.IsKeyPressed(InputKey.D6))
                AvailableSteps = 6;
            if (InputProvider.IsKeyPressed(InputKey.D7))
                AvailableSteps = 7;
            if (InputProvider.IsKeyPressed(InputKey.D8))
                AvailableSteps = 8;
            if (InputProvider.IsKeyPressed(InputKey.D9))
                AvailableSteps = 9;
        }

        if (InputProvider.IsKeyPressed(InputKey.RightBracket))
        {
            StepsLimit = false;
        }

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

        // reset
        if (InputProvider.IsKeyPressed(InputKey.R))
        {
            Reset();
        }
    }

    public RigidBodyApplication(
        int width = DefaultWidth,
        int height = DefaultHeight,
        string title = DefaultTitle) : base(width, height, title)
    {
    }
}