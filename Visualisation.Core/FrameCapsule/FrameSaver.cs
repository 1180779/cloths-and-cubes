using System.Diagnostics;

using Visualisation.Core.Display.Mesh.VisualObjects;
using Visualisation.Core.GameObjects.Scenes;

namespace Visualisation.Core.FrameCapsule;

/// <summary>
/// Represents a snapshot of the frame, containing the state of the visual objects
/// present in the scene at a specific point in time. This allows storing and
/// restoring the state of the scene as it existed during the snapshot's creation.
/// </summary>
public struct FrameSnapshot
{
    public FrameSnapshot(GameObject[] gameObjects)
    {
        GameObjects = gameObjects;
    }

    public GameObject[] GameObjects { get; set; }

    public void Restore(SceneManager scene)
    {
        Dictionary<Guid, GameObject> liveObjectsMap =
            scene.GameObjects.ToDictionary(go => go.Id, go => go);

        foreach (GameObject snapshotObject in GameObjects)
        {
            if (liveObjectsMap.TryGetValue(snapshotObject.Id, out GameObject liveObject))
            {
                ObjectStateRestorer.RestoreStateFrom(liveObject, snapshotObject);
            }
        }
    }
}

/// <summary>
/// Facilitates the saving and restoring of frames by maintaining a buffer of frame snapshots.
/// It allows navigation through saved frames to either analyze or restore past states of the scene.
///
/// Not intended to be used in the final product. 
/// </summary>
public class FrameSaver
{
    public FrameSaver(int frameBufferLimit)
    {
        FrameBufferLimit = frameBufferLimit;
    }

    private int _currentFrameIndex = 0;

    public int CurrentFrameIndex
    {
        get => _currentFrameIndex;
        set
        {
            _currentFrameIndex = value;
            // wrap around the buffer if necessary
            if (_currentFrameIndex < FrameBufferLimit && _currentFrameIndex >= 0) return;
            _currentFrameIndex %= FrameBufferLimit;
            if (_currentFrameIndex >= 0) return;
            _currentFrameIndex += FrameBufferLimit;
        }
    }

    private int NextIndexToSaveTo
    {
        get
        {
            if (NotYetUsedIndex == NotYetUsedIndexAllUsedValue)
            {
                return (_savedFrameIndex + 1) % FrameBufferLimit;
            }

            var res = NotYetUsedIndex;
            NotYetUsedIndex++;
            return res;
        }
    }

    private const int NotYetUsedIndexAllUsedValue = -1;
    private int _notYetUsedIndex = 0;

    private int NotYetUsedIndex
    {
        get => _notYetUsedIndex;
        set
        {
            _notYetUsedIndex = value;
            if (_notYetUsedIndex >= FrameBufferLimit)
            {
                _notYetUsedIndex = -1;
            }
        }
    }

    private int _savedFrameIndex = -1;

    private FrameSnapshot[] _frameBuffer = [];

    public FrameSnapshot? CurrentFrame => FrameBufferLimit == 0 ? null : _frameBuffer[CurrentFrameIndex];

    public int FrameBufferLimit
    {
        get => _frameBuffer.Length;
        set
        {
            if (_frameBuffer.Length > value)
            {
                _frameBuffer = [.._frameBuffer.Skip(CurrentFrameIndex - value).Take(value)];
            }
            else if (_frameBuffer.Length < value)
            {
                Array.Resize(ref _frameBuffer, value);
                NotYetUsedIndex = _savedFrameIndex + 1;
            }
        }
    }

    public void SaveFrame(SceneManager scene)
    {
        if (FrameBufferLimit == 0)
            return;

        CurrentFrameIndex = NextIndexToSaveTo;
        FrameSnapshot snapshot = new(scene.GameObjects.Select(go =>
        {
            var clone = ObjectCloner.CreateDeepCopy(go);
            return clone ?? null;
        }).Where(o => o != null).ToArray()!);
        _frameBuffer[CurrentFrameIndex] = snapshot;
        _savedFrameIndex = CurrentFrameIndex;
    }

    public void GoBackNFrames(int n)
    {
        if (CurrentFrameIndex <= _savedFrameIndex)
        {
            int maxGoBack;
            if (NotYetUsedIndex != NotYetUsedIndexAllUsedValue)
            {
                maxGoBack = CurrentFrameIndex;
            }
            else
            {
                maxGoBack = FrameBufferLimit - (_savedFrameIndex - CurrentFrameIndex);
            }

            if (n > maxGoBack)
            {
                n = maxGoBack;
            }

            CurrentFrameIndex -= n;
            Debug.Assert(CurrentFrameIndex >= 0 && CurrentFrameIndex < FrameBufferLimit);
            Debug.Assert(NotYetUsedIndex == NotYetUsedIndexAllUsedValue || CurrentFrameIndex < NotYetUsedIndex);
        }
        else if (CurrentFrameIndex > _savedFrameIndex)
        {
            int maxGoBack = CurrentFrameIndex - _savedFrameIndex - 1;
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
        if (CurrentFrameIndex < _savedFrameIndex)
        {
            int maxGoForward = _savedFrameIndex - CurrentFrameIndex - 1;
            if (n > maxGoForward)
            {
                n = maxGoForward;
            }

            CurrentFrameIndex += n;
            Debug.Assert(CurrentFrameIndex >= 0 && CurrentFrameIndex < FrameBufferLimit);
            Debug.Assert(NotYetUsedIndex == NotYetUsedIndexAllUsedValue || CurrentFrameIndex < NotYetUsedIndex);
        }
        else if (CurrentFrameIndex >= _savedFrameIndex)
        {
            int maxGoForward;
            if (NotYetUsedIndex != NotYetUsedIndexAllUsedValue)
            {
                maxGoForward = NotYetUsedIndex - _currentFrameIndex;
            }
            else
            {
                maxGoForward = FrameBufferLimit - (_savedFrameIndex - CurrentFrameIndex);
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