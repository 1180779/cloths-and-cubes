using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Engine.RigidBodies;

namespace Engine.Collision.ContactGraph
{
    public class ContactGraphComponent
    {
        public ContactGraphComponent()
        {
        }

        // public (Vector3 min, Vector3 max) Bounds { get; set; } = (new Vector3(0, 0, 0), new Vector3(0, 0, 0));
        public HashSet<RigidBody> Bodies { get; set; } = new HashSet<RigidBody>();
        public List<ContactGraphNode> Nodes { get; set; } = new List<ContactGraphNode>();
        public List<ContactGraphEdge> Edges { get; set; } = new List<ContactGraphEdge>();

        

        public static ContactGraphComponent operator+(ContactGraphComponent left, ContactGraphComponent right)
        {
            ContactGraphComponent result = new ContactGraphComponent();
            result.Nodes.AddRange(left.Nodes);
            result.Nodes.AddRange(right.Nodes);
            result.Edges.AddRange(left.Edges);
            result.Edges.AddRange(right.Edges);
            return result;
        }

        // Handle static contacts (one body is null)
        public void AddStaticContact(Contact contact)
        {
            ContactGraphNode node;
            if (contact.Body[0] != null)
            {

                if (!Bodies.Contains(contact.Body[0]!))
                {
                    Bodies.Add(contact.Body[0]!);
                    node = new ContactGraphNode(contact.Body[0]!);
                    Nodes.Add(node);
                }
                else
                {
                    node = Nodes.First(n => n.Data == contact.Body[0]!);
                }
                var edge = new ContactGraphEdge(node, null, contact);
                Edges.Add(edge);
                node.Edges.Add(edge);
                return;
            }
            if (contact.Body[1] != null)
            {
                if (!Bodies.Contains(contact.Body[1]!))
                {
                    Bodies.Add(contact.Body[1]!);
                    node = new ContactGraphNode(contact.Body[1]!);
                    Nodes.Add(node);
                }
                else
                {
                    node = Nodes.First(n => n.Data == contact.Body[1]!);
                }
                var edge = new ContactGraphEdge(node, null, contact);
                Edges.Add(edge);
                node.Edges.Add(edge);
                return;
            }
        }


        public void AddContact(Contact contact)
        {
            // Handle average case, both bodies are non-null
            ContactGraphNode nodeA, nodeB;
            if (!Bodies.Contains(contact.Body[0]!))
            {
                Bodies.Add(contact.Body[0]!);
                nodeA = new ContactGraphNode(contact.Body[0]!);
                Nodes.Add(nodeA);
            }
            else
            {
                nodeA = Nodes.First(n => n.Data == contact.Body[0]!);
            }
            if (!Bodies.Contains(contact.Body[1]!))
            {
                Bodies.Add(contact.Body[1]!);
                nodeB = new ContactGraphNode(contact.Body[1]!);
                Nodes.Add(nodeB);
            }
            else
            {
                nodeB = Nodes.First(n => n.Data == contact.Body[1]!);
            }

            var edgeAB = new ContactGraphEdge(nodeA, nodeB, contact);
            Edges.Add(edgeAB);
            nodeA.Edges.Add(edgeAB);
            nodeB.Edges.Add(edgeAB);

        }
        private const bool VERBAL = false;
        public void ResolvePositions(uint maxPositionIterations, float positionEpsilon)
        {
            PriorityQueue<ContactGraphEdge, Real> edgeQueue = new PriorityQueue<ContactGraphEdge, Real>();
            Vector3[] linearChange = [new(), new()];
            Vector3[] angularChange = [new(), new()];
            Vector3 deltaPosition = new();

            // iteratively resolve interpenetrations in order of severity.
            int PositionIterationsUsed = 0;
            foreach (var edge in Edges)
            {
                // Since PriorityQueue returns minimal priority element, we use negative penetration
                edgeQueue.Enqueue(edge, -edge.Data.Penetration);
            }
            while (edgeQueue.Count > 0 && PositionIterationsUsed < maxPositionIterations)
            {
                var edge = edgeQueue.Dequeue();
                if(edge.Data.Penetration < positionEpsilon)
                {
                    break;
                }

                if(VERBAL)
                    Console.WriteLine($"ROUND {PositionIterationsUsed + 1}");

                var contact = edge.Data;
                contact.MatchAwakeState();

                contact.ApplyPositionChange(linearChange, angularChange, contact.Penetration);

                // Update adjacent edges

                // Get edges adjacent to contact.Body[0]
                var adjacentEdgesA = edge.GetAdjacentEdgesA();
                // Get edges adjacent to contact.Body[1] (an empty list if the aforementioned object is null)
                var adjacentEdgesB = edge.GetAdjacentEdgesB();

                // Set d to 0, because edge.NodeA = contact.Body[0]
                int d = 0;
                foreach (var adjacentEdge in adjacentEdgesA)
                {
                    // b is the index of the matching body
                    int b = (adjacentEdge.NodeA == edge.NodeA) ? 0 : 1;
                    deltaPosition = linearChange[d] +
                                    angularChange[d].VectorProduct(
                                        adjacentEdge.Data.RelativeContactPosition[b]);
                    var oldPen = adjacentEdge.Data.Penetration;
                    adjacentEdge.Data.Penetration +=
                                    deltaPosition.ScalarProduct(adjacentEdge.Data.ContactNormal)
                                    * (b != 0 ? 1 : -1);

                    if(VERBAL)
                        Console.WriteLine($"\t[{adjacentEdge.Data.ContactPoint.X:0.##}, {adjacentEdge.Data.ContactPoint.Z:0.##}]: ({oldPen:0.####}) -> ({adjacentEdge.Data.Penetration:0.####})");
                    //Console.WriteLine($"\t\tDelta Pos: {deltaPosition}");
                    //TODO: CHECK IF THERE IS A BETTER WAY TO UPDATE THE QUEUE
                    ContactGraphEdge actuallyRemoved;
                    float removedPriority;
                    var removed = edgeQueue.Remove(adjacentEdge, out actuallyRemoved, out removedPriority);
                    if(removed)
                        edgeQueue.Enqueue(adjacentEdge, -adjacentEdge.Data.Penetration);

                }

                // Set d to 1, edge.NodeB = contact.Body[1]
                d = 1;
                foreach (var adjacentEdge in adjacentEdgesB)
                {
                    int b = (adjacentEdge.NodeA == edge.NodeB) ? 0 : 1;
                    deltaPosition = linearChange[d] +
                                    angularChange[d].VectorProduct(
                                        adjacentEdge.Data.RelativeContactPosition[b]);
                    var oldPen = adjacentEdge.Data.Penetration;
                    adjacentEdge.Data.Penetration +=
                                    deltaPosition.ScalarProduct(adjacentEdge.Data.ContactNormal)
                                    * (b != 0 ? 1 : -1);

                    if(VERBAL)
                        Console.WriteLine($"\t[{adjacentEdge.Data.ContactPoint.X:0.##}, {adjacentEdge.Data.ContactPoint.Z:0.##}]: ({oldPen:0.####}) -> ({adjacentEdge.Data.Penetration:0.####})");
                    //Console.WriteLine($"\t\tDelta Pos: {deltaPosition}");
                    ContactGraphEdge actuallyRemoved;
                    float removedPriority;
                    var removed = edgeQueue.Remove(adjacentEdge, out actuallyRemoved, out removedPriority);
                    if(removed)
                        edgeQueue.Enqueue(adjacentEdge, -adjacentEdge.Data.Penetration);
                }
                PositionIterationsUsed++;
                //Edges.Remove(edge);
                //edge.NodeA?.Edges.Remove(edge);
                //edge.NodeB?.Edges.Remove(edge);
            }

            if(VERBAL)
                Environment.Exit(0);
        }

        public void ResolveVelocities(uint maxVelocityIterations, Real velocityEpsilon, Real duration)
        {
            PriorityQueue<ContactGraphEdge, Real> edgeQueue = new PriorityQueue<ContactGraphEdge, Real>();
            Vector3[] velocityChange = [new(), new()];
            Vector3[] rotationChange = [new(), new()];
            Vector3 deltaPosition = new();

            // iteratively resolve interpenetrations in order of severity.
            int VelocityIterationsUsed = 0;
            foreach (var edge in Edges)
            {
                // Since PriorityQueue returns minimal priority element, we use negative penetration
                edgeQueue.Enqueue(edge, -edge.Data.DesiredDeltaVelocity);
            }
            while (edgeQueue.Count > 0 && VelocityIterationsUsed < maxVelocityIterations)
            {
                var edge = edgeQueue.Dequeue();
                if (edge.Data.DesiredDeltaVelocity < velocityEpsilon)
                {
                    break;
                }

                var contact = edge.Data;
                contact.MatchAwakeState();

                contact.ApplyVelocityChange(velocityChange, rotationChange);

                // Update adjacent edges

                // Get edges adjacent to contact.Body[0]
                var adjacentEdgesA = edge.GetAdjacentEdgesA();
                // Get edges adjacent to contact.Body[1] (an empty list if the aforementioned object is null)
                var adjacentEdgesB = edge.GetAdjacentEdgesB();

                // Set d to 0, because edge.NodeA = contact.Body[0]
                int d = 0;
                foreach (var adjacentEdge in adjacentEdgesA)
                {
                    // b is the index of the matching body
                    int b = (adjacentEdge.NodeA == edge.NodeA) ? 0 : 1;

                    Vector3 deltaVel = velocityChange[d] +
                                    rotationChange[d].VectorProduct(
                                        adjacentEdge.Data.RelativeContactPosition[b]);

                    // The sign of the change is negative if we're dealing
                    // with the second body in a contact.
                    adjacentEdge.Data.ContactVelocity +=
                        adjacentEdge.Data.ContactToWorld.TransformTranspose(deltaVel)
                        * (b != 0 ? -1 : 1);
                    adjacentEdge.Data.CalculateDesiredDeltaVelocity(duration);
                    //TODO: CHECK IF THERE IS A BETTER WAY TO UPDATE THE QUEUE
                    ContactGraphEdge actuallyRemoved;
                    float removedPriority;
                    var _ = edgeQueue.Remove(adjacentEdge, out actuallyRemoved, out removedPriority);
                    edgeQueue.Enqueue(adjacentEdge, -adjacentEdge.Data.DesiredDeltaVelocity);

                }

                // Set d to 1, edge.NodeB = contact.Body[1]
                d = 1;
                foreach (var adjacentEdge in adjacentEdgesB)
                {
                    int b = (adjacentEdge.NodeA == edge.NodeB) ? 0 : 1;
                    Vector3 deltaVel = velocityChange[d] +
                                     rotationChange[d].VectorProduct(
                                         adjacentEdge.Data.RelativeContactPosition[b]);

                    // The sign of the change is negative if we're dealing
                    // with the second body in a contact.
                    adjacentEdge.Data.ContactVelocity +=
                        adjacentEdge.Data.ContactToWorld.TransformTranspose(deltaVel)
                        * (b != 0 ? -1 : 1);
                    adjacentEdge.Data.CalculateDesiredDeltaVelocity(duration);
                    ContactGraphEdge actuallyRemoved;
                    float removedPriority;
                    var _ = edgeQueue.Remove(adjacentEdge, out actuallyRemoved, out removedPriority);
                    edgeQueue.Enqueue(adjacentEdge, -adjacentEdge.Data.DesiredDeltaVelocity);
                }
                VelocityIterationsUsed++;
                //Edges.Remove(edge);
                //edge.NodeA?.Edges.Remove(edge);
                //edge.NodeB?.Edges.Remove(edge);
            }
        }

    }


}
