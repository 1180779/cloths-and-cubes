using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

using Engine.RigidBodies;

namespace Engine.Collision.ContactGraph
{
    public class ContactGraph
    {
        public Dictionary<int, ContactGraphComponent> Components = [];
        private Dictionary<RigidBody, int> _bodyToIdx = [];
        private UnionFind _unionFind = new();
        private const bool PARALLEL_PROCESSING = true;
        public ContactGraph()
        {
        }

        public void AddContact(Contact contact)
        {
            bool bodyAnull = contact.Body[0] == null;
            bool bodyBnull = contact.Body[1] == null;
            bool isStaticContact = bodyAnull || bodyBnull;

            if (bodyAnull && bodyBnull)
            {
                return;
            }


            // Get or create body IDs
            int idA = GetOrCreateBodyId(contact.Body[0]);
            int idB = GetOrCreateBodyId(contact.Body[1]);

            if (isStaticContact)
            {
                // Static contact - add to existing component or create new
                int rootId = bodyAnull ? _unionFind.Find(idB) : _unionFind.Find(idA);
                GetOrCreateComponent(rootId).AddStaticContact(contact);
                return; 
            }

            // Find current roots
            int rootA = _unionFind.Find(idA);
            int rootB = _unionFind.Find(idB);

            if (rootA == rootB)
            {
                // Same component - add contact to existing component
                GetOrCreateComponent(rootA).AddContact(contact);
            }
            else
            {
                // Get or create components for both roots
                var compA = GetOrCreateComponent(rootA);
                var compB = GetOrCreateComponent(rootB);

                int newRoot;
                // Merge the smaller into the larger
                if (compA.Bodies.Count >= compB.Bodies.Count)
                {
                    _unionFind.Union(rootA, rootB);
                    newRoot = _unionFind.Find(idA);
                    compA += compB;
                    compA.AddContact(contact);
                    Components[newRoot] = compA;
                    Components.Remove(rootB);
                }
                else
                {
                    _unionFind.Union(rootB, rootA);
                    newRoot = _unionFind.Find(idA);
                    compB += compA;
                    compB.AddContact(contact);
                    Components[newRoot] = compB;
                    Components.Remove(rootA);
                }
            }

        }

        private int GetOrCreateBodyId(RigidBody body)
        {
            if (body == null) return -1;

            if (!_bodyToIdx.TryGetValue(body, out int id))
            {
                id = _bodyToIdx.Count;
                _bodyToIdx[body] = id;
                _unionFind.MakeSet(id);
            }
            return id;
        }

        private ContactGraphComponent GetOrCreateComponent(int rootId)
        {
            if (!Components.TryGetValue(rootId, out var component))
            {
                component = new ContactGraphComponent();
                Components[rootId] = component;
            }
            return component;
        }

        public static ContactGraph Build(Contact[] contacts, uint numContacts)
        {
            ContactGraph graph = new ContactGraph();
            for(int i = 0; i<numContacts; i++)
            {
                graph.AddContact(contacts[i]);
            }
            return graph;
        }

        public void ResolvePositions(uint maxPositionIterations, float positionEpsilon)
        {
            if(!PARALLEL_PROCESSING)
            {
                foreach (var comp in Components)
                {
                    comp.Value.ResolvePositions(maxPositionIterations, positionEpsilon);
                }
                return;
            }
            Parallel.ForEach(Components, comp => comp.Value.ResolvePositions(maxPositionIterations, positionEpsilon));
        }

        public void ResolveVelocities(uint maxVelocityIterations, float velocityEpsilon, Real duration)
        {
            if (!PARALLEL_PROCESSING)
            {
                foreach (var comp in Components)
                {
                    comp.Value.ResolveVelocities(maxVelocityIterations, velocityEpsilon, duration);
                }
                return;
            }
            Parallel.ForEach(Components, comp => comp.Value.ResolveVelocities(maxVelocityIterations, velocityEpsilon, duration));
        }

    }
}
