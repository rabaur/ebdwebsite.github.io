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
public abstract class Script_Instance_12013 : GH_ScriptInstance
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
  private void RunScript(List<Brep> GraphBreps, List<int> GraphTypes, List<Point3d> GraphLocations, List<Point3d> GraphFirstDelimitingPoints, List<Point3d> GraphSecondDelimitingPoints, object AdjacencyMatrix, List<Point3d> BranchPointList, ref object MergedBreps, ref object Curves, ref object Joinable, ref object Extremal)
  {
    Dictionary<Node, List<Node>> graph = ReassembleGraph(GraphBreps, GraphTypes, GraphLocations, GraphFirstDelimitingPoints, GraphSecondDelimitingPoints, (Matrix)AdjacencyMatrix);

    // Join all bipartite subgraphs which only consist of type 0 and type 2 nodes and the type 2 nodes are not concurrent, i.e. have at most 2 type 2 neighbors (ignoring type 1 neighbors).
    Dictionary<Node, bool> visited = new Dictionary<Node, bool>(); // Indicated whether a node was already visited.
    List<Brep> mergedBreps = new List<Brep>();
    List<Curve> curves = new List<Curve>();
    List<Brep> joinable = new List<Brep>();
    List<Point3d> extremal = new List<Point3d>();
    foreach (KeyValuePair<Node, List<Node>> keyVal in graph)
    {
      Node currNode = keyVal.Key;
      if (currNode.type != 2)
      {
        continue;
      }
      if (!ContainsPointParallel(currNode.delimitingPoints[0], BranchPointList, 1.0))
      {
        continue;
      }
      List<Node> neighbors = keyVal.Value;
      List<Brep> toBeMerged = new List<Brep>() { currNode.brep };
      if (currNode.brep == null)
      {
        continue;
      }
      foreach (Node neighbor in neighbors)
      {
        if (neighbor.type != 1)
        {
          continue;
        }
        if (neighbor.delimitingPoints[0].DistanceTo(currNode.delimitingPoints[0]) > 2.0 && neighbor.delimitingPoints[1].DistanceTo(currNode.delimitingPoints[0]) > 2.0)
        {
          continue;
        }
        toBeMerged.Add(neighbor.brep);
      }
      if (toBeMerged.Count == 1)
      {
        continue;
      }
      if (joinable.Count == 0)
      {
        joinable.AddRange(toBeMerged);
        extremal.Add(currNode.delimitingPoints[0]);
      }
      Point3d center = ComputePolyCenter(toBeMerged);
      List<Point3d> corners = new List<Point3d>();
      foreach (Brep brep in toBeMerged)
      {
        foreach (BrepVertex bVert in brep.Vertices)
        {
          corners.Add(bVert.Location);
        }
      }
      Point3d[] sortedCorners = SortPointsWithAngle(center, corners);
      List<LineCurve> edges = new List<LineCurve>();
      for (int i = 0; i < sortedCorners.Length; i++)
      {
        edges.Add(new LineCurve(sortedCorners[i], sortedCorners[(i + 1) % sortedCorners.Length]));
      }
      curves.AddRange(edges);
      Brep[] res = Brep.CreatePlanarBreps(edges, RhinoMath.SqrtEpsilon);
      if (res == null)
      {
        continue;
      }
      mergedBreps.AddRange(Brep.CreatePlanarBreps(edges, RhinoMath.SqrtEpsilon));
    }
    MergedBreps = mergedBreps;
    Curves = curves;
    Joinable = joinable;
    Extremal = extremal;
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
    for (int i = 0; i < breps.Count; i++)
    {
      Node currNode = new Node(breps[i], types[i], locations[i], new List<Point3d>() { firstDelimitingPoint[i], secondDelimitingPoint[i] });
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
      Print(AngleToXY(connector).ToString());
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
  #endregion
}