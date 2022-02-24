using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;



/// <summary>
/// This class will be instantiated on demand by the Script component.
/// </summary>
public abstract class Script_Instance_966dc : GH_ScriptInstance
{
  #region Utility functions
  /// <summary>Print a String to the [Out] Parameter of the Script component.</summary>
  /// <param name="text">String to print.</param>
  private void Print(string text) { /* Implementation hidden. */ }
  /// <summary>Print a formatted String to the [Out] Parameter of the Script component.</summary>
  /// <param name="format">String format.</param>
  /// <param name="args">Formatting parameters.</param>
  private void Print(string format, params object[] args) { /* Implementation hidden. */ }
  /// <summary>Print useful information about an object instance to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj) { /* Implementation hidden. */ }
  /// <summary>Print the signatures of all the overloads of a specific method to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj, string method_name) { /* Implementation hidden. */ }
  #endregion

  #region Members
  /// <summary>Gets the current Rhino document.</summary>
  private readonly RhinoDoc RhinoDocument;
  /// <summary>Gets the Grasshopper document that owns this script.</summary>
  private readonly GH_Document GrasshopperDocument;
  /// <summary>Gets the Grasshopper script component that owns this script.</summary>
  private readonly IGH_Component Component;
  /// <summary>
  /// Gets the current iteration count. The first call to RunScript() is associated with Iteration==0.
  /// Any subsequent call within the same solution will increment the Iteration count.
  /// </summary>
  private readonly int Iteration;
  #endregion
  /// <summary>
  /// This procedure contains the user code. Input parameters are provided as regular arguments,
  /// Output parameters as ref arguments. You don't have to assign output parameters,
  /// they will have a default value.
  /// </summary>
  #region Runscript
  private void RunScript(List<Brep> GraphBreps, List<int> GraphTypes, List<Point3d> GraphLocations, List<Point3d> GraphFirstDelimitingPoints, List<Point3d> GraphSecondDelimitingPoints, object AdjacencyMatrix, ref object MergedBreps)
  {
    Dictionary<Node, List<Node>> graph = ReassembleGraph(GraphBreps, GraphTypes, GraphLocations, GraphFirstDelimitingPoints, GraphSecondDelimitingPoints, (Matrix)AdjacencyMatrix);

    // Join all bipartite subgraphs which only consist of type 0 and type 2 nodes and the type 2 nodes are not concurrent, i.e. have at most 2 type 2 neighbors (ignoring type 1 neighbors).
    Dictionary<Node, bool> visited = new Dictionary<Node, bool>(); // Indicated whether a node was already visited.
    List<Brep> mergedBreps = new List<Brep>();
    foreach (KeyValuePair<Node, List<Node>> keyVal in graph)
    {
      visited[keyVal.Key] = false;
    }
    foreach (KeyValuePair<Node, List<Node>> keyVal in graph)
    {
      Node node = keyVal.Key;
      if (visited[node])
      {
        continue;
      }
      if (node.type != 2)
      {
        continue;
      }

      // Node fulfills criteria: Has not been visited before (not merged before) and is a type 2 node.
      // Start BFS from this node to find connected component and count type 2 neighbors (ignoring type 0) for each type 2 node.
      Queue<Node> queue = new Queue<Node>();
      queue.Enqueue(node);
      int cnt = 0;
      List<Node> connectedComponent = new List<Node>();
      Dictionary<Node, int> node2Neighs = new Dictionary<Node, int>();
      while (queue.Count > 0)
      {
        cnt++;
        if (cnt > 10000)
        {
          throw new Exception("Probably stuck in a while loop");
        }
        Node currNode = queue.Dequeue();
        if (visited[currNode])
        {
          continue;
        }
        connectedComponent.Add(currNode);
        visited[currNode] = true;

        // Go through all neighbors.
        List<Node> neighbors = graph[currNode];
        foreach (Node neighbor in neighbors)
        {
          // Respect the bipartite rule: If previous node was type 2, next node must be type 0, and vice versa. Type 1 not considered in this step.
          if (currNode.type == 2 && neighbor.type != 0)
          {
            continue;
          } 
          else if (currNode.type == 0 && neighbor.type != 2)
          {
            continue;
          }
          queue.Enqueue(neighbor);
        }
      }
      if (connectedComponent.Count <= 1)
      {
        continue;
      }
      foreach (Node connected in connectedComponent)
      {
        mergedBreps.Add(connected.brep);
      }
    }
    MergedBreps = mergedBreps;
  }
  #endregion
  #region Additional
  public struct Node
  {
    public Brep brep;
    public int type;
    public Point3d location;
    public List<Point3d> delimitingPoints; // Only used for type 2 nodes.
    public Node(Brep brep, int type, Point3d location, List<Point3d> delimitingPoints)
    {
      this.brep = brep;
      this.type = type;
      this.location = location;
      this.delimitingPoints = delimitingPoints;
    }
  }

  private Dictionary<Node, List<Node>> ReassembleGraph(
    List<Brep> breps, 
    List<int> types, 
    List<Point3d> locations, 
    List<Point3d> firstDelimitingPoint, 
    List<Point3d> secondDelimitingPoint, 
    Matrix adjacencyMatrix)
  {

    // Create all nodes.
    List<Node> nodes = new List<Node>();
    Dictionary<Node, List<Node>> graph = new Dictionary<Node, List<Node>>();
    Print(breps.Count.ToString());
    for (int i = 0; i < breps.Count; i++)
    {
      Node currNode = new Node(breps[i], types[i], locations[i], new List<Point3d>() { firstDelimitingPoint[i], secondDelimitingPoint[i] });
      graph[currNode] = new List<Node>();
      nodes.Add(currNode);
    }

    // Add neighbors.
    Print(adjacencyMatrix.RowCount.ToString());
    for (int i = 0; i < breps.Count; i++)
    {
      for (int j = 0; j < breps.Count; j++)
      {
        if (adjacencyMatrix[i, j] == 1)
        {
          graph[nodes[i]].Add(nodes[j]);
        }
      }
    }
    return graph;
  }
  #endregion
}