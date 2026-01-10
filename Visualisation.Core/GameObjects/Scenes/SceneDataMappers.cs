using Engine;
using Engine.Collision;
using Engine.Force;
using Engine.RigidBodies;

using Visualisation.Core.Display.Materials;

namespace Visualisation.Core.GameObjects.Scenes;

/// <summary>
/// Extension methods for mapping between domain models and serialization DTOs
/// </summary>
public static class SceneDataMappers
{
    #region Basic Type Conversions (Vector3, Quaternion, Matrix)

    public static Vector3Data ToData(this Engine.Vector3 v) =>
        new(v.X, v.Y, v.Z);

    public static Engine.Vector3 ToEngine(this Vector3Data v) =>
        new(v.X, v.Y, v.Z);

    public static QuaternionData ToData(this Engine.Quaternion q) =>
        new(q.I, q.J, q.K, q.R);

    public static Engine.Quaternion ToEngine(this QuaternionData q) =>
        new(q.I, q.J, q.K, q.R);

    public static Matrix3Data ToData(this Matrix3 m) =>
        new(
            m.Data[0], m.Data[1], m.Data[2],
            m.Data[3], m.Data[4], m.Data[5],
            m.Data[6], m.Data[7], m.Data[8]
        );

    public static Matrix3 ToEngine(this Matrix3Data m) =>
        new([
            m.M11, m.M12, m.M13,
            m.M21, m.M22, m.M23,
            m.M31, m.M32, m.M33
        ]);

    public static Matrix4Data ToData(this Engine.Matrix4 m) =>
        new(
            m.Data[0], m.Data[1], m.Data[2], m.Data[3],
            m.Data[4], m.Data[5], m.Data[6], m.Data[7],
            m.Data[8], m.Data[9], m.Data[10], m.Data[11]
        );

    public static Engine.Matrix4 ToEngine(this Matrix4Data m) =>
        new([
            m.M11, m.M12, m.M13, m.M14,
            m.M21, m.M22, m.M23, m.M24,
            m.M31, m.M32, m.M33, m.M34
        ]);

    #endregion

    #region RigidBody Mapping

    public static RigidBodyData ToData(
        this RigidBody body)
    {
        return new RigidBodyData
        {
            InverseMass = body.InverseMass,
            Position = body.Position.ToData(),
            Orientation = body.Orientation.ToData(),
            Velocity = body.Velocity.ToData(),
            Rotation = body.Rotation.ToData(),
            CanSleep = body.CanSleep,
            Acceleration = body.Acceleration.ToData(),
            AngularDamping = body.AngularDamping,
            LinearDamping = body.LinearDamping,
            InverseInertiaTensor = body.InverseInertiaTensor.ToData()
        };
    }

    public static void UpdateFromData(this RigidBody body, RigidBodyData data)
    {
        body.InverseMass = data.InverseMass;
        body.Position = data.Position.ToEngine();
        body.Orientation = data.Orientation.ToEngine();
        body.Velocity = data.Velocity.ToEngine();
        body.Rotation = data.Rotation.ToEngine();
        body.CanSleep = data.CanSleep;
        body.Acceleration = data.Acceleration.ToEngine();
        body.AngularDamping = data.AngularDamping;
        body.LinearDamping = data.LinearDamping;
        body.InverseInertiaTensor = data.InverseInertiaTensor.ToEngine();
        body.SetAwake();
        body.CalculateDerivedData();
    }

    public static RigidBody ToRigidBody(this RigidBodyData body)
    {
        var rigidBody = new RigidBody();
        rigidBody.UpdateFromData(body);
        return rigidBody;
    }

    #endregion

    #region Base CollisionPrimitive

    public static CollisionPrimitiveData ToData(this CollisionPrimitive primitive)
    {
        return new CollisionPrimitiveData { RigidBody = primitive.Body.ToData(), Offset = primitive.Offset.ToData() };
    }

    public static void UpdateFromData(this CollisionPrimitive primitive, CollisionPrimitiveData data)
    {
        primitive.Body.UpdateFromData(data.RigidBody);
        primitive.Offset = data.Offset.ToEngine();
        primitive.CalculateInternals();
    }

    public static CollisionPrimitive ToCollisionPrimitive(this CollisionPrimitiveData data)
    {
        var primitive = new CollisionPrimitive();
        primitive.UpdateFromData(data);
        return primitive;
    }

    public static RigidBody ToRigidBody(this CollisionPrimitiveData data)
    {
        return data.RigidBody.ToRigidBody();
    }

    #endregion

    #region CollisionBox

    public static CollisionBoxData ToData(this CollisionBox box)
    {
        return new CollisionBoxData
        {
            CollisionPrimitive = ((CollisionPrimitive)box).ToData(), HalfSize = box.HalfSize.ToData()
        };
    }

    public static void UpdateFromData(this CollisionBox box, CollisionBoxData data)
    {
        box.Body.UpdateFromData(data.CollisionPrimitive.RigidBody);
        box.Offset = data.CollisionPrimitive.Offset.ToEngine();
        box.HalfSize = data.HalfSize.ToEngine();
        box.CalculateInternals();
    }

    public static CollisionBox ToCollisionBox(this CollisionBoxData data)
    {
        var box = new CollisionBox();
        box.UpdateFromData(data);
        return box;
    }

    public static EngineBoxData ToData(this Engine.RigidBodies.Box box)
    {
        return new EngineBoxData
        {
            CollisionPrimitive = ((CollisionBox)box).ToData().CollisionPrimitive, HalfSize = box.HalfSize.ToData()
        };
    }

    public static void UpdateFromData(this Engine.RigidBodies.Box box, EngineBoxData data)
    {
        box.UpdateFromData(data.CollisionPrimitive);
        box.HalfSize = data.HalfSize.ToEngine();
    }

    public static Engine.RigidBodies.Box ToEngineBox(this EngineBoxData data)
    {
        var box = new Engine.RigidBodies.Box();
        box.UpdateFromData(data);
        return box;
    }

    public static BoxData ToData(this Box box)
    {
        return new BoxData
        {
            EngineBox = box.EngineBox.ToData(),
            GameObjectSpecific = new GameObjectSpecificData { Id = box.Id, Material = box.Material.ToData() }
        };
    }

    public static void UpdateFromData(this Box box, BoxData data)
    {
        box.Id = data.GameObjectSpecific.Id;
        box.Material = data.GameObjectSpecific.Material.ToMaterial();
        box.EngineBox.UpdateFromData(data.EngineBox);
    }

    public static Box ToBox(this BoxData data)
    {
        return new Box
        {
            Id = data.GameObjectSpecific.Id,
            Material = data.GameObjectSpecific.Material.ToMaterial(),
            EngineBox = data.EngineBox.ToEngineBox()
        };
    }

    #endregion

    #region CollisionSphere

    public static CollisionSphereData ToData(this CollisionSphere sphere)
    {
        return new CollisionSphereData
        {
            CollisionPrimitive = ((CollisionPrimitive)sphere).ToData(), Radius = sphere.Radius,
        };
    }

    public static void UpdateFromData(this CollisionSphere sphere, CollisionSphereData data)
    {
        sphere.UpdateFromData(data.CollisionPrimitive);
        sphere.Radius = data.Radius;
    }

    public static CollisionSphere ToCollisionSphere(this CollisionSphereData data)
    {
        var sphere = new CollisionSphere();
        sphere.UpdateFromData(data);
        return sphere;
    }

    public static EngineSphereData ToData(this Sphere sphere)
    {
        return new EngineSphereData
        {
            CollisionPrimitive = ((CollisionSphere)sphere).ToData().CollisionPrimitive, Radius = sphere.Radius,
        };
    }

    public static void UpdateFromData(this Sphere sphere, EngineSphereData data)
    {
        sphere.UpdateFromData((CollisionSphereData)data);
    }

    public static Sphere ToEngineSphere(this EngineSphereData data)
    {
        var sphere = new Sphere();
        sphere.UpdateFromData(data);
        return sphere;
    }

    public static BallData ToData(this Ball ball)
    {
        return new BallData
        {
            GameObjectSpecific = new GameObjectSpecificData { Id = ball.Id, Material = ball.Material.ToData() },
            CollisionSphere = ball.EngineBall.ToData()
        };
    }

    public static void UpdateFromData(this Ball ball, BallData data)
    {
        ball.Id = data.GameObjectSpecific.Id;
        ball.Material = data.GameObjectSpecific.Material.ToMaterial();
        ball.EngineBall.UpdateFromData(data.CollisionSphere);
    }

    public static Ball ToBall(this BallData data)
    {
        return new Ball
        {
            Id = data.GameObjectSpecific.Id,
            Material = data.GameObjectSpecific.Material.ToMaterial(),
            EngineBall = data.CollisionSphere.ToEngineSphere()
        };
    }

    #endregion

    #region CollisionParticle

    public static ClothParticleData ToData(this RigidParticle particle)
    {
        return new ClothParticleData { RigidBody = particle.Body.ToData(), Radius = particle.Radius.ToData() };
    }

    public static void UpdateFromData(this RigidParticle particle, ClothParticleData data)
    {
        particle.Body.UpdateFromData(data.RigidBody);
        particle.Radius = data.Radius.ToEngine();
        particle.RefreshPhysicsState();
    }

    public static ClothRigidParticle ToEngineParticle(this ClothParticleData data, Engine.Cloth cloth)
    {
        var particle = new ClothRigidParticle()
        {
            AttachedToCloth = cloth, ClothParticleX = data.ClothParticleX, ClothParticleY = data.ClothParticleY
        };
        particle.UpdateFromData(data);
        return particle;
    }

    #endregion

    #region Cloth

    public static EngineClothData ToData(this Engine.Cloth cloth, bool includeParticleStates = false)
    {
        List<ClothParticleData>? particleStates = null;
        if (includeParticleStates)
        {
            particleStates = new List<ClothParticleData>();
            for (int i = 0; i < cloth.SizeX; i++)
            {
                for (int j = 0; j < cloth.SizeY; j++)
                {
                    var particle = cloth.Particles[i, j];
                    particleStates.Add(particle.ToData());
                }
            }
        }

        return new EngineClothData
        {
            SizeX = cloth.SizeX,
            SizeY = cloth.SizeY,
            SpringLength = cloth.SpringLength,
            SpringConstant = cloth.SpringConstant,
            ParticleMass = cloth.ParticleMass,
            Particle0Position = cloth.Particle0Pos.ToData(),
            ParticleStates = particleStates
        };
    }

    public static ClothData ToData(this Cloth cloth, bool includeParticleStates = false)
    {
        return new ClothData
        {
            EngineCloth = cloth.EngineCloth.ToData(includeParticleStates),
            GameObjectSpecific = new GameObjectSpecificData { Id = cloth.Id, Material = cloth.Material.ToData() }
        };
    }

    public static void UpdateFromData(this Cloth cloth, ClothData data, ForceRegistry forceRegistry)
    {
        cloth.Id = data.GameObjectSpecific.Id;
        cloth.Material = data.GameObjectSpecific.Material.ToMaterial();

        // Check if cloth dimensions match - if not, we need to regenerate
        bool needsRegeneration = cloth.EngineCloth.SizeX != data.EngineCloth.SizeX ||
            cloth.EngineCloth.SizeY != data.EngineCloth.SizeY;

        if (needsRegeneration)
        {
            // Remove old springs from registry
            cloth.EngineCloth.RemoveSpringsFromForceRegistry();

            // Regenerate cloth with new dimensions
            cloth.RegenerateClothPreservingTheCenter(
                data.EngineCloth.SizeX,
                data.EngineCloth.SizeY,
                data.EngineCloth.SpringLength,
                data.EngineCloth.SpringConstant,
                data.EngineCloth.ParticleMass);
        }

        // Set the origin position
        var offset = data.EngineCloth.Particle0Position.ToEngine() - cloth.EngineCloth.Particle0Pos;
        if (offset.SquareMagnitude() > 0.001f)
        {
            cloth.EngineCloth.Move(offset);
        }

        // Restore particle states if available
        if (data.EngineCloth.ParticleStates != null &&
            data.EngineCloth.ParticleStates.Count == data.EngineCloth.SizeX * data.EngineCloth.SizeY)
        {
            int index = 0;
            for (int i = 0; i < data.EngineCloth.SizeX; i++)
            {
                for (int j = 0; j < data.EngineCloth.SizeY; j++)
                {
                    var particleData = data.EngineCloth.ParticleStates[index++];
                    cloth.EngineCloth.Particles[i, j].UpdateFromData(particleData);
                }
            }
        }
        else
        {
            // Even without particle states, ensure all particles are awake
            for (int i = 0; i < cloth.EngineCloth.SizeX; i++)
            {
                for (int j = 0; j < cloth.EngineCloth.SizeY; j++)
                {
                    cloth.EngineCloth.Particles[i, j].Body.SetAwake();
                }
            }
        }
    }

    public static Cloth ToCloth(this ClothData data, ForceRegistry forceRegistry, Func<float> positionEpsilonProvider)
    {
        var cloth = new Cloth(
            forceRegistry,
            positionEpsilonProvider,
            data.EngineCloth.SizeX,
            data.EngineCloth.SizeY,
            data.EngineCloth.SpringLength,
            data.EngineCloth.SpringConstant,
            data.EngineCloth.ParticleMass)
        {
            Id = data.GameObjectSpecific.Id, Material = data.GameObjectSpecific.Material.ToMaterial()
        };

        // Set the origin position
        var offset = data.EngineCloth.Particle0Position.ToEngine() - cloth.EngineCloth.Particle0Pos;
        if (offset.SquareMagnitude() > 0.001f)
        {
            cloth.EngineCloth.Move(offset);
        }

        // Restore particle states if available
        if (data.EngineCloth.ParticleStates != null &&
            data.EngineCloth.ParticleStates.Count == data.EngineCloth.SizeX * data.EngineCloth.SizeY)
        {
            int index = 0;
            for (int i = 0; i < data.EngineCloth.SizeX; i++)
            {
                for (int j = 0; j < data.EngineCloth.SizeY; j++)
                {
                    var particleData = data.EngineCloth.ParticleStates[index++];
                    cloth.EngineCloth.Particles[i, j] = particleData.ToEngineParticle(cloth.EngineCloth);
                }
            }
        }
        else
        {
            // Even without particle states, ensure all particles are awake
            for (int i = 0; i < cloth.EngineCloth.SizeX; i++)
            {
                for (int j = 0; j < cloth.EngineCloth.SizeY; j++)
                {
                    cloth.EngineCloth.Particles[i, j].Body.SetAwake();
                }
            }
        }

        return cloth;
    }

    #endregion

    #region CollisionPlane

    public static CollisionPlaneData ToData(this CollisionPlane plane)
    {
        return new CollisionPlaneData { Direction = plane.Direction.ToData(), Offset = plane.Offset };
    }

    public static CollisionPlane ToCollisionPlane(this CollisionPlaneData data)
    {
        return new CollisionPlane { Direction = data.Direction.ToEngine(), Offset = data.Offset };
    }

    public static PlaneData ToData(this Plane plane)
    {
        return new PlaneData
        {
            CollisionPlane = plane.EnginePlane.ToData(),
            GameObjectSpecific = new GameObjectSpecificData { Id = plane.Id, Material = plane.Material.ToData() }
        };
    }

    public static void UpdateFromData(this Plane plane, PlaneData data)
    {
        plane.Id = data.GameObjectSpecific.Id;
        plane.Material = data.GameObjectSpecific.Material.ToMaterial();
        plane.EnginePlane = data.CollisionPlane.ToCollisionPlane();
    }

    public static Plane ToPlane(this PlaneData data)
    {
        var plane = new Plane
        {
            Id = data.GameObjectSpecific.Id,
            Material = data.GameObjectSpecific.Material.ToMaterial(),
            EnginePlane = data.CollisionPlane.ToCollisionPlane()
        };

        return plane;
    }

    #endregion

    #region Material Mapping

    public static MaterialData ToData(this IMaterial material)
    {
        return material switch
        {
            MaterialConstant constant => new MaterialData
            {
                Name = constant.Name,
                Type = MaterialData.MaterialType.Constant,
                Albedo = new Vector3Data(constant.Albedo.X, constant.Albedo.Y, constant.Albedo.Z),
                Metallic = constant.Metallic,
                Roughness = constant.Roughness,
                AmbientOcclusion = constant.Ao
            },
            MaterialTextured textured => new MaterialData
            {
                Name = textured.Name,
                Type = MaterialData.MaterialType.Textured,
                AlbedoTexturePath = textured.AlbedoMap,
                NormalTexturePath = textured.NormalMap,
                MetallicTexturePath = textured.MetallicMap,
                RoughnessTexturePath = textured.RoughnessMap,
                AoTexturePath = textured.AoMap,
            },
            _ => new MaterialData { Name = "Unknown", Type = MaterialData.MaterialType.Constant }
        };
    }

    public static IMaterial ToMaterial(this MaterialData data)
    {
        return data.Type switch
        {
            MaterialData.MaterialType.Constant => new MaterialConstant
            {
                Name = data.Name,
                Albedo =
                    data.Albedo is not null
                        ? new Vector3(data.Albedo.X, data.Albedo.Y, data.Albedo.Z)
                        : new Vector3(),
                Metallic = data.Metallic ?? 0.0f,
                Roughness = data.Roughness ?? 1.0f,
                Ao = data.AmbientOcclusion ?? 1.0f
            },
            MaterialData.MaterialType.Textured => new MaterialTextured
            {
                Name = data.Name,
                AlbedoMap = data.AlbedoTexturePath ?? "",
                NormalMap = data.NormalTexturePath ?? "",
                MetallicMap = data.MetallicTexturePath ?? "",
                RoughnessMap = data.RoughnessTexturePath ?? "",
                AoMap = data.AoTexturePath ?? ""
            },
            _ => new MaterialConstant()
        };
    }

    #endregion

    #region Scene-level Mapping

    public static SceneData ToSceneData(
        this IEnumerable<GameObject> gameObjects,
        CollisionData collisionData,
        string sceneName = "Untitled Scene",
        string description = "",
        bool includeParticleStates = false)
    {
        var boxes = new List<BoxData>();
        var balls = new List<BallData>();
        var cloths = new List<ClothData>();
        PlaneData? plane = null;

        int count = 0;
        foreach (var obj in gameObjects)
        {
            count++;
            switch (obj)
            {
                case Box box:
                    boxes.Add(box.ToData());
                    break;
                case Ball ball:
                    balls.Add(ball.ToData());
                    break;
                case Cloth cloth:
                    cloths.Add(cloth.ToData(includeParticleStates));
                    break;
                case Plane p:
                    plane = p.ToData();
                    break;
            }
        }

        return new SceneData
        {
            Metadata = new SceneMetadata
            {
                Name = sceneName,
                Description = description,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                Version = "1.0",
                TotalObjects = count
            },
            CollisionSettings = new CollisionSettings
            {
                Friction = collisionData.Friction,
                Restitution = collisionData.Restitution,
                Tolerance = collisionData.Tolerance
            },
            Plane = plane,
            Boxes = boxes,
            Balls = balls,
            Cloths = cloths
        };
    }

    public static List<GameObject> ToGameObjects(
        this SceneData sceneData,
        ForceRegistry forceRegistry,
        Func<float> positionEpsilonProvider,
        out Plane? plane,
        out CollisionData collisionData)
    {
        var gameObjects = new List<GameObject>();

        collisionData = new CollisionData
        {
            Friction = sceneData.CollisionSettings.Friction,
            Restitution = sceneData.CollisionSettings.Restitution,
            Tolerance = sceneData.CollisionSettings.Tolerance
        };

        plane = sceneData.Plane?.ToPlane();
        if (plane != null)
        {
            gameObjects.Add(plane);
        }

        // Create game objects
        gameObjects.AddRange(sceneData.Boxes.Select(b => b.ToBox()));
        gameObjects.AddRange(sceneData.Balls.Select(b => b.ToBall()));
        gameObjects.AddRange(sceneData.Cloths.Select(c => c.ToCloth(forceRegistry, positionEpsilonProvider)));

        return gameObjects;
    }

    /// <summary>
    /// Updates existing GameObjects from SceneData or creates new ones if they don't exist.
    /// This preserves object identity, which is important for maintaining gizmo and selection references.
    /// </summary>
    public static List<GameObject> ToGameObjectsWithUpdate(
        this SceneData sceneData,
        ForceRegistry forceRegistry,
        ICollection<GameObject> existingObjects,
        Func<float> positionEpsilonProvider,
        out Plane? plane,
        out CollisionData collisionData,
        out List<GameObject> objectsToRemove)
    {
        var gameObjects = new List<GameObject>();
        objectsToRemove = new List<GameObject>();

        var existingById = existingObjects.ToDictionary(obj => obj.Id, obj => obj);
        var idsInSceneData = new HashSet<Guid>();

        collisionData = new CollisionData
        {
            Friction = sceneData.CollisionSettings.Friction,
            Restitution = sceneData.CollisionSettings.Restitution,
            Tolerance = sceneData.CollisionSettings.Tolerance
        };

        // Handle plane
        plane = null;
        if (sceneData.Plane != null)
        {
            idsInSceneData.Add(sceneData.Plane.GameObjectSpecific.Id);

            if (existingById.TryGetValue(sceneData.Plane.GameObjectSpecific.Id, out var existingPlane)
                && existingPlane is Plane planeObj)
            {
                planeObj.UpdateFromData(sceneData.Plane);
                plane = planeObj;
            }
            else
            {
                plane = sceneData.Plane.ToPlane();
            }

            gameObjects.Add(plane);
        }

        // Handle boxes
        foreach (var boxData in sceneData.Boxes)
        {
            idsInSceneData.Add(boxData.GameObjectSpecific.Id);

            if (existingById.TryGetValue(boxData.GameObjectSpecific.Id, out var existingObj)
                && existingObj is Box existingBox)
            {
                existingBox.UpdateFromData(boxData);
                gameObjects.Add(existingBox);
            }
            else
            {
                gameObjects.Add(boxData.ToBox());
            }
        }

        // Handle balls
        foreach (var ballData in sceneData.Balls)
        {
            idsInSceneData.Add(ballData.GameObjectSpecific.Id);

            if (existingById.TryGetValue(ballData.GameObjectSpecific.Id, out var existingObj)
                && existingObj is Ball existingBall)
            {
                existingBall.UpdateFromData(ballData);
                gameObjects.Add(existingBall);
            }
            else
            {
                gameObjects.Add(ballData.ToBall());
            }
        }

        // Handle cloths
        foreach (var clothData in sceneData.Cloths)
        {
            idsInSceneData.Add(clothData.GameObjectSpecific.Id);

            if (existingById.TryGetValue(clothData.GameObjectSpecific.Id, out var existingObj)
                && existingObj is Cloth existingCloth)
            {
                existingCloth.UpdateFromData(clothData, forceRegistry);
                gameObjects.Add(existingCloth);
            }
            else
            {
                gameObjects.Add(clothData.ToCloth(forceRegistry, positionEpsilonProvider));
            }
        }

        // Identify objects that need to be removed
        foreach (var existingObj in existingObjects)
        {
            if (!idsInSceneData.Contains(existingObj.Id))
            {
                objectsToRemove.Add(existingObj);
            }
        }

        return gameObjects;
    }

    #endregion
}