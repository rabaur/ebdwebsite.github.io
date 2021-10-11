using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;



/// <summary>
/// This class will be instantiated on demand by the Script component.
/// </summary>
public abstract class Script_Instance_f4d0e : GH_ScriptInstance
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
  private void RunScript(List<Curve> MedialAxisCurveList, int InitialSubdivision, List<Curve> BoundaryCurveList, List<Point3d> CornerPointList, ref object ClassifiedPointList, ref object TypeList, ref object initialEvaluationPointsOut, ref object SideA, ref object SideB, ref object Chords, ref object ChordA)
  {
    // Find shortest segment on the boundary.
    double shortestSegmentLength = MedialAxisCurveList.Min(medialAxisCurve => medialAxisCurve.GetLength());
    double initalDelta = shortestSegmentLength / InitialSubdivision;

    // Generate the initial evaluation points.
    List<double> initialEvaluationParameters = new List<double>();
    List<Curve> replicatedMedialAxisCurves = new List<Curve>();

    // In order to keep track where the evaluation points belonging to a single sequence belong.
    List<int> sequenceStarts = new List<int>();
    List<int> sequenceEnds = new List<int>();

    // Subdivide each medialAxis to generate evaluation points.
    foreach (Curve medialAxisCurve in MedialAxisCurveList)
    {
      sequenceStarts.Add(initialEvaluationParameters.Count);
      double[] parameters = medialAxisCurve.DivideByLength(initalDelta, false);
      initialEvaluationParameters.AddRange(parameters);
      replicatedMedialAxisCurves.AddRange(Enumerable.Repeat(medialAxisCurve, parameters.Length));
      sequenceEnds.Add(initialEvaluationParameters.Count - 1);
    }


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

  public struct MedialAxisPoint
  {
    public Curve MedialAxisCurve;
    public double Parameter;
    public Point3d Point;
    public List<Curve> ChordEndPointCurves;
    public List<double> ChordEndPointParameters;
    public List<Point3d> ChordEndPoints;
    public int Type;

    public MedialAxisPoint(Curve medialAxisCurve, double parameter, List<Curve> chordEndPointCurves, List<double> chordEndPointParameters, List<Point3d> chordEndPoints, int type)
    {
      MedialAxisCurve = medialAxisCurve;
      Parameter = parameter;
      Point = medialAxisCurve.PointAt(parameter);
      ChordEndPointCurves = chordEndPointCurves;
      ChordEndPointParameters = chordEndPointParameters;
      ChordEndPoints = chordEndPoints;
      Type = type;
    }
  }
  #endregion
}