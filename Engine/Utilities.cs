namespace Engine;

public static class Utilities
{
    // https://stackoverflow.com/questions/6539571/how-to-resize-multidimensional-2d-array-in-c
    /// <summary>
    /// Resizes a 2D array, preserving existing elements where dimensions overlap.
    /// </summary>
    public static T[,] ResizeArray<T>(T[,] original, int x, int y)
    {
        T[,] newArray = new T[x, y];
        int minX = Math.Min(original.GetLength(0), newArray.GetLength(0));
        int minY = Math.Min(original.GetLength(1), newArray.GetLength(1));

        for (int i = 0; i < minY; ++i)
            Array.Copy(original, i * original.GetLength(0), newArray, i * newArray.GetLength(0), minX);

        return newArray;
    }
}