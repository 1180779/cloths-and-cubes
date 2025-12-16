global using Real = float;

namespace Engine;

public static class Core
{
    public static bool Debug = false;

    public static Real SleepEpsilon { get; set; } = (Real)0.3;
    public static Real Epsilon { get; set; } = (Real)1e-6;
}