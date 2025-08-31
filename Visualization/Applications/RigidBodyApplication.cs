using Engine;
using Engine.Collision;
using OpenTK.Windowing.Common;
using Visualization.Display.Inputs;

namespace Visualization.Applications;

public abstract class RigidBodyApplication : Application
{
    protected static uint MaxContacts => 256;
    protected CollisionData CollisionData = new();
    protected ContactResolver ContactResolver = new(MaxContacts * 8);
    // TODO: implement this after adding some ui elements
    // protected bool RenderDebugInfo = false;
    // protected bool PauseSimulation = false;
    // protected bool AutoPauseSimulation = false;
    protected abstract void GenerateContacts();
    protected abstract void UpdateObjects(float duration);
    protected abstract void Reset();

    private int simulationStep = 0;
    protected override void Update(float deltaTime)
    {
        if (deltaTime <= 0.0f) 
            return;
        if (deltaTime > 0.05f)
        {
            deltaTime = 0.05f;
        }
        
        simulationStep++;
        // Console.WriteLine($"Simulation step: {simulationStep}");
        UpdateObjects(deltaTime);

        // Perform the contact generation
        GenerateContacts();

        // Resolve detected contacts
        ContactResolver.ResolveContacts(
            CollisionData.ContactList,
            CollisionData.ContactCount,
            deltaTime
        );
    }
    
    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);

        if (!IsFocused) // check to see if the window is focused
        {
            return;
        }

        if (InputProvider.IsKeyDown(InputKey.R))
        {
            Reset();
        }
    }
    
    public RigidBodyApplication(int width = DefaultWidth, int height = DefaultHeight, string title = DefaultTitle) : base(width, height, title) { }
    
}