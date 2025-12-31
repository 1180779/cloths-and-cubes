using System.Runtime.CompilerServices;

namespace Visualisation.Core;

public static class EngineMathExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 ToOpenTK(this Engine.Vector3 v)
    {
        return new Vector3(v.X, v.Y, v.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Engine.Vector3 ToEngine(this Vector3 v)
    {
        return new Engine.Vector3(v.X, v.Y, v.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion ToOpenTK(this Engine.Quaternion q)
    {
        return new Quaternion(q.I, q.J, q.K, q.R);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Engine.Quaternion ToEngine(this Quaternion q)
    {
        return new Engine.Quaternion(q.W, q.X, q.Y, q.Z);
    }
}