namespace Visualisation.Core.GameObjects.Scenes;

/// <summary>
/// Root data structure for scene serialization
/// </summary>
public sealed record SceneData
{
    public SceneMetadata Metadata { get; init; } = new();
    public CollisionSettings CollisionSettings { get; init; } = new();
    public PlaneData? Plane { get; init; }
    public List<BoxData> Boxes { get; init; } = new();
    public List<BallData> Balls { get; init; } = new();
    public List<ClothData> Cloths { get; init; } = new();
}

/// <summary>
/// Scene metadata for identification and organization
/// </summary>
public sealed record SceneMetadata
{
    public string Name { get; init; } = "Untitled Scene";
    public string Description { get; init; } = "";
    public DateTime CreatedDate { get; init; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; init; } = DateTime.UtcNow;
    public string Version { get; init; } = "1.0";
    public int TotalObjects { get; init; }
}

public sealed record CollisionSettings
{
    public float Friction { get; init; }
    public float Restitution { get; init; }
    public float Tolerance { get; init; }
}

public sealed record RigidBodyData
{
    public float InverseMass { get; init; }
    public Vector3Data Position { get; init; } = new(0, 0, 0);
    public QuaternionData Orientation { get; init; } = new(0, 0, 0, 1);
    public Vector3Data Velocity { get; init; } = new(0, 0, 0);
    public Vector3Data Rotation { get; init; } = new(0, 0, 0);
    public bool CanSleep { get; init; }
    public Vector3Data Acceleration { get; init; } = new(0, 0, 0);
    public Real AngularDamping { get; init; }
    public Real LinearDamping { get; init; }
    public Matrix3Data InverseInertiaTensor { get; init; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0);
}

public record CollisionPrimitiveData
{
    public RigidBodyData RigidBody { get; init; } = new();

    public Matrix4Data Offset { get; init; } = new(
        0, 0, 0, 0,
        0, 0, 0, 0,
        0, 0, 0, 0);
}

public record CollisionBoxData
{
    public CollisionPrimitiveData CollisionPrimitive { get; init; } = new();
    public Vector3Data HalfSize { get; init; } = new(1, 1, 1);
}

public sealed record EngineBoxData : CollisionBoxData;

public sealed record GameObjectSpecificData
{
    public Guid Id { get; init; }
    public bool Invisible { get; init; }
    public MaterialData Material { get; init; } = new();
}

public sealed record BoxData
{
    public EngineBoxData EngineBox { get; init; } = new();
    public GameObjectSpecificData GameObjectSpecific { get; init; } = new();
}

public record CollisionSphereData
{
    public CollisionPrimitiveData CollisionPrimitive { get; init; } = new();
    public float Radius { get; init; }
}

public sealed record EngineSphereData : CollisionSphereData;

public sealed record BallData
{
    public EngineSphereData CollisionSphere { get; init; } = new();
    public GameObjectSpecificData GameObjectSpecific { get; init; } = new();
}

public sealed record EngineClothData
{
    public int SizeX { get; init; }
    public int SizeY { get; init; }
    public float SpringLength { get; init; }
    public float SpringConstant { get; init; }
    public float ParticleMass { get; init; }

    public Vector3Data Particle0Position { get; init; } = new(0, 0, 0);

    // Optional: store all particle states for exact recreation
    public List<ClothParticleData>? ParticleStates { get; init; }
}

public record CollisionParticleData
{
    public RigidBodyData RigidBody { get; init; } = new();
    public Vector3Data Radius { get; init; } = new(0, 0, 0);
}

public sealed record ClothParticleData : CollisionParticleData
{
    public int ClothParticleX { get; init; }
    public int ClothParticleY { get; init; }
}

public sealed record ClothData
{
    public EngineClothData EngineCloth { get; init; } = new();
    public GameObjectSpecificData GameObjectSpecific { get; init; } = new();
}

public sealed record CollisionPlaneData
{
    public Vector3Data Direction { get; init; } = new(0, 1, 0);
    public float Offset { get; init; }
}

public sealed record PlaneData
{
    public CollisionPlaneData CollisionPlane { get; init; } = new();
    public GameObjectSpecificData GameObjectSpecific { get; init; } = new();
}

public sealed record Matrix3Data(
    float M11,
    float M12,
    float M13,
    float M21,
    float M22,
    float M23,
    float M31,
    float M32,
    float M33
);

public sealed record Matrix4Data(
    float M11,
    float M12,
    float M13,
    float M14,
    float M21,
    float M22,
    float M23,
    float M24,
    float M31,
    float M32,
    float M33,
    float M34
);

public sealed record Vector3Data(float X, float Y, float Z);

public sealed record QuaternionData(float I, float J, float K, float R);

public sealed record MaterialData
{
    public enum MaterialType
    {
        Constant,
        Textured
    }

    public string Name { get; init; } = "";
    public MaterialType Type { get; init; } = MaterialType.Constant;

    // For constant materials
    public Vector3Data? Albedo { get; init; }
    public float? Metallic { get; init; }
    public float? Roughness { get; init; }
    public float? AmbientOcclusion { get; init; }

    // For textured materials
    public string? AlbedoTexturePath { get; init; }
    public string? NormalTexturePath { get; init; }
    public string? MetallicTexturePath { get; init; }
    public string? RoughnessTexturePath { get; init; }
    public string? AoTexturePath { get; init; }
}