using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Engine.RigidBodies;

namespace Engine.Collision.ContactGraph
{
    public class ContactGraphNode
    {
        public RigidBody Data { get; set; }

        public List<ContactGraphEdge> Edges { get; set; } = new List<ContactGraphEdge>();

        public ContactGraphNode(RigidBody data)
        {
            Data = data;
        }


    }
}
