namespace Visualisation.Core.Display;

public static class MeshManager
{
    public class MeshData
    {
        /// <summary>
        /// Vertex Buffer Object
        /// </summary>
        public int Vbo { get; init; }

        /// <summary>
        /// Vertex Array Object
        /// </summary>
        public int Vao { get; init; }

        /// <summary>
        /// Name used to identify the mesh.
        /// </summary>
        public required string MeshName { get; init; }
    }

    public class InternalMeshData
    {
        public required MeshData PublicMeshData;

        /// <summary>
        /// Number of references to the mesh registered as in use.
        /// </summary>
        public int UsagesCount;

        public static InternalMeshData FromMeshData(MeshData meshData)
        {
            return new InternalMeshData
            {
                PublicMeshData = meshData,
            };
        }
    }

    private static readonly Dictionary<string, InternalMeshData> MeshDataDict = new();
    private static readonly object Lock = new();

    public delegate MeshData InitMeshCallback();

    public delegate void FreeMeshCallback(MeshData meshData);

    /// <summary>
    /// Get the Mesh. Load the mesh first if necessary by calling provided initialization function. 
    /// </summary>
    /// <param name="meshName">Unique ID for the mesh. Can be generated with GetID method.</param>
    /// <param name="initCallback">Callback used to initialize the mesh data. Should register and return VBO and VAO</param>
    /// <returns>MeshData class with mesh data. Copy of the data is returned. </returns>
    /// <exception cref="InvalidOperationException">Provided callback did not return the mesh data.</exception>
    public static MeshData GetOrLoadMesh(string meshName, InitMeshCallback initCallback)
    {
        lock (Lock)
        {
            if (!MeshDataDict.TryGetValue(meshName, out var meshData))
            {
                var initData = initCallback?.Invoke() ??
                    throw new InvalidOperationException("Mesh Data Not Initialized");
                meshData = InternalMeshData.FromMeshData(initData);
                MeshDataDict.Add(meshName, meshData);
            }

            meshData.UsagesCount++;
            return meshData.PublicMeshData;
        }
    }

    public static void FreeMesh(string meshName, FreeMeshCallback freeCallback)
    {
        lock (Lock)
        {
            if (!MeshDataDict.TryGetValue(meshName, out var meshData))
            {
                throw new InvalidOperationException("Mesh Data Not Initialized");
            }

            if (meshData.UsagesCount <= 0)
            {
                throw new InvalidOperationException("Released more times than freed!");
            }

            meshData.UsagesCount--;
            if (meshData.UsagesCount == 0)
            {
                freeCallback?.Invoke(meshData.PublicMeshData);
            }
        }
    }
}