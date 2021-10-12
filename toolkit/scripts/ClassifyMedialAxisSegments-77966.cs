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


/// <summary>
/// This class will be instantiated on demand by the Script component.
/// </summary>
public abstract class Script_Instance_77966 : GH_ScriptInstance
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
  private void RunScript(List<Curve> MedialAxisCurveList, int InitialSubdivision, List<Curve> BoundaryCurveList, List<Point3d> CornerPointList, List<Point3d> BranchPointList, ref object ClassifiedPointList, ref object TypeList, ref object initialEvaluationPointsOut, ref object SwitchPointLocations, ref object SwitchPointTypes, ref object Chords, ref object BranchPointBoundaries)
  {
    // Find shortest segment on the boundary.
    double shortestSegmentLength = MedialAxisCurveList.Min(medialAxisCurve => medialAxisCurve.GetLength());
    double initialDelta = shortestSegmentLength / InitialSubdivision;
    // initialDelta = 10.0f;

    // Generate the initial evaluation points.
    List<double> initialEvaluationParameters = new List<double>();
    List<Curve> replicatedMedialAxisCurves = new List<Curve>();

    // In order to keep track where the evaluation points belonging to a single sequence belong.
    List<int> sequenceStarts = new List<int>();
    List<int> sequenceLengths = new List<int>();

    // Not necessarily all medial axis segments will be be evaluated, so we need to keep track which are.
    List<Curve> evaluatedMedialAxisCurves = new List<Curve>();

    // Subdivide each medialAxis to generate evaluation points.
    foreach (Curve medialAxisCurve in MedialAxisCurveList)
    {
      double[] parameters = medialAxisCurve.DivideByLength(initialDelta, false);
      if (parameters == null)
      {
        continue;
      }
      evaluatedMedialAxisCurves.Add(medialAxisCurve);
      sequenceStarts.Add(initialEvaluationParameters.Count);
      initialEvaluationParameters.AddRange(parameters);
      replicatedMedialAxisCurves.AddRange(Enumerable.Repeat(medialAxisCurve, parameters.Length));
      sequenceLengths.Add(parameters.Length);
    }

    // Array that will hold the point descriptors for each evaluation point.
    MedialAxisPoint[] medialAxisPointDescriptorArray = new MedialAxisPoint[initialEvaluationParameters.Count];

    // Classify all medial axis points.
    System.Threading.Tasks.Parallel.For(0, initialEvaluationParameters.Count, i =>
      {
      medialAxisPointDescriptorArray[i] = ClassifyMedialAxisPoint(replicatedMedialAxisCurves[i], initialEvaluationParameters[i], BoundaryCurveList, CornerPointList); ;
      });
    List<MedialAxisPoint> medialAxisPointDescriptors = medialAxisPointDescriptorArray.ToList();

    // Write out the results.
    TypeList = medialAxisPointDescriptors.Select(pd => pd.Type).ToList();
    ClassifiedPointList = medialAxisPointDescriptors.Select(pd => pd.Point).ToList();

    // Select all pairs of point-descriptor between which the medial axis segment type changes.
    List<List<MedialAxisPointPair>> switchPoints = new List<List<MedialAxisPointPair>>();
    for (int i = 0; i < evaluatedMedialAxisCurves.Count; i++)
    {
      Curve medialAxisCurve = evaluatedMedialAxisCurves[i];
      List<MedialAxisPoint> medialAxisPoints = medialAxisPointDescriptors.GetRange(sequenceStarts[i], sequenceLengths[i]);
      List<MedialAxisPointPair> currSwitchPoints = new List<MedialAxisPointPair>();
      int currType = medialAxisPoints[0].Type;
      for (int j = 1; j < medialAxisPoints.Count; j++)
      {
        if (medialAxisPoints[j].Type != currType)
        {
          currSwitchPoints.Add(new MedialAxisPointPair(medialAxisPoints[j - 1], medialAxisPoints[j]));
          currType = medialAxisPoints[j].Type;
        }
      }
      switchPoints.Add(currSwitchPoints);
    }

    int numSwitchPoints = switchPoints.Select(l => l.Count).ToList().Sum();
    Print("Number of switch-points: " + numSwitchPoints.ToString());
    double numSwitchPointsPercent = ((double)numSwitchPoints) / medialAxisPointDescriptors.Count * 100.0;
    Print("That's only " + numSwitchPointsPercent.ToString() + " percent of all evaluated points.");

    // Extract switchpoint-locations and types for debugging.
    List<Point3d> switchPointLocations = new List<Point3d>();
    List<int> switchPointTypes = new List<int>();
    List<LineCurve> chords = new List<LineCurve>();
    foreach (List<MedialAxisPointPair> subList in switchPoints)
    {
      foreach (MedialAxisPointPair pair in subList)
      {
        switchPointLocations.Add(pair.PointA.Point);
        switchPointLocations.Add(pair.PointB.Point);
        switchPointTypes.Add(pair.PointA.Type);
        switchPointTypes.Add(pair.PointB.Type);
        chords.Add(pair.PointA.Chord);
        chords.Add(pair.PointB.Chord);
      }
    }

    SwitchPointLocations = switchPointLocations;
    SwitchPointTypes = switchPointTypes;
    Chords = chords;
    BranchPointBoundaries = CreateNodesAroundBranchPoints(BranchPointList, BoundaryCurveList, 7.5);
  }
  #endregion
  #region Additional

  const int NORM = 0;
  const int SEMI = 1;
  const int FULL = 2;

  /// <summary>
  /// Classifies a point on a medial axis segment.
  /// </summary>
  /// <param name="medialAxisCurve">The medial axis segment the point lies on.</param>
  /// <param name="param">The parameter at which the medial axis should be evaluated on.</param>
  /// <param name="boundaryCurves">The boundaries of the shape described by the medial axis.</param>
  /// <param name="corners">The list of all corners of the boundary.</param>
  /// <returns>A medial axis point descriptor.</returns>
  private MedialAxisPoint ClassifyMedialAxisPoint(Curve medialAxisCurve, double param, List<Curve> boundaryCurves, List<Point3d> corners)
  {
    // The point on the medial axis.
    Point3d point = medialAxisCurve.PointAt(param);

    // Find the closest points for each boundary curve.
    List<double> closestParams = new List<double>();
    foreach (Curve boundaryCurve in boundaryCurves)
    {
      double closeParam;
      boundaryCurve.ClosestPoint(point, out closeParam);
      closestParams.Add(closeParam);
    }

    // Determine the distances to each of the closest points on the boundary curves.
    List<double> distances = Enumerable.Range(0, closestParams.Count).Select(i => point.DistanceTo(boundaryCurves[i].PointAt(closestParams[i]))).ToList();

    // Determine the smallest two distances.
    List<int> minIdxs = FindSmallestTwo(distances);
    int idxA = minIdxs[0];
    int idxB = minIdxs[1];

    // The closest boundaries.
    List<Curve> chordEndPointCurves = new List<Curve>() { boundaryCurves[idxA], boundaryCurves[idxB] };

    // The parameters on the closest boundaries.
    List<double> chordEndPointParameters = new List<double>() { closestParams[idxA], closestParams[idxB] };

    // The points on the closest boundaries.
    Point3d pointA = boundaryCurves[idxA].PointAt(closestParams[idxA]);
    Point3d pointB = boundaryCurves[idxB].PointAt(closestParams[idxB]);
    List<Point3d> chordEndPoints = new List<Point3d> { pointA, pointB };

    // Determine the type of this point.
    bool pointAIsCorner = corners.Select(corner => corner.DistanceTo(pointA) < RhinoMath.DefaultDistanceToleranceMillimeters).ToList().Any(boolean => boolean == true);
    bool pointBIsCorner = corners.Select(corner => corner.DistanceTo(pointB) < RhinoMath.DefaultDistanceToleranceMillimeters).ToList().Any(boolean => boolean == true);
    int type = 0;
    if (!pointAIsCorner && !pointBIsCorner)
    {
      type = NORM;
    }
    else if (pointAIsCorner != pointBIsCorner)
    {
      type = SEMI;
    }
    else
    {
      type = FULL;
    }
    return new MedialAxisPoint(medialAxisCurve, param, chordEndPointCurves, chordEndPointParameters, chordEndPoints, type);
  }


  /// <summary>
  /// Returns the indices of the smallest two elements in <paramref name="measures"/>.
  /// </summary>
  /// <param name="measure">The list of values to search in.</param>
  /// <returns>List of two indices, the first one of which corresponds to the smallest, the second one to the second-smallest element.</returns>
  private List<int> FindSmallestTwo(List<double> measures)
  {
    // Invariant 1: measures[idxA] < measures[idxB].
    int idxA = 0;
    int idxB = 1;
    double minA = measures[idxA];
    double minB = measures[idxB];

    // Check if invariant 1 holds.
    if (minB < minA)
    {
      idxA = 1;
      idxB = 0;
      minA = measures[idxA];
      minB = measures[idxB];
    }

    // Go through remainder of list and find minima.
    for (int i = 2; i < measures.Count; i++)
    {
      if (measures[i] < minA)
      {
        // Need to update both pointers.
        idxB = idxA;
        minB = minA;
        idxA = i;
        minA = measures[i];
      }
      else if (measures[i] < minB)
      {
        // Only need to update pointer to second-smallest element.
        idxB = i;
        minB = measures[i];
      }
    }

    // Return both indices as list.
    return new List<int>() { idxA, idxB };
  }

  /// <summary>
  /// A pair of medial axis points, used to determine switch-points.
  /// </summary>
  public struct MedialAxisPointPair
  {
    public MedialAxisPoint PointA;
    public MedialAxisPoint PointB;
    
    public MedialAxisPointPair(MedialAxisPoint pointA, MedialAxisPoint pointB)
    {
      PointA = pointA;
      PointB = pointB;
    }
  }

  /// <summary>
  /// A descriptor for a medial axis point.
  /// </summary>
  public struct MedialAxisPoint
  {
    public Curve MedialAxisCurve;
    public double Parameter;
    public Point3d Point;
    public List<Curve> ChordEndPointCurves;
    public List<double> ChordEndPointParameters;
    public List<Point3d> ChordEndPoints;
    public int Type;
    public LineCurve Chord;

    public MedialAxisPoint(Curve medialAxisCurve, double parameter, List<Curve> chordEndPointCurves, List<double> chordEndPointParameters, List<Point3d> chordEndPoints, int type)
    {
      MedialAxisCurve = medialAxisCurve;
      Parameter = parameter;
      Point = medialAxisCurve.PointAt(parameter);
      ChordEndPointCurves = chordEndPointCurves;
      ChordEndPointParameters = chordEndPointParameters;
      ChordEndPoints = chordEndPoints;
      Type = type;
      Chord = new LineCurve(ChordEndPoints[0], ChordEndPoints[1]);
    }
  }

  /// <summary>
  /// TODO: Rewrite into function that can take points and create convex brep.
  /// </summary>
  /// <param name="points"></param>
  /// <returns></returns>
  private Brep CreateConvexBrepFromPoints(List<Point3d> points)
  {
    Grasshopper.Kernel.Geometry.Node2List points2D = new Grasshopper.Kernel.Geometry.Node2List(points);
    Polyline boundary = Grasshopper.Kernel.Geometry.ConvexHull.Solver.ComputeHull(points2D);
    try
    {
      return Brep.CreatePlanarBreps(boundary.ToNurbsCurve(), RhinoMath.DefaultDistanceToleranceMillimeters)[0];
    }
    catch {
      return null;
    }
  }

  /// <summary>
  /// TODO: Rewrite into function that generates breps around branchpoints.
  /// </summary>
  /// <param name="branchPoints"></param>
  /// <param name="boundaryCurves"></param>
  /// <param name="tol"></param>
  /// <returns></returns>
  private List<Brep> CreateNodesAroundBranchPoints(List<Point3d> branchPoints, List<Curve> boundaryCurves, double tol)
  {
    List<Brep> boundaries = new List<Brep>();
    foreach (Point3d branchPoint in branchPoints)
    {
      List<Point3d> closestPoints = GetClosestPointsToCurves(branchPoint, boundaryCurves);
      List<double> distances = closestPoints.Select(cp => cp.DistanceTo(branchPoint)).ToList();
      List<PointAndDistance> pointsAndDistances = Enumerable.Range(0, closestPoints.Count).Select(i => new PointAndDistance(closestPoints[i], distances[i])).ToList();
      pointsAndDistances = pointsAndDistances.OrderBy(pD => pD.Distance).ToList();
      List<Point3d> corners = new List<Point3d>();
      double minDistance = pointsAndDistances[0].Distance;
      foreach (PointAndDistance pointAndDistance in pointsAndDistances)
      {
        if (pointAndDistance.Distance - minDistance < tol)
        {
          corners.Add(pointAndDistance.Point);
        }
      }
      Brep boundary = CreateConvexBrepFromPoints(corners);
      boundaries.Add(boundary);
    }
    return boundaries;
  }

  /// <summary>
  /// Finds the closest point from <paramref name="point"/> for each curve in <paramref name="curves"/>.
  /// </summary>
  /// <param name="point">The points from which to find the closest points.</param>
  /// <param name="curves">The curves to find the closest points.</param>
  /// <returns>The list of closest points, where the i-th point corresponds to the i-th curve in <paramref name="curves"/></returns>
  private List<Point3d> GetClosestPointsToCurves(Point3d point, List<Curve> curves)
  {
    List<double> closestParams = new List<double>();
    foreach (Curve curve in curves)
    {
      double closeParam;
      curve.ClosestPoint(point, out closeParam);
      closestParams.Add(closeParam);
    }
    return Enumerable.Range(0, curves.Count).Select(i => curves[i].PointAt(closestParams[i])).ToList();
  }

  public struct PointAndDistance
  {
    public Point3d Point;
    public double Distance;

    public PointAndDistance(Point3d point, double distance)
    {
      Point = point;
      Distance = distance;
    }
  }
  #endregion
}