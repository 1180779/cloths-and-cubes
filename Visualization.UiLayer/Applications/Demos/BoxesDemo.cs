using Engine.Collision;
using Engine.Collision.Bounding_Volume_Hierarchy;
using Engine.Force;

using Visualisation.Core;
using Visualisation.Core.GameObjects;

using Visualization.UiLayer.UI.Windows;

using Random = Engine.Random;

namespace Visualization.UiLayer.Applications.Demos;

public class BoxesDemo : RigidBodyApplication
{
    protected Box[] _boxes = [];
    protected Ball[] _balls = [];
    protected Plane _plane = null!; // initialized in scene initialization

    protected ForceRegistry _forceRegistry = new();
    protected Cloth _cloth = null!; // initialized in scene initialization

    protected BvhNodesWindow _bvhNodesWindow = new();
    protected CollisionParametersWindow _collisionParametersWindow;

    protected BoxesDemoSettingsWindow
        _boxesDemoSettingsWindow =
            new(0, 0); // the delegates need to be initialized in the constructor

    public BoxesDemo()
    {
        _collisionParametersWindow = new(_collisionData);

        _boxesDemoSettingsWindow.SetBoxesCount = count =>
        {
            Random random = new();
            int length = _boxes.Length;
            if (count < length)
            {
                for (int i = count; i < length; ++i)
                {
                    _sceneManager.RemoveGameObject(_boxes[i]);
                }
            }

            Array.Resize(ref _boxes, count);
            if (count > length)
            {
                for (int i = length; i < count; ++i)
                {
                    _boxes[i] = new Box();
                    _boxes[i].EngineBox.Random(random);
                    _sceneManager.AddGameObject(_boxes[i]);
                }
            }
        };
        _boxesDemoSettingsWindow.SetSpheresCount = count =>
        {
            Random random = new();
            int length = _balls.Length;
            if (count < length)
            {
                for (int i = count; i < length; ++i)
                {
                    _sceneManager.RemoveGameObject(_balls[i]);
                }
            }
            
            Array.Resize(ref _balls, count);
            if (count > length)
            {
                for (int i = length; i < count; ++i)
                {
                    _balls[i] = new Ball();
                    _balls[i].EngineBall.Random(random);
                    _sceneManager.AddGameObject(_balls[i]);
                }
            }
        };
    }

    protected override void InitializeScene()
    {
        base.InitializeScene();
        /* add cloth to the scene */
        _cloth = new Cloth(_forceRegistry, _boxesDemoSettingsWindow.SizeX, _boxesDemoSettingsWindow.SizeY,
            _boxesDemoSettingsWindow.SpringLength, _boxesDemoSettingsWindow.SpringConstant,
            _boxesDemoSettingsWindow.ParticleMass);
        _sceneManager.AddGameObject(this._cloth);
        
        /* add ground plane to the scene */
        _plane = new();

        /* boxes already added by the boxes demo settings callback on loading of settings; or empty */
        /* the boxes can be added via ui */
        // _sceneManager.AddGameObject(_plane); // TODO: add not rendered objects; or rendered with wireframe only
        
        /* set everything up */
        Reset();
    }

    protected override void RenderWindows(double dt)
    {
        base.RenderWindows(dt);
        _collisionParametersWindow.Draw();
        _bvhNodesWindow.Draw();
#if DEBUG
        ContactsInspectorWindow.Draw(_collisionData.ContactList);
#endif
        _boxesDemoSettingsWindow.Draw();
    }

    protected override void DebugRenderInScene(Shader sh)
    {
        base.DebugRenderInScene(sh);
        Dictionary<int, IBoxable> boxDict = new();
        for (int i = 0; i < _boxes.Length + _balls.Length; i++)
        {
            boxDict[i] = i < _boxes.Length ? _boxes[i] : _balls[i - _boxes.Length];
        }

        for (int i = 0; i < _cloth.EngineCloth.Particles.GetLength(0); i++)
        {
            for (int j = 0; j < _cloth.EngineCloth.Particles.GetLength(1); j++)
            {
                var particle = _cloth.EngineCloth.Particles[i, j];
                if (particle != null && particle.Body != null)
                {
                    boxDict[_boxes.Length + _balls.Length + i * _cloth.EngineCloth.SizeY + j] = particle;
                }
            }
        }

        BVH bvh = BVH.Build(boxDict);
        _bvhNodesWindow.DebugRenderInScene(sh, bvh);
    }

    /// <summary>
    /// Processes the contact generation code.
    /// </summary>
    protected override void GenerateContacts()
    {
        // Set up the collision data structure
        _collisionData.Reset(MaxContacts);

        Dictionary<int, IBoxable> boxDict = new();
        for (int i = 0; i < _boxes.Length + _balls.Length; i++)
        {
            boxDict[i] = i < _boxes.Length ? _boxes[i] : _balls[i - _boxes.Length];
        }

        for (int i = 0; i < _cloth.EngineCloth.Particles.GetLength(0); i++)
        {
            for (int j = 0; j < _cloth.EngineCloth.Particles.GetLength(1); j++)
            {
                var particle = _cloth.EngineCloth.Particles[i, j];
                if (particle != null && particle.Body != null)
                {
                    boxDict[_boxes.Length + _balls.Length + i * _cloth.EngineCloth.SizeY + j] = particle;
                }
            }
        }

        BVH bvh = BVH.Build(boxDict);

        // Process box-plane collisions
        foreach (var box in _boxes)
        {
            if (!_collisionData.HasMoreContacts()) return;
            CollisionDetector.BoxAndHalfSpace(box.EngineBox, _plane.EnginePlane, _collisionData);
        }

        // Process sphere-plane collisions
        foreach (var ball in _balls)
        {
            if (!_collisionData.HasMoreContacts()) return;
            CollisionDetector.SphereAndHalfSpace(ball.EngineBall, _plane.EnginePlane, _collisionData);
        }

        // Process particle-plane collisions
        foreach (var particle in _cloth.EngineCloth.Particles)
        {
            if (particle == null || particle.Body == null) continue;
            if (!_collisionData.HasMoreContacts()) return;
            CollisionDetector.ParticleAndHalfSpace(particle, _plane.EnginePlane, _collisionData);
        }

        List<(int, int)> potentialCollisions = new();
        BVH.GetPotentialContacts(ref potentialCollisions, bvh.root);
        //
        foreach (var pair in potentialCollisions)
        {
            if (!_collisionData.HasMoreContacts()) return;

            // TODO: refactor this check
            if (pair.Item1 < _boxes.Length) // box and ...
            {
                if (pair.Item2 < _boxes.Length) // box and box
                {
                    var box1 = _boxes[pair.Item1];
                    var box2 = _boxes[pair.Item2];

                    var contacts = CollisionDetector.BoxAndBox(box1.EngineBox, box2.EngineBox, _collisionData);
                    if (contacts > 0)
                    {
                        box1.EngineBox.IsOverlapping = box2.EngineBox.IsOverlapping = true;
                    }
                }
                else if (pair.Item2 >= _boxes.Length && pair.Item2 < _boxes.Length + _balls.Length) // box and sphere
                {
                    var box1 = _boxes[pair.Item1];
                    var ball2 = _balls[pair.Item2 - _boxes.Length];

                    var contacts = CollisionDetector.BoxAndSphere(box1.EngineBox, ball2.EngineBall, _collisionData);
                    if (contacts)
                    {
                        box1.EngineBox.IsOverlapping = ball2.EngineBall.IsOverlapping = true;
                    }
                }
                else // box and particle
                {
                    var box1 = _boxes[pair.Item1];
                    var particle2 = _cloth.EngineCloth.Particles[
                        (pair.Item2 - _boxes.Length - _balls.Length) / _cloth.EngineCloth.SizeY,
                        (pair.Item2 - _boxes.Length - _balls.Length) % _cloth.EngineCloth.SizeY];
                    if (particle2 == null || particle2.Body == null) continue;

                    var contacts = CollisionDetector.BoxAndParticle(box1.EngineBox, particle2, _collisionData);
                    if (contacts > 0)
                    {
                        box1.EngineBox.IsOverlapping = true;
                    }
                }
            }
            else if (pair.Item1 >= _boxes.Length && pair.Item1 < _boxes.Length + _balls.Length) // sphere and ...
            {
                if (pair.Item2 < _boxes.Length) // sphere and box
                {
                    var ball1 = _balls[pair.Item1 - _boxes.Length];
                    var box2 = _boxes[pair.Item2];

                    var contacts = CollisionDetector.BoxAndSphere(box2.EngineBox, ball1.EngineBall, _collisionData);
                    if (contacts)
                    {
                        box2.EngineBox.IsOverlapping = ball1.EngineBall.IsOverlapping = true;
                    }
                }
                else if (pair.Item2 >= _boxes.Length && pair.Item2 < _boxes.Length + _balls.Length) // sphere and sphere
                {
                    var ball1 = _balls[pair.Item1 - _boxes.Length];
                    var ball2 = _balls[pair.Item2 - _boxes.Length];

                    var contacts =
                        CollisionDetector.SphereAndSphere(ball1.EngineBall, ball2.EngineBall, _collisionData);
                    if (contacts)
                    {
                        ball1.EngineBall.IsOverlapping = ball2.EngineBall.IsOverlapping = true;
                    }
                }
            }
            else // particle and ...
            {
                //var particle1 = _cloth.EngineCloth.particles[
                //    (pair.Item1 - _boxes.Length - _balls.Length) / _cloth.EngineCloth.sizeY,
                //    (pair.Item1 - _boxes.Length - _balls.Length) % _cloth.EngineCloth.sizeY];
                //if (particle1 == null || particle1.Body == null) continue;
            }
        }

        // cloth contacts
        //var engCloth = _cloth.EngineCloth;
        //for (int x = 0; x < engCloth.sizeX; x++)
        //{
        //    for (int y = 0; y < engCloth.sizeY; y++)
        //    {
        //        if (!_collisionData.HasMoreContacts()) return;

        //        var rigidParticle = engCloth.particles[x, y];
        //        if (rigidParticle == null || rigidParticle.Body == null) continue;

        //        var collParticle = new CollisionParticle();
        //        collParticle.Body = rigidParticle.Body;
        //        collParticle.CalculateInternals();


        //        if (!_collisionData.HasMoreContacts()) return;
        //        CollisionDetector.ParticleAndHalfSpace(collParticle, _plane.EnginePlane, _collisionData);


        //        for (var b = 0; b < _boxes.Length; b++)
        //        {
        //            if (!_collisionData.HasMoreContacts()) return;
        //            CollisionDetector.BoxAndParticle(_boxes[b].EngineBox, collParticle, _collisionData);
        //        }
        //    }
        //}
    }

    /// <summary>
    /// Processes the objects in the simulation forward in time.
    /// </summary>
    protected override void UpdateObjects(float duration)
    {
        _forceRegistry.updateForces(duration);
        _cloth.EngineCloth.Update(duration);

        // Update the physics of each box in turn
        foreach (var box in _boxes)
        {
            box.EngineBox.Body.Integrate(duration);
            box.EngineBox.CalculateInternals();
            box.EngineBox.IsOverlapping = false;
        }

        foreach (var ball in _balls)
        {
            ball.EngineBall.Body.Integrate(duration);
            ball.EngineBall.CalculateInternals();
            ball.EngineBall.IsOverlapping = false;
        }
    }

    /// <summary>
    /// Resets the position of all the boxes and primes the explosion.
    /// </summary>
    protected override void Reset()
    {
        _forceRegistry.Clear();
        _cloth.EngineCloth = new Engine.Cloth(_forceRegistry, _boxesDemoSettingsWindow.SizeX,
            _boxesDemoSettingsWindow.SizeY,
            _boxesDemoSettingsWindow.SpringLength, _boxesDemoSettingsWindow.SpringConstant,
            _boxesDemoSettingsWindow.ParticleMass);

        // reset boxes; some in preconfigured positions
        if (_boxes.Length > 0)
        {
            _boxes[0].EngineBox.SetState(
                position: new Engine.Vector3(0, 3, 0),
                orientation: new Engine.Quaternion(),
                extents: new Engine.Vector3(4, 1, 1),
                velocity: new Engine.Vector3(0, 0, 0)
            );
        }

        if (_boxes.Length > 1)
        {
            _boxes[1].EngineBox.SetState(
                position: new Engine.Vector3(0, 4.75f, 2),
                orientation: new Engine.Quaternion(1.0f, 0.1f, 0.05f, 0.01f),
                extents: new Engine.Vector3(1, 1, 4),
                velocity: new Engine.Vector3(0, 0, 0)
            );
        }

        Random random = new();
        for (var i = 2; i < _boxes.Length; i++)
        {
            _boxes[i].EngineBox.Random(random);
        }

        // reset spheres; some in preconfigured positions
        if (_balls.Length > 0)
        {
            _balls[0].EngineBall.SetState(
                position: new Engine.Vector3(0, 10, 0),
                orientation: new Engine.Quaternion(),
                radius: 1.0f,
                velocity: new Engine.Vector3(0, 0, 0));
        }

        if (_balls.Length > 1)
        {
            _balls[1].EngineBall.SetState(
                position: new Engine.Vector3(3, 4.75f, 2),
                orientation: new Engine.Quaternion(),
                radius: 1.0f,
                velocity: new Engine.Vector3(0, 0, 0));
        }

        for (var i = 2; i < _balls.Length; i++)
        {
            _balls[i].EngineBall.Random(random);
        }

        // Reset the contacts
        _collisionData.ContactCount = 0;
    }

    protected override ApplicationState SaveState()
    {
        var state = base.SaveState();
        state.BvhNodes = _bvhNodesWindow.SaveState();
        state.CollisionParameters = _collisionParametersWindow.SaveState();
        state.ClothSettings = _boxesDemoSettingsWindow.SaveState();
        return state;
    }

    protected override void LoadState(ApplicationState state)
    {
        base.LoadState(state);
        if (state.BvhNodes is not null)
        {
            _bvhNodesWindow.RestoreState(state.BvhNodes);
        }

        if (state.CollisionParameters is not null)
        {
            _collisionParametersWindow.RestoreState(state.CollisionParameters);
        }

        if (state.ClothSettings is not null)
        {
            _boxesDemoSettingsWindow.RestoreState(state.ClothSettings);
        }
    }
}