using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Visualisation.Core.Display.Mesh.VisualObjects;

namespace Visualisation.Core.GameObjects
{
    internal class Cloth
    {
        public Engine.Cloth EngineCloth { get; set; }
        public SpringMesh VisualCloth { get; set; }
        public void Dispose()
        {
            VisualCloth.Dispose();
        }

        public Guid Id => VisualCloth.Id;
        public AbstractVisualObject AbstractVisualObject => VisualCloth;
        public object PhysicsObject => EngineCloth;
    }
}
