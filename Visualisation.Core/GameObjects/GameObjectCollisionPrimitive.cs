using Engine.Collision;

using Visualisation.Core.Display;

namespace Visualisation.Core.GameObjects;

public abstract class GameObjectCollisionPrimitive : GameObject
{
    public abstract CollisionPrimitive EngineCollisionPrimitive { get; }

    private IRenderStrategy? _renderStrategy;

    public override IRenderStrategy RenderStrategy
    {
        get
        {
            _renderStrategy ??= new StaticMeshRenderStrategy(Mesh, Material);
            return _renderStrategy;
        }
    }

    protected override void OnMaterialChanged()
    {
        _renderStrategy = null;
    }
}