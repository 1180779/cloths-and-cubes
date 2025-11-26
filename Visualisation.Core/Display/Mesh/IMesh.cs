namespace Visualisation.Core.Display.Mesh;

public interface IMesh : IDisposable
{
    public void Render();
}

public class MeshDataEmptyException : Exception
{
    public MeshDataEmptyException() : base("Mesh Data Not Initialized")
    {
    }

    public MeshDataEmptyException(string message) : base(message)
    {
    }
}