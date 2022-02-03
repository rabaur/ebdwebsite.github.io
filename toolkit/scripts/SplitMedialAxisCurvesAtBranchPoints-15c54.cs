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
public abstract class Script_Instance_15c54 : GH_ScriptInstance
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
  private void RunScript(List<Curve> medialAxisCurvesList, List<Point3d> branchpointsList, ref object splitMedialAxisCurvesList, ref object notAdjacent)
  {
    List<Curve> splitAtBranchPoints = new List<Curve>();

    // For each curve, find which branch-points are close.
    // Then split the curve at these branchpoints.
    foreach (Curve curve in medialAxisCurvesList)
    {
      // Find closest points on current curve for each branchpoint.
      double[] closestParameters = new double[branchpointsList.Count];
      for (int i = 0; i < branchpointsList.Count; i++)
      {
        double param;
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
        if (closestPoints[i].DistanceTo(branchpointsList[i]) <= RhinoMath.DefaultDistanceToleranceMillimeters)
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

    // Removing all segments that are not adjacent to branchpoints.
    List<Curve> final = new List<Curve>();
    foreach (Curve split in splitAtBranchPoints)
    {
      bool isAdjacentToBranchPoint = false;
      foreach (Point3d branchPoint in branchpointsList)
      {
        if (branchPoint.DistanceTo(split.PointAtStart) < RhinoMath.DefaultDistanceToleranceMillimeters || branchPoint.DistanceTo(split.PointAtEnd) < RhinoMath.DefaultDistanceToleranceMillimeters)
        {
          isAdjacentToBranchPoint = true;
          break;
        }
      }
      if (isAdjacentToBranchPoint)
      {
        final.Add(split);
      }
    }
    splitMedialAxisCurvesList = splitAtBranchPoints;
  }
  #endregion
  #region Additional

  /// <summary>
  /// Default tolerance when comparing distances.
  /// </summary>
  private const double DIST_TOL = RhinoMath.DefaultDistanceToleranceMillimeters;

  /// <summary>
  /// Constructs curves whose endpoints are branchpoints from potentially ill-formed curves.
  /// </summary>
  /// <remarks>A curve is ill-formed if it is adjacent to only 1 or 0 branchpoints.</remarks>
  /// <param name="inputCurves">All curves, including well formed ones, from the initial splitting step.</param>
  /// <returns></returns>
  private Curve[] JoinIllFormedCurves(List<Curve> inputCurves, List<Point3d> branchPoints)
  {
    // Identify ill-formed curves.
    List<Curve> illFormedCurves = new List<Curve>();
    List<List<Point3d>> allAdjacentBranchPoints = new List<List<Point3d>>();
    FindIllFormedCurves(inputCurves, branchPoints, ref illFormedCurves, ref allAdjacentBranchPoints);

    // Create graph from ill-formed curves.
    IllFormedCurveGraph graph = CreateIllFormedCurveGraph(illFormedCurves, allAdjacentBranchPoints, branchPoints);

    // TODO: Remove after testing.
    Print("Created graph of ill-formed segments with " + graph.GetNumNodes().ToString() + " nodes and " + graph.GetNumEdges().ToString() + " edges.");
    Print(graph.GetNumNodesAtBranchPoint() + " of these nodes are adjacent to a branch-point.");

    return new Curve[1];
  }

  /// <summary>
  /// For a set of curves created from splitting medial axis curves at branchpoints, find curves that are ill-formed. 
  /// A curve is ill-formed if it is not adjacent to exactly 2 branchpoints. 
  /// Such curves have to undergo further merging.
  /// </summary>
  /// <exception>Throws exception if there is a curve in inputCurves that is adjacent to more than 2 branchpoints.</exception>
  /// <param name="inputCurves">The curves resulting from splitting the medial axis curves at branchpoints.</param>
  /// <param name="branchPoints">The branchpoints at which the curves were split.</param>
  /// <param name="illFormedCurves">Array which will hold the ill-formed curves.</param>
  /// <param name="allAdjacentBranchPoints">Element allAdjacentBranchPoints[i] corresponds to illFormedCurves[i]. It holds the branch-points illFormedCurves[i] is adjacent to (either empty list or one element).</param>
  private void FindIllFormedCurves(List<Curve> inputCurves, List<Point3d> branchPoints, ref List<Curve> illFormedCurves, ref List<List<Point3d>> allAdjacentBranchPoints)
  {
    foreach (Curve curve in inputCurves)
    {
      List<Point3d> adjacentBranchPoints = new List<Point3d>();
      Point3d curveStartPoint = curve.PointAtStart;
      Point3d curveEndPoint = curve.PointAtEnd;
      foreach (Point3d branchPoint in branchPoints)
      {
        if (branchPoint.DistanceTo(curveStartPoint) <= DIST_TOL || branchPoint.DistanceTo(curveEndPoint) <= DIST_TOL)
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
  }

  /// <summary>
  /// Creates a graph based on the adjacency of the ill-formed curves.
  /// Checks whether two curves share an endpoints that does not belong to the set of branchpoints.
  /// Such an endpoint is exactly a location where the corresponding curves should be merged to form a larger (and possibly complete) medial axis curve,
  /// which is delimited by two branchpoints.
  /// </summary>
  /// <remarks>
  /// Note that it is sufficient to check whether the enpoints agree in this case, since the input curves are ill-formed,
  /// so one of the enpoints guaranteed to be a non-branchpoint (the other point is ignored with a proximity check).
  /// Thus it cannot happen that curves are represented as connected at positions that were correctly split previously.
  /// </remarks>
  /// <param name="illFormedCurves">The set of ill-formed curves.</param>
  /// <param name="allAdjacentBranchPoints">The branchpoints each ill-formed curve is adjacent to.</param>
  /// <param name="branchPoints">The set of branch points of the medial axis.</param>
  /// <returns>The graph capturing the connectivity of the ill-formed curves.</returns>
  private IllFormedCurveGraph CreateIllFormedCurveGraph(List<Curve> illFormedCurves, List<List<Point3d>> allAdjacentBranchPoints, List<Point3d> branchPoints)
  {
    IllFormedCurveGraph graph = new IllFormedCurveGraph();

    // Create all nodes.
    for (int i = 0; i < illFormedCurves.Count; i++)
    {
      try
      {
        graph.AddNode(illFormedCurves[i], allAdjacentBranchPoints[i]);
      }
      catch
      {
        Print("Duplicate curve was attempted to add.");
        continue;
      }
    }

    // Create edges between nodes. Since we want a undirected graph, we have to create both directions of edges.
    for (int i = 0; i < graph.GetNumNodes(); i++)
    {
      for (int j = 0; j < graph.GetNumNodes(); j++)
      {
        // No self-loops.
        if (i == j)
        {
          continue;
        }

        // Create edge.
        Curve curve1 = graph.nodes[i].curve;
        Curve curve2 = graph.nodes[j].curve;
        if (AreCurvesAdjacentAtNonBranchPoint(curve1, curve2, branchPoints))
        {
          graph.AddEdge(curve1, curve2);
        }
      }
    }
    return graph;
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

    // Filter all non-point intersections.
    List<Rhino.Geometry.Intersect.IntersectionEvent> pointIntersections = new List<Rhino.Geometry.Intersect.IntersectionEvent>();
    foreach (Rhino.Geometry.Intersect.IntersectionEvent intersection in intersections)
    {
      if (intersection.IsPoint)
      {
        pointIntersections.Add(intersection);
      }
    }

    // If we only have overlaps (no point-intersections), we return false.
    if (pointIntersections.Count == 0)
    {
      return false;
    }

    // For each point-intersection check if the intersecting point is a branch point.
    foreach (Point3d branchPoint in branchPoints)
    {
      foreach (Rhino.Geometry.Intersect.IntersectionEvent intersection in pointIntersections)
      {
        if (intersection.PointA.DistanceTo(branchPoint) < DIST_TOL)
        {
          return false;
        }
      }
    }
    return true;
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
        throw new Exception("Tried to add node " + node.ToString() + " but node was already added at index " + GetNodeIndex(node.curve) + " in the form " + _nodes[GetNodeIndex(node.curve)] + ".");
      }

      // Add node.
      _nodes.Add(node);
      _adjacencyList.Add(new List<IllFormedCurveNode>());
    }

    /// <summary>
    /// Adds directed edge from node corresponding to curve1 to node corresponding to curve2.
    /// </summary>
    /// <remarks>To model undirected graph, symmetric edge (from node corresponding to curve2 to node corresponding to node1) must be added as well.</remarks>
    /// <exception>Throws exception if (1) either of the nodes are not present in the graph or (2) the edge has already been added or (3) the nodes are equal (self-loop not allowed).</exception>
    /// <param name="curve1">The node corresponding </param>
    /// <param name="curve2"></param>
    public void AddEdge(Curve curve1, Curve curve2)
    {
      // Get indices of nodes.
      int index1 = GetNodeIndex(curve1);
      int index2 = GetNodeIndex(curve2);
      if (index1 == -1 || index2 == -1)
      {
        throw new Exception("Attempted to get nodes when creating edge, but nodes are not in graph.");
      }

      IllFormedCurveNode node1 = _nodes[index1];
      IllFormedCurveNode node2 = _nodes[index2];

      // Check nodes for equality.
      if (node1 == node2)
      {
        throw new Exception("Node corresponding to curve1 and curve2 are equivalent, but self-loops are not allowed.");
      }

      // Check if edge has already been added.
      if (_adjacencyList[index1].Contains(node2))
      {
        throw new Exception("Attempted to add edge, but node " + node2.ToString() + " was already added as neighbor at index " + _adjacencyList[index1].IndexOf(node2) + " in the form " + _adjacencyList[index1][_adjacencyList[index1].IndexOf(node2)] + ".");
      }

      // Add edge.
      _adjacencyList[index1].Add(node2);
    }


    /// <summary>
    /// Gets a node by its curve.
    /// </summary>
    /// <param name="curve">The curve that corresponds to the node.</param>
    /// <exception>Throws exception if node is not in graph.</exception>
    /// <returns>Node if exists.</returns>
    private IllFormedCurveNode GetNode(Curve curve)
    {
      int index = GetNodeIndex(curve);
      if (index == -1)
      {
        throw new Exception("Attempted to get node, but node is not in graph.");
      }
      return _nodes[index];
    }

    /// <summary>
    /// Gets neighbors of node identified by curve.
    /// </summary>
    /// <param name="curve">Curve that corresponds to node.</param>
    /// <exception>Throws exception if node is not in graph.</exception>
    /// <returns>Neighbors if node is found.</returns>
    private List<IllFormedCurveNode> GetNeighbors(Curve curve)
    {
      int index = GetNodeIndex(curve);
      if (index == -1)
      {
        throw new Exception("Attempted to get neighbors of node, but node is not in graph.");
      }
      return adjacencyList[index];
    }

    /// <summary>
    /// Gets the index of a node by curve.
    /// </summary>
    /// <param name="curve">Curve that corresponds to the node.</param>
    /// <returns>The index of the node in the _nodes list if present, otherwise -1.</returns>
    private int GetNodeIndex(Curve curve)
    {
      // Create mock node for comparison.
      IllFormedCurveNode mockNode = new IllFormedCurveNode(curve);

      // Get index.
      return nodes.IndexOf(mockNode);
    }


    /// <summary>
    /// Gets the index of a node.
    /// </summary>
    /// <param name="node"></param>
    /// <returns>The index of the node in the _nodes list if present, otherwise -1.</returns>
    private int GetNodeIndex(IllFormedCurveNode node)
    {
      return GetNodeIndex(node.curve);
    }


    /// <summary>
    /// Creates as many distinct, non-overlapping branchpoint-bounded medial segment from the current nodes as possible.
    /// </summary>
    /// <returns>Array of medial-axis curves from ill-formed curves represented by the currently added nodes and their edges.</returns>
    public Curve[] GetJoinedCurves()
    {
      // Get all nodes that are adjacent to one branchpoint, as these are the starting points for curves that can be joined.
      List<IllFormedCurveNode> adjToBranchPointNodes = new List<IllFormedCurveNode>();
      foreach (IllFormedCurveNode node in _nodes)
      {
        if (node.adjacentToBranchPoint)
        {
          adjToBranchPointNodes.Add(node);
        }
      }

      // Start new run of BFS for each node that is adjacent to a branch node.
      List<Curve> joinedCurves = new List<Curve>();
      foreach (IllFormedCurveNode startNode in adjToBranchPointNodes)
      {
        // List to keep track of the nodes already visited.
        List<IllFormedCurveNode> visited = new List<IllFormedCurveNode>();
        List<IllFormedCurveNode>[] predecessor = new List<IllFormedCurveNode>[_nodes.Count];
        List<IllFormedCurveNode> queue = new List<IllFormedCurveNode>();
        queue.Add(startNode);

        // Run BFS until no more nodes are enqueued.
        while (queue.Count > 0)
        {

          // Pop node from queue.
          IllFormedCurveNode currNode = queue[0];
          queue.RemoveAt(0);

          // Ignore the current node if it has been visited before.
          if (visited.Contains(currNode))
          {
            continue;
          }

          // Remember that this node has been visited.
          visited.Add(currNode);
          List<IllFormedCurveNode> neighbors = GetNeighbors(currNode.curve);

          // Add each neighbor to the queue.
          foreach (IllFormedCurveNode neighbor in neighbors)
          {
            // If this neighbor has already been visited, we can ignore it.
            if (visited.Contains(neighbor))
            {
              continue;
            }

            // Remember the current node as the predecessor.
            predecessor[GetNodeIndex(neighbor)].Add(currNode);

            // Enqueue the neighbor.
            queue.Add(neighbor);
          }
        }

        // After the BFS has been run, we can read out the paths from the predecessor array and try to combine the curves.
      }

      return new Curve[1];
    }

    /// <summary>
    /// Given a list of immediate predecessors per node, this function calculates the leading to each node.
    /// </summary>
    /// <param name="allPredecessors">List of immediate predecessor per node in graph.</param>
    /// <returns>A list of paths per node in the graph.</returns>
    private List<List<IllFormedCurveNode>>[] ReconstructPredecessorPaths(List<IllFormedCurveNode>[] allPredecessors)
    {
      return new List<List<IllFormedCurveNode>>[1];
    }

    private List<List<IllFormedCurveNode>>[] FollowPath(IllFormedCurveNode thisNode, ref List<List<IllFormedCurveNode>>[] paths)
    {
      return new List<List<IllFormedCurveNode>>[1];
    }


    /// <summary>
    /// Gets the number of currently added nodes.
    /// </summary>
    /// <returns>The number of currently added nodes.</returns>
    public int GetNumNodes()
    {
      return _nodes.Count;
    }

    /// <summary>
    /// Gets the number of currently added edges.
    /// </summary>
    /// <remarks>This gives the number of directed edges, if you are modelling an undirected graph, the number needs to be divided by 2.</remarks>
    /// <returns>The number of currently added edges.</returns>
    public int GetNumEdges()
    {
      int numEdges = 0;
      foreach (List<IllFormedCurveNode> neighbors in _adjacencyList)
      {
        numEdges += neighbors.Count;
      }
      return numEdges;
    }

    public int GetNumNodesAtBranchPoint()
    {
      int num = 0;
      for (int i = 0; i < _nodes.Count; i++)
      {
        if (_nodes[i].adjacentToBranchPoint)
        {
          num += 1;
        }
      }
      return num;
    }
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

    public static bool operator ==(IllFormedCurveNode left, IllFormedCurveNode right)
    {
      return left.Equals(right);
    }

    public static bool operator !=(IllFormedCurveNode left, IllFormedCurveNode right)
    {
      return !left.Equals(right);
    }

    /// <summary>
    /// Tests for equality. IllFormedCurveNode are equal if the end points of their curves are equal (by some tolerance).
    /// </summary>
    /// <remarks>This comparison only works in this very restricted use-case.</remarks>
    /// <param name="other">The other node to compare to.</param>
    /// <returns>true if nodes are </returns>
    public bool Equals(IllFormedCurveNode other)
    {
      // Compare distances between endpoints - if they are smaller than some tolerance, nodes are considered equal.
      Point3d thisStart = _curve.PointAtStart;
      Point3d thisEnd = _curve.PointAtEnd;
      Point3d otherStart = other.curve.PointAtStart;
      Point3d otherEnd = other.curve.PointAtEnd;

      if (thisStart.DistanceTo(otherStart) < RhinoMath.SqrtEpsilon && thisEnd.DistanceTo(otherEnd) < RhinoMath.SqrtEpsilon ||
          thisStart.DistanceTo(otherEnd) < RhinoMath.SqrtEpsilon && thisEnd.DistanceTo(otherStart) < RhinoMath.SqrtEpsilon)
      {
        return true;
      }
      return false;
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

    /// <summary>
    /// Overrides to string methods to print the start- and end-points of curve.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
      return "(" + curve.PointAtStart.X.ToString() + ", " + curve.PointAtStart.Y.ToString() + ")";
    }
  }
  #endregion
}