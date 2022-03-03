using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using System.Linq;
using Rhino.Geometry.Intersect;


/// <summary>
/// This class will be instantiated on demand by the Script component.
/// </summary>
public abstract class Script_Instance_2c318 : GH_ScriptInstance
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
  private void RunScript(List<int> SwitchPointMedialAxisCurveIdx, List<double> SwitchPointParameters, List<int> SwitchPointPreviousTypes, List<int> SwitchPointNextTypes, List<Curve> BoundaryCurveList, List<Point3d> BranchPointList, List<Curve> MedialAxisCurveList, ref object ElementarySurfacesList, ref object ElementarySurfaceTypeList, ref object NodeLocations, ref object Edges, ref object OutBreps, ref object OutTypes, ref object OutLocations, ref object OutDelimitingPoints, ref object OutAdjacencyMatrix)
  {
    // Reassemble input into mapping from medial axis curves to switchpoints.
    Dictionary<Curve, List<SwitchPoint>> medax2SwitchPoint = ReassembleInput(MedialAxisCurveList, SwitchPointMedialAxisCurveIdx, SwitchPointParameters, SwitchPointPreviousTypes, SwitchPointNextTypes);

    // Reassemble medial axis curves.
    MedialAxisCurveList = new List<Curve>();
    foreach (KeyValuePair<Curve, List<SwitchPoint>> keyVal in medax2SwitchPoint)
    {
      MedialAxisCurveList.Add(keyVal.Key);
    }

    // Create surfaces and graph.
    List<Brep> elementaryBreps = new List<Brep>();
    List<int> elementaryBrepTypes = new List<int>();
    Dictionary<Node, List<Node>> graph = new Dictionary<Node, List<Node>>(); // Each key is a vertex, each value is a list of neighbors.
    Dictionary<Curve, List<Node>> medax2Node = new Dictionary<Curve, List<Node>>(); // Maps medial axis curves to nodes for more efficient retrieval when creating branchpoint nodes.
    foreach (Curve medax in MedialAxisCurveList)
    {
      medax2Node[medax] = new List<Node>();
    }
    foreach (KeyValuePair<Curve, List<SwitchPoint>> keyVal in medax2SwitchPoint)
    {
      Curve medax = keyVal.Key;
      List<SwitchPoint> switchPoints = keyVal.Value;
      if (switchPoints[0].prevType != 2)
      {
        throw new Exception("First ligature type was not 2: " + switchPoints[0].prevType);
      }
      if (switchPoints[switchPoints.Count - 1].nextType != 2)
      {
        throw new Exception("Last ligature type was not 2: " + switchPoints[switchPoints.Count - 1].nextType);
      }
      if (switchPoints.Count == 2)
      {
        if (switchPoints[0].nextType == 2 && switchPoints[1].prevType == 2)
        {
          continue;
        }
      }

      // Generating all nodes corresponding to this medial axis segment on the fly. Ensuring adjacency between consecutive nodes afterwards.
      List<Node> nodes = new List<Node>();
      List<SwitchPoint> switchPointsToRemove = new List<SwitchPoint>();
      for (int i = 0; i < switchPoints.Count - 1; i++)
      {
        SwitchPoint currSwitchPoint = switchPoints[i];
        SwitchPoint nextSwitchPoint = switchPoints[i + 1];
        Point3d currSwitchLoc = medax.PointAt(currSwitchPoint.param);
        Point3d nextSwitchLoc = medax.PointAt(nextSwitchPoint.param);
        Chord c0 = GetChordParallel(currSwitchLoc, BoundaryCurveList);
        Chord c1 = GetChordParallel(nextSwitchLoc, BoundaryCurveList);
        Curve currTrim = medax.Trim(switchPoints[i].param, nextSwitchPoint.param);

        // There tend to be instabilities in proximity of branchpoints. Often this results in spurious, small segments being classified as semi,
        // however these segments are delimited by faulty corners. When these surfaces of these segments are generated, they cover the surface
        // that would normally be covered by a full-ligature.
        if (currTrim != null)
        {
          if (currTrim.GetLength() < 1.0 && currSwitchPoint.nextType == 1 && ContainsPointParallel(currTrim.PointAtStart, BranchPointList, 1.5))
          {
            switchPointsToRemove.Add(nextSwitchPoint);
            continue;
          }
        }
        if (currSwitchPoint.nextType != nextSwitchPoint.prevType)
        {
          throw new Exception("The type of consecutive switchpoints did not match.");
        }
        Brep currBrep = CreateBrepFromChords(c0, c1);
        int currType = currSwitchPoint.nextType;
        elementaryBreps.Add(currBrep);
        elementaryBrepTypes.Add(currType);

        // Adding the node.
        nodes.Add(new Node(currBrep, currType, ComputePolyCenter(currBrep), new List<Point3d>() { currSwitchLoc, nextSwitchLoc } ));
      }

      // Remove invalid switchpoints.
      foreach (SwitchPoint switchPointToRemove in switchPointsToRemove)
      {
        int idx = switchPointsToRemove.IndexOf(switchPointToRemove);
        Print("Removing: " + idx);
        // Change previous and next-types of adjacent switchpoints.
        // Note that an invalid switchpoint can neither be the last or first switchpoint, as the first and last one are always type 2 nodes.
        Print(switchPoints[idx].prevType + ", " + switchPoints[idx + 1].prevType);
        SwitchPoint switch0 = new SwitchPoint(switchPoints[idx + 1].param, switchPoints[idx].prevType, switchPoints[idx + 1].nextType);
        switchPoints[idx + 1] = switch0;
        switchPoints.Remove(switchPointToRemove);
      }

      // Adding newly created nodes to graph and establishing adjacency between consecutive nodes.
      for (int i = 0; i < nodes.Count; i++)
      {
        Node node = nodes[i];
        graph[node] = new List<Node>();

        // A node is adjacent to the node corresponding to the last segment and the next segment.
        if (i > 0)
        {
          graph[node].Add(nodes[i - 1]);
        }
        if (i < nodes.Count - 1)
        {
          graph[node].Add(nodes[i + 1]);
        }
      }
      // Only the first or last node can be adjacent to a branchpoint.
      medax2Node[medax] = new List<Node>() { nodes[0], nodes[nodes.Count - 1] };
    }

    // Add the surfaces that correspond to branchpoints.
    foreach (Point3d branchPoint in BranchPointList)
    {
      List<Curve> adjMedaxs = new List<Curve>();
      List<double> adjParams = new List<double>();
      foreach (KeyValuePair<Curve, List<Node>> keyVal in medax2Node)
      {
        double closestParam;
        Curve medax = keyVal.Key;
        medax.ClosestPoint(branchPoint, out closestParam);
        if (branchPoint.DistanceTo(medax.PointAt(closestParam)) < 0.05)
        {
          adjMedaxs.Add(medax);
          adjParams.Add(closestParam);
        }
      }

      // Generate the chord (on the correct side of the medial axis segment) for each medial axis segment that ends in this branchpoint.
      List<Chord> adjChords = new List<Chord>();
      List<Node> adjNodes = new List<Node>();
      for (int i = 0; i < adjMedaxs.Count; i++)
      {
        if (Math.Abs(adjMedaxs[i].Domain.Min - adjParams[i]) < Math.Abs(adjMedaxs[i].Domain.Max - adjParams[i]))
        {
          adjChords.Add(GetChordParallel(adjMedaxs[i].PointAt(medax2SwitchPoint[adjMedaxs[i]].First().param), BoundaryCurveList));
          if (medax2Node[adjMedaxs[i]].Count > 0)
          {
            adjNodes.Add(medax2Node[adjMedaxs[i]][0]);
          }
        }
        else
        {
          adjChords.Add(GetChordParallel(adjMedaxs[i].PointAt(medax2SwitchPoint[adjMedaxs[i]].Last().param), BoundaryCurveList));
          if (medax2Node[adjMedaxs[i]].Count > 0)
          {
            adjNodes.Add(medax2Node[adjMedaxs[i]][1]);
          }
        }
      }

      // Get the endpoints of all chords, as they delimit the newly created face.
      List<Point3d> chordEnds = new List<Point3d>();
      foreach (Chord chord in adjChords)
      {
        chordEnds.Add(chord.line.PointAtStart);
        chordEnds.Add(chord.line.PointAtEnd);
      }

      // Make the corners unique, such that we do not have double corners.
      List<Point3d> uniqueChordEnds = new List<Point3d>();
      foreach (Point3d chordEnd in chordEnds)
      {
        bool tooClose = false;
        foreach (Point3d added in uniqueChordEnds)
        {
          if (added.DistanceTo(chordEnd) < 0.1)
          {
            tooClose = true;
            break;
          }
        }
        if (!tooClose)
        {
          uniqueChordEnds.Add(chordEnd);
        }
      }

      // Determine convex hull in order to get points in correct order to generate boundary.
      List<Point3d> hull = ConvexHullXY(uniqueChordEnds);

      // Generate the boundary edges.
      List<LineCurve> edges = new List<LineCurve>();
      for (int i = 0; i < hull.Count; i++)
      {
        edges.Add(new LineCurve(hull[i], hull[(i + 1) % hull.Count]));
      }

      Brep[] currBreps = Brep.CreatePlanarBreps(edges, RhinoMath.SqrtEpsilon);
      if (currBreps.Length != 1)
      {
        throw new Exception("Too little or too many surfaces generated");
      }
      Brep currBrep = currBreps[0];
      elementaryBreps.Add(currBrep);
      elementaryBrepTypes.Add(2);
      Node newNode = new Node(currBrep, 2, ComputePolyCenter(currBrep), new List<Point3d>() { branchPoint, branchPoint });
      graph[newNode] = adjNodes;
      foreach (Node adjNode in adjNodes)
      {
        graph[adjNode].Add(newNode);
      }
    }

    // Connect type 2 breps which share a medial axis segment.
    foreach (Curve medax in MedialAxisCurveList)
    {
      List<SwitchPoint> switchPoints = medax2SwitchPoint[medax];
      if (switchPoints.Count != 2)
      {
        // We are only interested in purely type 2 segments.
        continue;
      }
      if (!(switchPoints[0].nextType == 2 && switchPoints[1].prevType == 2))
      {
        // We are only interested in purely type 2 segments.
        continue;
      }

      // Getting the branchpoints.
      Point3d startBp = medax.PointAtStart;
      Point3d endBp = medax.PointAtEnd;

      // Search the nodes corresponding to these two branchpoints.
      // TODO: Restrict search to graph only consisting on type 2 nodes (search on subgraph).
      List<Node> toConnect = new List<Node>();
      foreach (KeyValuePair<Node, List<Node>> keyVal in graph)
      {
        Node currNode = keyVal.Key;
        List<Node> neighbors = keyVal.Value;
        if (currNode.type != 2)
        {
          continue;
        }
        if (currNode.delimitingPoints[0].DistanceTo(startBp) < 1.0 || currNode.delimitingPoints[0].DistanceTo(endBp) < 1.0)
        {
          toConnect.Add(currNode);
        }
      }
      
      // Connect all nodes that are adjacent to this medial axis segment.
      for (int i = 0; i < toConnect.Count; i++)
      {
        Node node0 = toConnect[i];
        for (int j = 0; j < toConnect.Count; j++)
        {
          if (i == j)
          {
            continue;
          }
          Node node1 = toConnect[j];
          graph[node0].Add(node1);
        }
      }
    }
    ElementarySurfacesList = elementaryBreps;
    ElementarySurfaceTypeList = elementaryBrepTypes;
    List<Point3d> nodeLocs = new List<Point3d>();
    List<LineCurve> graphEdges = new List<LineCurve>();
    foreach (KeyValuePair<Node, List<Node>> keyVal in graph)
    {
      nodeLocs.Add(keyVal.Key.location);
      foreach (Node neighbor in keyVal.Value)
      {
        graphEdges.Add(new LineCurve(keyVal.Key.location, neighbor.location));
      }
    }
    NodeLocations = nodeLocs;
    Edges = graphEdges;

    // Deconstruct graph.
    List<Brep> outBreps = new List<Brep>();
    List<int> outTypes = new List<int>();
    List<Point3d> outLocations = new List<Point3d>();
    DataTree<Point3d> outDelimitingPoints = new DataTree<Point3d>();
    Matrix adjacencyMatrix = new Matrix(graph.Count, graph.Count);
    DeconstructGraph(graph, ref outBreps, ref outTypes, ref outLocations, ref outDelimitingPoints, ref adjacencyMatrix);
    OutBreps = outBreps;
    OutTypes = outTypes;
    OutLocations = outLocations;
    OutDelimitingPoints = outDelimitingPoints;
    OutAdjacencyMatrix = adjacencyMatrix;
  }
  #endregion
  #region Additional
  private Dictionary<Curve, List<SwitchPoint>> ReassembleInput(List<Curve> medialAxisCurves, List<int> medialAxisCurveIdxs, List<double> parameters, List<int> previousTypes, List<int> nextTypes)
  {
    Dictionary<Curve, List<SwitchPoint>> res = new Dictionary<Curve, List<SwitchPoint>>();
    for (int i = 0; i < medialAxisCurveIdxs.Count; i++)
    {
      Curve medax = medialAxisCurves[medialAxisCurveIdxs[i]];
      if (!res.ContainsKey(medax))
      {
        res[medax] = new List<SwitchPoint>();
      }
      double param = parameters[i];
      int prevType = previousTypes[i];
      int nextType = nextTypes[i];
      res[medax].Add(new SwitchPoint(param, prevType, nextType));
    }
    return res;
  }

  public struct SwitchPoint
  {
    public SwitchPoint(double param, int prevType, int nextType)
    {
      this.param = param;
      this.prevType = prevType;
      this.nextType = nextType;
    }
    public double param;
    public int prevType;
    public int nextType;
  }

  public struct Chord
  {
    public Chord(Tuple<double, double> chordParams, Tuple<Curve, Curve> boundaryCurves, Tuple<Point3d, Point3d> chordPoints)
    {
      parameters = chordParams;
      curves = boundaryCurves;
      points = chordPoints;
      line = new LineCurve(chordPoints.Item1, chordPoints.Item2);
    }
    public Tuple<double, double> parameters;
    public Tuple<Curve, Curve> curves;
    public Tuple<Point3d, Point3d> points;
    public LineCurve line;
  }
  public struct PointOnCurve
  {
    public PointOnCurve(Curve curve, double param, Point3d point)
    {
      this.curve = curve;
      this.param = param;
      this.point = point;
    }
    public Curve curve;
    public double param;
    public Point3d point;
  }

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

  private Chord GetChordParallel(Point3d queryPoint, List<Curve> boundaryCurveList)
  {
    PointOnCurve[] closestPoints = new PointOnCurve[boundaryCurveList.Count];  // Holds closest point on each boundary-segment to point on medial axis.
    double[] distances = new double[boundaryCurveList.Count];  // Holds the distances to the closest points.

    // Determine closest points on boundary segments and distances to them.
    System.Threading.Tasks.Parallel.For(0, boundaryCurveList.Count, i =>
    {
      double closestParam = 0.0;
      boundaryCurveList[i].ClosestPoint(queryPoint, out closestParam);
      Point3d closestPoint = boundaryCurveList[i].PointAt(closestParam);
      distances[i] = queryPoint.DistanceTo(closestPoint);
      closestPoints[i] = new PointOnCurve(boundaryCurveList[i], closestParam, closestPoint);
    });

    Array.Sort(distances, closestPoints);
    Point3d c0 = closestPoints[0].point;
    int idx = 1;
    while (idx < closestPoints.Length && c0.DistanceTo(closestPoints[idx].point) < 1.0)
    {
      idx++;
    }
    double param0 = closestPoints[0].param;
    double param1 = closestPoints[idx].param;
    Curve curve0 = closestPoints[0].curve;
    Curve curve1 = closestPoints[idx].curve;
    Point3d point0 = closestPoints[0].point;
    Point3d point1 = closestPoints[idx].point;
    return new Chord(new Tuple<double, double>(param0, param1), new Tuple<Curve, Curve>(curve0, curve1), new Tuple<Point3d, Point3d>(point0, point1));
  }

  private Brep CreateBrepFromChords(Chord c0, Chord c1)
  {
    List<Curve> edges = new List<Curve> { c0.line, c1.line }; // Contains the edges of the surface to be generated.

    // Case: the resulting face is triangular, i.e. two of the endpoints of each chord are identical.
    if (c0.points.Item1.DistanceTo(c1.points.Item1) < RhinoMath.SqrtEpsilon)
    {
      edges.Add(new LineCurve(c0.points.Item2, c1.points.Item2));
    }
    else if (c0.points.Item1.DistanceTo(c1.points.Item2) < RhinoMath.SqrtEpsilon)
    {
      edges.Add(new LineCurve(c0.points.Item2, c1.points.Item1));
    }
    else if (c0.points.Item2.DistanceTo(c1.points.Item1) < RhinoMath.SqrtEpsilon)
    {
      edges.Add(new LineCurve(c0.points.Item1, c1.points.Item2));
    }
    else if (c0.points.Item2.DistanceTo(c1.points.Item2) < RhinoMath.SqrtEpsilon)
    {
      edges.Add(new LineCurve(c0.points.Item1, c1.points.Item1));
    }
    else
    {
      // None of the chord-endpoints are identical, so we are dealing with a rectangular surface.

      // Here we need to check that we connect the endpoints such that the connecting lines do not cross.
      Curve sameA = new LineCurve(c0.points.Item1, c1.points.Item1);
      Curve sameB = new LineCurve(c0.points.Item2, c1.points.Item2);

      // Check intersection.
      if (Intersection.CurveCurve(sameA, sameB, RhinoMath.SqrtEpsilon, RhinoMath.SqrtEpsilon).Count == 0)
      {
        edges.Add(sameA);
        edges.Add(sameB);
      }
      else
      {
        // Then we need to connect the other way around.
        Curve diffA = new LineCurve(c0.points.Item1, c1.points.Item2);
        Curve diffB = new LineCurve(c0.points.Item2, c1.points.Item1);

        // Should not happen, but just to be sure.
        if (Intersection.CurveCurve(diffA, diffB, RhinoMath.SqrtEpsilon, RhinoMath.SqrtEpsilon).Count > 0)
        {
          throw new Exception("Connecting lines between chords cross both ways");
        }

        edges.Add(diffA);
        edges.Add(diffB);
      }
    }
    return Brep.CreateEdgeSurface(edges);
  }

  private Point3d ComputePolyCenter(Brep brep)
  {
    List<Point3d> corners = new List<Point3d>();
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
    Point3d center = Point3d.Origin;
    foreach (Point3d corner in corners)
    {
      center += corner;
    }
    return center / corners.Count;
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

  private List<Point3d> GetUniqueCorners(List<Brep> breps)
  {
    List<Point3d> corners = new List<Point3d>();
    foreach (Brep brep in breps)
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
    return corners;
  }

  private List<Point3d> ConvexHullXY(List<Point3d> points)
  {
    // If we have less than 4 points, the hull is trivially convex.
    if (points.Count <= 3)
    {
      return points;
    }

    // Find most left point.
    int startIdx = 0;
    for (int i = 1; i < points.Count; i++)
    {
      if (points[i].X < points[startIdx].X)
      {
        startIdx = i;
      }
    }

    // Wrapping.
    List<Point3d> hull = new List<Point3d>();
    int last = startIdx;
    int next;
    do
    {
      hull.Add(points[last]);
      next = (last + 1) % points.Count;
      for (int i = 0; i < points.Count; i++)
      {
        if (Orientation(points[last], points[i], points[next]) == 2)
        {
          // The current segment is a right turn.
          next = i;
        }
      }
      last = next;
    }
    while (last != startIdx);
    return hull;
  }

  private int Orientation(Point3d p, Point3d q, Point3d r)
  {
    double val = (q.Y - p.Y) * (r.X - q.X) - (q.X - p.X) * (r.Y - q.Y);

    if (val == 0.0)
    {
      return 0; // collinear
    }
    return (val > 0) ? 1 : 2; // clock or counterclock wise
  }
  #endregion
}