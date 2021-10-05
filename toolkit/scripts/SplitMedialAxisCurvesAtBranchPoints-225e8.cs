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
public abstract class Script_Instance_225e8 : GH_ScriptInstance
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
  private void RunScript(List<Curve> medialAxisCurvesList, List<Point3d> branchpointsList, ref object splitMedialAxisCurvesList)
  {
    List<Curve> splitAtBranchPoints = new List<Curve>();

    // For each curve, find which branch-points are close.
    // Then split the curve at these branchpoints.
    foreach (Curve curve in medialAxisCurvesList)
    {
      // Find closest points on current for each branchpoint.
      double[] closestParameters = new double[branchpointsList.Count];
      for (int i = 0; i < branchpointsList.Count; i++)
      {
        double param = -1.0;
        curve.ClosestPoint(branchpointsList[i], out param);
        closestParameters[i] = param;
      }
      Point3d[] closestPoints = new Point3d[branchpointsList.Count];
      for (int i = 0; i < branchpointsList.Count; i++)
      {
        closestPoints[i] = curve.PointAt(closestParameters[i]);
      }

      // Only choose points that are sufficiently close (lie on the current curve).
      List<double> splitParameters = new List<double>();
      for (int i = 0; i < branchpointsList.Count; i++)
      {
        if (closestPoints[i].DistanceTo(branchpointsList[i]) <= 0.01)
        {
          splitParameters.Add(closestParameters[i]);
        }
      }

      // We just append the current curve if no branchpoint is close enough.
      if (splitParameters.Count == 0)
      {
        splitAtBranchPoints.Add(curve);
        continue;
      }

      // Split current curve at selected points.
      Curve[] splitCurves = curve.Split(splitParameters);

      // Add new segments to result.
      splitAtBranchPoints.AddRange(splitCurves);
    }
    splitMedialAxisCurvesList = splitAtBranchPoints;
  }
  #endregion
  #region Additional

  /// <summary>
  /// Constructs curves whose endpoints are branchpoints from potentially ill-formed curves.
  /// </summary>
  /// <remarks>A curve is ill-formed if it is adjacent to only 1 or 0 branchpoints.</remarks>
  /// <param name="inputCurves">All curves, including well formed ones, from the initial splitting step.</param>
  /// <returns></returns>
  private Curve[] JoinIllFormedCurves(Curve[] inputCurves, List<Point3d> branchPoints)
  {
    // Identify ill-formed curves.
    List<Curve> illFormedCurves = new List<Curve>();
    List<List<Point3d>> allAdjacentBranchPoints = new List<List<Point3d>>();
    foreach (Curve curve in inputCurves)
    {
      List<Point3d> adjacentBranchPoints = new List<Point3d>();
      Point3d curveStartPoint = curve.PointAtStart;
      Point3d curveEndPoint = curve.PointAtStart;
      foreach (Point3d branchPoint in branchPoints)
      {
        if (branchPoint.DistanceTo(curveStartPoint) < RhinoMath.SqrtEpsilon || branchPoint.DistanceTo(curveEndPoint) < RhinoMath.SqrtEpsilon)
        {
          adjacentBranchPoints.Add(branchPoint);
        }
        if (adjacentBranchPoints.Count == 2)
        {
          break;
        }
      }

      // If this curve is adjacent to two branchpoints, it is a well formed curve and we do not need to process it further.
      if (adjacentBranchPoints.Count == 2)
      {
        continue;
      }

      // Ill-formed segment.
      else if (adjacentBranchPoints.Count < 2)
      {
        illFormedCurves.Add(curve);
        allAdjacentBranchPoints.Add(adjacentBranchPoints);
      }

      // Should not happen.
      else
      {
        throw new Exception("Curve segment was adjacent to " + adjacentBranchPoints.Count.ToString() + " branch points. A medial axis curve should only be adjacent to 0, 1 or 2 branchpoints.");
      }
    }

    // Assemble curves.
    
  }

  /// <summary>
  /// Checks whether two curves are adjacent at a non-branch-point location.
  /// </summary>
  /// <param name="curve1">The first curve to be checked.</param>
  /// <param name="curve2">The second curve to be checked.</param>
  /// <param name="branchPoints">If the curves are adjacent at a location which is in this list, the adjacency is disregarded.</param>
  /// <returns>true if it is the case, false otherwise.</returns>
  private bool AreCurvesAdjacentAtNonBranchPoint(Curve curve1, Curve curve2, List<Point3d> branchPoints)
  {
    // Test for intersections.
    Rhino.Geometry.Intersect.CurveIntersections intersections = Rhino.Geometry.Intersect.Intersection.CurveCurve(curve1, curve2, RhinoMath.SqrtEpsilon, RhinoMath.SqrtEpsilon);

    // We only consider cases where we have exactly one point where the curves intersect.
    if (intersections.Count != 1)
    {
      return false;
    }

    Rhino.Geometry.Intersect.IntersectionEvent intersection = intersections[0];
    
    // Only consider point-intersections (overlapping curves cannot be joined easily).
    if (intersection.IsOverlap)
    {
      return false;
    }

    // Check if the point of overlap is a branchpoint, since curves should be joined in between branchpoints, but not at branchpoints.
    Point3d intersectionPoint = intersection.PointA;
    foreach (Point3d branchPoint in branchPoints)
    {
      if (intersectionPoint.DistanceTo(branchPoint) < RhinoMath.SqrtEpsilon)
      {
        return false;
      }
    }
    return true;
  }
  #endregion
}

/// <summary>
/// A node in the graph of ill-formed segments. A node corresponds to an ill-formed segment.
/// </summary>
public class IllFormedCurveNode : IEquatable<IllFormedCurveNode>
{
  private Curve _curve;

  public Curve curve
  {
    get
    {
      return _curve;
    }
  }

  private bool _adjacentToBranchPoint;

  public bool adjacentToBranchPoint
  {
    get
    {
      return _adjacentToBranchPoint;
    }
  }

  private Point3d _branchPoint;

  public Point3d branchPoint
  {
    get
    {
      return _branchPoint;
    }
  }

  /// <summary>
  /// Constructor if curve is not adjacent to branchpoint.
  /// </summary>
  /// <param name="curve">The curve this node corresponds to.</param>
  public IllFormedCurveNode(Curve curve)
  {
    _curve = curve;
    _adjacentToBranchPoint = false;
    _branchPoint = Point3d.Origin;
  }

  /// <summary>
  /// Constructor if curve is adjacent to branchpoint.
  /// </summary>
  /// <param name="curve">The curve this node corresponds to.</param>
  /// <param name="branchPoint">The branchpoint the curve this node represents is adjacent to.</param>
  public IllFormedCurveNode(Curve curve, Point3d branchPoint)
  {
    _curve = curve;
    _adjacentToBranchPoint = true;
    _branchPoint = branchPoint;
  }

  /// <summary>
  /// Tests for equality. IllFormedCurveNode are equal if the end points of their curves are equal (by some tolerance).
  /// </summary>
  /// <remarks>This comparison only works in this very restricted use-case.</remarks>
  /// <exception>Throws an exception if the curves are equal (by the definition given previously), but nodes are not adjacent to the same branchpoints.</exception>
  /// <param name="other">The other node to compare to.</param>
  /// <returns>true if nodes are </returns>
  public bool Equals(IllFormedCurveNode other)
  {
    // Compare distances between endpoints - if they are smaller than some tolerance, nodes are considered equal.
    Point3d thisStart = _curve.PointAtStart;
    Point3d thisEnd = _curve.PointAtEnd;
    Point3d otherStart = other.curve.PointAtStart;
    Point3d otherEnd = other.curve.PointAtEnd;

    bool equal = false;
    if (thisStart.DistanceTo(otherStart) < RhinoMath.SqrtEpsilon && thisEnd.DistanceTo(otherEnd) < RhinoMath.SqrtEpsilon ||
        thisStart.DistanceTo(otherEnd) < RhinoMath.SqrtEpsilon && thisEnd.DistanceTo(otherStart) < RhinoMath.SqrtEpsilon)
    {
      equal = true;
    }

    // If nodes are equal, they should be adjacent to the same branchpoints. 
    // TODO: This is not really the responsibility of this method, so future versions should probably offload the responsibility.
    if (equal && _branchPoint.DistanceTo(other.branchPoint) > RhinoMath.SqrtEpsilon)
    {
      throw new Exception("Nodes have same curves, but not same branchpoints. There is a bug in the node creation.");
    }

    return equal;
  }


  /// <summary>
  /// Tests for equality.
  /// </summary>
  /// <param name="otherObject">Object of any type.</param>
  /// <returns>true if objects are equal, false otherwise.</returns>
  public override bool Equals(object otherObject)
  {
    // Converting to IllFormedCurveNode.
    IllFormedCurveNode other = otherObject as IllFormedCurveNode;

    // Null-check covering faulty conversion.
    if (other == null)
    {
      return false;
    }

    // If conversion was successful (the other object is a IllFormedCurveNode), we can simply call the Equals method from this class.
    return this.Equals(other);
  }


  /// <summary>
  /// Override. Probably not a valid override, but will not be used here.
  /// </summary>
  /// <returns>Hashcode.</returns>
  public override int GetHashCode()
  {
    return base.GetHashCode();
  }
}

/// <summary>
/// Models the connectivity of ill-formed segments.
/// </summary>
public class IllFormedCurveGraph
{
  private List<IllFormedCurveNode> _nodes;

  public List<IllFormedCurveNode> nodes
  {
    get
    {
      return _nodes;
    }
  }

  private List<List<IllFormedCurveNode>> _adjacencyList;

  public List<List<IllFormedCurveNode>> adjacencyList
  {
    get
    {
      return _adjacencyList;
    }
  }

  public IllFormedCurveGraph()
  {
    _nodes = new List<IllFormedCurveNode>();
    _adjacencyList = new List<List<IllFormedCurveNode>>();
  }

  /// <summary>
  /// Adds a node to the graph.
  /// </summary>
  /// <exception>Exception will be thrown if (1) the curve is not ill-formed (determined by the number of adjacent branch points) or (2) the node was already added.</exception>
  /// <param name="curve">Curve this node corresponds to.</param>
  /// <param name="branchPoints">List of branchpoints this curve is adjacent to. Since IllFormedNodes only correspond to curves which have only 1 or 0 adjacent branchpoints, length of this list can at most be 1.</param>
  public void AddNode(Curve curve, List<Point3d> branchPoints)
  {

    // Create node.
    IllFormedCurveNode node;
    if (branchPoints.Count == 0)
    {
      node = new IllFormedCurveNode(curve);
    }
    else if (branchPoints.Count == 1)
    {
      node = new IllFormedCurveNode(curve, branchPoints[0]);
    }
    else
    {
      throw new Exception("Attempted to construct IllFormedCurveNode, but branchPoint.Count: " + branchPoints.Count.ToString() + ". This curve is either not ill-formed or there are too many branchpoints.");
    }

    // Check if node is unique.
    if (_nodes.Contains(node))
    {
      throw new Exception("Node was already added.");
    }

    // Add node.
    _nodes.Add(node);
    _adjacencyList.Add(new List<IllFormedCurveNode>());
  }

  /// <summary>
  /// Adds directed edge between the nodes corresponding to curve1 and curve2.
  /// </summary>
  /// <param name="curve1">The node corresponding </param>
  /// <param name="curve2"></param>
  public void AddEdge(Curve curve1, Curve curve2)

}