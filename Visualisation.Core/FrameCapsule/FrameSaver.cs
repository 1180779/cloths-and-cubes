using System.Diagnostics;
using Visualisation.Core.Display.Mesh.VisualObjects;
using Visualisation.Core.GameObjects.Scenes;

namespace Visualisation.Core.FrameCapsule;

public struct FrameSnapshot
{
    public FrameSnapshot(IVisualObject[] gameObjects)
    {
        GameObjects = gameObjects;
    }

    public IVisualObject[] GameObjects { get; set; }

    public void Restore(SceneManager scene)
    {
        // 1. Create a fast-lookup map of the CURRENT live objects by their unique ID.
        // This is the key to decoupling from list order and count.
        Dictionary<Guid, IVisualObject> liveObjectsMap =
            scene.GameObjects.ToDictionary(go => go.Id, go => go);

        // 2. Iterate through the objects in the snapshot.
        foreach (IVisualObject snapshotObject in GameObjects)
        {
            // 3. Find the corresponding live object in the map using the snapshot's ID.
            if (liveObjectsMap.TryGetValue(snapshotObject.Id, out IVisualObject liveObject))
            {
                // 4. We found a match! Restore the state.
                ObjectStateRestorer.RestoreStateFrom(liveObject, snapshotObject);
            }
            else
            {
                // This object existed in the snapshot but has since been destroyed
                // in the live scene. We can safely ignore it.
                // You could add logging here if needed:
                // Console.WriteLine($"Could not find live object with ID {snapshotObject.Id} to restore.");
            }
        }

        // Note: Any new objects created in the live scene since the snapshot was taken
        // will simply be ignored, as they won't have a corresponding entry in the snapshot.
        // This is usually the desired behavior.
    }
}

public class FrameSaver
{
    public FrameSaver()
    {
    }

    public FrameSaver(int frameBufferLimit)
    {
        FrameBufferLimit = frameBufferLimit;
    }

    private int currentFrameIndex = 0;

    public int CurrentFrameIndex
    {
        get => currentFrameIndex;
        set
        {
            currentFrameIndex = value;
            // wrap around the buffer if necessary
            if (currentFrameIndex >= FrameBufferLimit || currentFrameIndex < 0)
            {
                currentFrameIndex %= FrameBufferLimit;
                if (currentFrameIndex < 0)
                {
                    currentFrameIndex += FrameBufferLimit;
                }
            }
        }
    }

    private int NextIndexToSaveTo
    {
        get
        {
            if (NotYetUsedIndex == NotYetUsedIndexAllUsedValue)
            {
                return (savedFrameIndex + 1) % FrameBufferLimit;
            }

            var res = NotYetUsedIndex;
            NotYetUsedIndex++;
            return res;
        }
    }

    private const int NotYetUsedIndexAllUsedValue = -1;
    private int notYetUsedIndex = 0;

    private int NotYetUsedIndex
    {
        get => notYetUsedIndex;
        set
        {
            notYetUsedIndex = value;
            if (notYetUsedIndex >= FrameBufferLimit)
            {
                notYetUsedIndex = -1;
            }
        }
    }

    private int savedFrameIndex = -1;

    private FrameSnapshot[] frameBuffer = [];

    public FrameSnapshot? CurrentFrame => FrameBufferLimit == 0 ? null : frameBuffer[CurrentFrameIndex];

    public int FrameBufferLimit
    {
        get => frameBuffer.Length;
        set
        {
            if (frameBuffer.Length > value)
            {
                frameBuffer = [..frameBuffer.Skip(CurrentFrameIndex - value).Take(value)];
            }
            else if (frameBuffer.Length < value)
            {
                Array.Resize(ref frameBuffer, value);
                NotYetUsedIndex = savedFrameIndex + 1;
            }
        }
    }

    public void SaveFrame(SceneManager scene)
    {
        if (FrameBufferLimit == 0)
            return;

        CurrentFrameIndex = NextIndexToSaveTo;
        // TODO: do something with the nullability of the CreateDeepCopy method return value
        FrameSnapshot snapshot = new(scene.GameObjects.Select(go =>
        {
            var clone = ObjectCloner.CreateDeepCopy(go);
            if (clone is { })
            {
                return clone;
            }

            return null;
        }).Where(o => o != null).ToArray());
        frameBuffer[CurrentFrameIndex] = snapshot;
        savedFrameIndex = CurrentFrameIndex;
    }

    public void GoBackNFrames(int n)
    {
        if (CurrentFrameIndex <= savedFrameIndex)
        {
            int maxGoBack;
            if (NotYetUsedIndex != NotYetUsedIndexAllUsedValue)
            {
                maxGoBack = CurrentFrameIndex;
            }
            else
            {
                maxGoBack = FrameBufferLimit - (savedFrameIndex - CurrentFrameIndex);
            }

            if (n > maxGoBack)
            {
                n = maxGoBack;
            }

            CurrentFrameIndex -= n;
            Debug.Assert(CurrentFrameIndex >= 0 && CurrentFrameIndex < FrameBufferLimit);
            Debug.Assert(NotYetUsedIndex == NotYetUsedIndexAllUsedValue || CurrentFrameIndex < NotYetUsedIndex);
        }
        else if (CurrentFrameIndex > savedFrameIndex)
        {
            int maxGoBack = CurrentFrameIndex - savedFrameIndex - 1;
            if (n > maxGoBack)
            {
                n = maxGoBack;
            }

            CurrentFrameIndex -= n;
            Debug.Assert(CurrentFrameIndex >= 0 && CurrentFrameIndex < FrameBufferLimit);
            Debug.Assert(NotYetUsedIndex == NotYetUsedIndexAllUsedValue || CurrentFrameIndex < NotYetUsedIndex);
        }
    }

    public void GoForwardNFrames(int n)
    {
        if (CurrentFrameIndex < savedFrameIndex)
        {
            int maxGoForward = savedFrameIndex - CurrentFrameIndex - 1;
            if (n > maxGoForward)
            {
                n = maxGoForward;
            }

            CurrentFrameIndex += n;
            Debug.Assert(CurrentFrameIndex >= 0 && CurrentFrameIndex < FrameBufferLimit);
            Debug.Assert(NotYetUsedIndex == NotYetUsedIndexAllUsedValue || CurrentFrameIndex < NotYetUsedIndex);
        }
        else if (CurrentFrameIndex >= savedFrameIndex)
        {
            int maxGoForward;
            if (NotYetUsedIndex != NotYetUsedIndexAllUsedValue)
            {
                maxGoForward = NotYetUsedIndex - currentFrameIndex;
            }
            else
            {
                maxGoForward = FrameBufferLimit - (savedFrameIndex - CurrentFrameIndex);
            }

            if (n > maxGoForward)
            {
                n = maxGoForward;
            }

            CurrentFrameIndex += n;
            Debug.Assert(CurrentFrameIndex >= 0 && CurrentFrameIndex < FrameBufferLimit);
            Debug.Assert(NotYetUsedIndex == NotYetUsedIndexAllUsedValue || CurrentFrameIndex < NotYetUsedIndex);
        }
    }
}