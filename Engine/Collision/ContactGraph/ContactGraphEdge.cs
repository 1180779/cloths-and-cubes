using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Collision.ContactGraph
{
    public class ContactGraphEdge
    {
        public ContactGraphNode NodeA { get; set; }
        public ContactGraphNode? NodeB { get; set; }

        public Contact Data { get; set; }
        
        public bool IsSceneContact =>
            (NodeA == null || NodeB == null);

        public ContactGraphEdge(ContactGraphNode nodeA, ContactGraphNode? nodeB, Contact data)
        {
            NodeA = nodeA;
            NodeB = nodeB;
            Data = data;
        }

        public List<ContactGraphEdge> GetAdjacentEdges()
        {
            List<ContactGraphEdge> adjacentEdges = [.. NodeA.Edges];
            if (NodeB != null)
            {
                adjacentEdges.AddRange(NodeB.Edges);
            }
            adjacentEdges.Remove(this);
            return adjacentEdges;
        }

        public List<ContactGraphEdge> GetAdjacentEdgesA()
        {
            List<ContactGraphEdge> adjacentEdges = [.. NodeA.Edges];
            adjacentEdges.Remove(this);
            return adjacentEdges;
        }

        public List<ContactGraphEdge> GetAdjacentEdgesB()
        {
            if (NodeB == null) return new List<ContactGraphEdge>();
            List<ContactGraphEdge> adjacentEdges = [.. NodeB.Edges];
            adjacentEdges.Remove(this);
            return adjacentEdges;
        }
    }
}
