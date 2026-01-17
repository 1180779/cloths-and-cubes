namespace Engine.ContactGenerators;

/// <summary>
/// A rigid body keeping track of multiple joints attached to it.
/// </summary>
public interface IBodyWithJoints : IJointTrackable
{
    /// <summary>
    /// Hold the joints attached to this box and particles from cloths. 
    /// </summary>
    public List<ConnectedJointData> ConnectedJoints { get; }

    void IJointTrackable.AddConnectedJoint(ConnectedJointData jointData)
    {
        ConnectedJoints.Add(jointData);
    }

    void IJointTrackable.ClearConnectedJoints()
    {
        ConnectedJoints.Clear();
    }

    void IJointTrackable.RemoveConnectedJoint(Joint joint)
    {
        ConnectedJoints.RemoveAll(jd => jd.Joint == joint);
    }

    void IJointTrackable.UpdateJointIndex(Joint joint, int newIndex)
    {
        for (int i = 0; i < ConnectedJoints.Count; i++)
        {
            if (ConnectedJoints[i].Joint == joint)
            {
                var jointData = ConnectedJoints[i];
                jointData.SetIndex(newIndex);
                ConnectedJoints[i] = jointData;
                break;
            }
        }
    }
}