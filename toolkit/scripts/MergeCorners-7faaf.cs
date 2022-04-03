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
public abstract class Script_Instance_7faaf : GH_ScriptInstance
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
  private void RunScript(List<Brep> InBreps, List<int> InTypes, List<Point3d> InLocations, DataTree<Point3d> InDelimitingPoints, object InAdjacencyMatrix, List<Point3d> BranchPointList, ref object NodeLocations, ref object Edges, ref object OutBreps, ref object OutTypes, ref object OutLocations, ref object OutDelimitingPoints, ref object OutAdjacencyMatrix)
  {
    // Reassemble input into graph.
    Dictionary<Node, List<Node>> graph = ReassembleGraph(InBreps, InTypes, InLocations, InDelimitingPoints, (Matrix)InAdjacencyMatrix);
    Dictionary<Node, List<Node>> graphCopy = new Dictionary<Node, List<Node>>(graph);

    // Find type 2 nodes with type 0 neighbors which have only 1 neighbor (the type 2 node). Contract them.
    foreach (KeyValuePair<Node, List<Node>> keyVal in graph)
    {
      Node currNode = keyVal.Key;
      if (currNode.type != 2)
      {
        continue;
      }
      if (currNode.brep == null)
      {
        continue;
      }
      List<Node> neighbors = keyVal.Value;
      List<Node> toContract = new List<Node>() { currNode };
      foreach (Node neighbor in neighbors)
      {
        if (neighbor.type == 0)
        {
          for (int i = 0; i < graph[neighbor].Count; i++)
          {
            for (int j = i + 1; j < graph[neighbor].Count; j++)
            {
              if (ComputePolyCenter(new List<Brep>() { graph[neighbor][i].brep }).DistanceTo(ComputePolyCenter(new List<Brep>() { graph[neighbor][j].brep })) < 0.1)
              {
                throw new Exception("Great, we have node aliasing");
              }
            }
          }
        }
        if (neighbor.type == 0 && graph[neighbor].Count == 1)
        {
          toContract.Add(neighbor);
        }
      }
      if (toContract.Count <= 1)
      {
        continue;
      }

      ContractNodes(graphCopy, toContract, 2);
    }

    // Deconstruct graph.
    List<Brep> outBreps = new List<Brep>();
    List<int> outTypes = new List<int>();
    List<Point3d> outLocations = new List<Point3d>();
    DataTree<Point3d> outDelimitingPoints = new DataTree<Point3d>();
    Matrix adjacencyMatrix = new Matrix(graphCopy.Count, graphCopy.Count);
    DeconstructGraph(graphCopy, ref outBreps, ref outTypes, ref outLocations, ref outDelimitingPoints, ref adjacencyMatrix);
    OutBreps = outBreps;
    OutTypes = outTypes;
    OutLocations = outLocations;
    OutDelimitingPoints = outDelimitingPoints;
    OutAdjacencyMatrix = adjacencyMatrix;
    Tuple<List<Point3d>, List<LineCurve>> nodeLocsAndEdges = GetPointsAndEdges(graphCopy);
    NodeLocations = nodeLocsAndEdges.Item1;
    Edges = nodeLocsAndEdges.Item2;

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
    DataTree<Point3d> delimitingPoints, 
    Matrix adjacencyMatrix)
  {

    // Create all nodes.
    List<Node> nodes = new List<Node>();
    Dictionary<Node, List<Node>> graph = new Dictionary<Node, List<Node>>();
    for (int i = 0; i < breps.Count; i++)
    {
      Node currNode = new Node(breps[i], types[i], locations[i], delimitingPoints.Branch(i));
      graph[currNode] = new List<Node>();
      nodes.Add(currNode);
    }

    // Add neighbors.
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

  private bool ContainsPointParallel(Point3d queryPoint, List<Point3d> pointList, double tol)
  {
    bool[] isClose = new bool[pointList.Count];
    System.Threading.Tasks.Parallel.For(0, pointList.Count, i =>
    {
      isClose[i] = queryPoint.DistanceTo(pointList[i]) < tol;
    });
    for (int i = 0; i < isClose.Length; i++)
    {
      if (isClose[i])
      {
        return true;
      }
    }
    return false;
  }

  private Point3d ComputePolyCenter(List<Brep> breps)
  {
    List<Point3d> corners = new List<Point3d>();
    foreach (Brep brep in breps)
    {
      if (brep == null)
      {
        return Point3d.Origin;
      }
      foreach (BrepVertex brepVertex in brep.Vertices)
      {
        Point3d location = brepVertex.Location;

        // Need to check if point is already close to corners.
        bool tooClose = false;
        foreach (Point3d corner in corners)
        {
          if (location.DistanceTo(corner) < 2.0)
          {
            tooClose = true;
            break;
          }
        }
        if (!tooClose)
        {
          corners.Add(location);
        }
      }
    }
    Point3d center = Point3d.Origin;
    foreach (Point3d corner in corners)
    {
      center += corner;
    }
    return center / corners.Count;
  }

  private Point3d[] SortPointsWithAngle(Point3d center, List<Point3d> points)
  {
    // Compute connecting vectors.
    List<Vector3d> connectors = new List<Vector3d>();
    foreach (Point3d point in points)
    {
      connectors.Add(point - center);
    }

    // Compute angles.
    List<double> angles = new List<double>();
    foreach (Vector3d connector in connectors)
    {
      angles.Add(AngleToXY(connector));
    }

    // Sort points according to angle.
    Point3d[] res = points.ToArray();
    Array.Sort(angles.ToArray(), res);
    return res;
  }

  private double AngleToXY(Vector3d vec)
  {
    Vector3d xAxis = Vector3d.XAxis;
    if (vec.Y >= 0)
    {
      return RhinoMath.ToDegrees(Vector3d.VectorAngle(vec, xAxis));
    }
    else
    {
      return 360.0 - RhinoMath.ToDegrees(Vector3d.VectorAngle(vec, xAxis));
    }
  }

  private void ContractNodes(Dictionary<Node, List<Node>> graph, List<Node> toContract, int newType)
  {
    // Find neighborhood of nodes to be contracted (without the nodes themselves).
    // TODO: Make more efficient by tracking the neighbors that were already added (instead of invoking Contains every time.)
    List<Node> neighbors = new List<Node>();
    foreach (Node contract in toContract)
    {
      foreach (Node neighbor in graph[contract])
      {
        if (!toContract.Contains(neighbor) && !neighbors.Contains(neighbor))
        {
          neighbors.Add(neighbor);
        }
      }
    }

    // Delete each node which was to be contracted.
    foreach (Node contract in toContract)
    {
      DeleteNode(graph, contract);
    }

    // Construct new node from nodes that were contracted.
    Brep newBrep = MergeBreps(toContract); // New brep.

    // Breaking the convention that there can only be two delimiting points now.
    List<Point3d> newDelimPoints = new List<Point3d>();
    foreach (Node contract in toContract)
    {
      foreach (Point3d delimPoint in contract.delimitingPoints)
      {
        bool tooClose = false;
        foreach (Point3d added in newDelimPoints)
        {
          if (added.DistanceTo(delimPoint) <= 1.0)
          {
            tooClose = true;
            break;
          }
        }
        if (!tooClose)
        {
          newDelimPoints.Add(delimPoint);
        }
      }
    }
    Node newNode = new Node(newBrep, newType, ComputePolyCenter(new List<Brep> { newBrep }), newDelimPoints);

    // Add node to the graph.
    graph[newNode] = neighbors;

    // Add node to all neighbors.
    foreach (Node neighbor in neighbors)
    {
      graph[neighbor].Add(newNode);
    }
  }

  // Returns old neighborhood if successful.
  private void DeleteNode(Dictionary<Node, List<Node>> graph, Node toDelete)
  {
    List<Node> neighbors;
    if (!graph.TryGetValue(toDelete, out neighbors))
    {
      throw new Exception("The node to be deleted was not present in the graph.");
    }

    // Delete the node.
    graph.Remove(toDelete);

    // Remove node from neighbor's adjacency list.
    foreach (Node neighbor in neighbors)
    {
      graph[neighbor].Remove(toDelete);
    }
  }

  private Brep MergeBreps(List<Node> nodes)
  {
    List<Brep> brepsToBeMerged = new List<Brep>();
    foreach (Node contract in nodes)
    {
      brepsToBeMerged.Add(contract.brep);
    }
    Point3d center = ComputePolyCenter(brepsToBeMerged);
    List<Point3d> corners = new List<Point3d>();
    foreach (Brep brep in brepsToBeMerged)
    {
      foreach (BrepVertex bVert in brep.Vertices)
      {
        bool tooClose = false;
        foreach (Point3d added in corners)
        {
          if (added.DistanceTo(bVert.Location) < 0.1)
          {
            tooClose = true;
            break;
          }
        }
        if (!tooClose)
        {
          corners.Add(bVert.Location);
        }
      }
    }
    Point3d[] sortedCorners = SortPointsWithAngle(center, corners);

    // Check if any of the corners lies on the straight line segment connecting the previous and next corner.
    // If so, this corner is redundant.
    bool[] redundant = new bool[sortedCorners.Length];

    // Starting from 1 to avoid needing to deal with negative remainders.
    for (int i = 1; i < sortedCorners.Length + 1; i++)
    {
      LineCurve connector = new LineCurve(sortedCorners[i - 1], sortedCorners[(i + 1) % sortedCorners.Length]);

      // Find closest point.
      double closestParam;
      connector.ClosestPoint(sortedCorners[i % sortedCorners.Length], out closestParam);
      Point3d closestPoint = connector.PointAt(closestParam);
      if (closestPoint.DistanceTo(sortedCorners[i % sortedCorners.Length]) < 0.1)
      {
        redundant[i % sortedCorners.Length] = true;
      }
    }

    List<Point3d> nonRedundantCorners = new List<Point3d>();
    for (int i = 0; i < sortedCorners.Length; i++)
    {
      if (!redundant[i])
      {
        nonRedundantCorners.Add(sortedCorners[i]);
      }
    }
    List<LineCurve> newBrepEdges = new List<LineCurve>();
    for (int i = 0; i < nonRedundantCorners.Count; i++)
    {
      newBrepEdges.Add(new LineCurve(nonRedundantCorners[i], nonRedundantCorners[(i + 1) % nonRedundantCorners.Count]));
    }
    Brep[] res = Brep.CreatePlanarBreps(newBrepEdges, RhinoMath.SqrtEpsilon);
    if (res == null)
    {
      string msg = "";
      for (int i = 0; i < redundant.Length; i++)
      {
        msg += redundant[i].ToString() + ", ";
      }
      throw new Exception("Merged surface was null");
    }
    if (res.Length != 1)
    {
      throw new Exception("Too few or too many breps resulted from merging: " + res.Length);
    }
    return res[0];
  }

  private Tuple<List<Point3d>, List<LineCurve>> GetPointsAndEdges(Dictionary<Node, List<Node>> graph)
  {
    List<Point3d> nodeLocs = new List<Point3d>();
    List<LineCurve> edges = new List<LineCurve>();
    foreach (KeyValuePair<Node, List<Node>> keyVal in graph)
    {
      Node node = keyVal.Key;
      List<Node> neighbors = keyVal.Value;
      nodeLocs.Add(node.location);
      foreach (Node neighbor in neighbors)
      {
        edges.Add(new LineCurve(node.location, neighbor.location));
      }
    }
    return new Tuple<List<Point3d>, List<LineCurve>>(nodeLocs, edges);
  }

  private void DeconstructGraph(
    Dictionary<Node, List<Node>> graph,
    ref List<Brep> breps,
    ref List<int> types,
    ref List<Point3d> locations,
    ref DataTree<Point3d> delimitingPoints,
    ref Matrix adjacencyMatrix)
  {
    List<Node> allNodes = new List<Node>();
    foreach (KeyValuePair<Node, List<Node>> keyValue in graph)
    {
      allNodes.Add(keyValue.Key);
    }
    int branchIdx = 0;
    foreach (KeyValuePair<Node, List<Node>> keyValue in graph)
    {
      Node currNode = keyValue.Key;
      List<Node> neighbors = keyValue.Value;

      // Transform into lists.
      breps.Add(currNode.brep);
      types.Add(currNode.type);
      locations.Add(currNode.location);
      delimitingPoints.AddRange(currNode.delimitingPoints, new GH_Path(new int[] { 0, branchIdx }));
      branchIdx++;

      int currIdx = allNodes.IndexOf(currNode);
      foreach (Node neighbor in neighbors)
      {
        int neighIdx = allNodes.IndexOf(neighbor);
        adjacencyMatrix[currIdx, neighIdx] = 1;
      }
    }
  }
  #endregion
}