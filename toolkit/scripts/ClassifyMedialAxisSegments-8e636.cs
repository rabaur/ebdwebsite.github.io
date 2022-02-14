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
public abstract class Script_Instance_8e636 : GH_ScriptInstance
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
  private void RunScript(List<Curve> MedialAxisCurveList, double InitialStepSize, List<Curve> BoundaryCurveList, List<Point3d> CornerPointList, List<Point3d> BranchPointList, int idx, double t, ref object ClassifiedPointList, ref object TypeList, ref object initialEvaluationPointsOut, ref object SwitchPointLocations, ref object SwitchPointTypes, ref object Chords, ref object BranchPointBoundaries, ref object chord1, ref object chord2, ref object qPoint, ref object radius1, ref object radius2, ref object SwitchPointChords)
  {
    Curve selectedCurve = MedialAxisCurveList[idx];
    qPoint = selectedCurve.PointAt(t);
    Tuple<Point3d, Point3d> cEnds = GetChordEndsParallel(selectedCurve.PointAt(t), BoundaryCurveList);
    chord1 = cEnds.Item1;
    chord2 = cEnds.Item2;
    radius1 = selectedCurve.PointAt(t).DistanceTo(cEnds.Item1);
    radius2 = selectedCurve.PointAt(t).DistanceTo(cEnds.Item2);

    // Parameters corresponding to the locations of the points to be classified.
    List<double> queryParams = new List<double>();

    // Locations of the points to be classified.
    List<Point3d> queryPoints = new List<Point3d>();

    // Maps medial axis segments to sublist (indexes by start and length) containing the classified medial axis points.
    Dictionary<Curve, Tuple<int, int>> medialAxisCurve2Index = new Dictionary<Curve, Tuple<int, int>>();
    foreach (Curve medialAxisCurve in MedialAxisCurveList)
    {
      List<double> parameters = new List<double>();
      if (medialAxisCurve.GetLength() < InitialStepSize)
      {
        parameters.Add(medialAxisCurve.Domain.Mid);
      }
      else
      {
        parameters.AddRange(medialAxisCurve.DivideByLength(InitialStepSize, includeEnds: false));
      }
      Tuple<int, int> tup = new Tuple<int, int>(queryPoints.Count, parameters.Count);
      medialAxisCurve2Index.Add(medialAxisCurve, tup);
      foreach (double param in parameters)
      {
        queryPoints.Add(medialAxisCurve.PointAt(param));
      }
      queryParams.AddRange(parameters);
    }
    int[] types = new int[queryPoints.Count];
    System.Threading.Tasks.Parallel.For(0, queryPoints.Count, i =>
      {
      types[i] = ClassifyMedialAxisPoint(queryPoints[i], CornerPointList, BoundaryCurveList);
      });

    List<int> typeList = types.ToList();

    // Now we identify all switchpoints. A switchpoint is given as a three-tuple:
    // 1. Parameter of the switchpoint on the curve.
    // 2. Type of the previous segment.
    // 3. Type of the following segment
    Dictionary<Curve, List<SwitchPoint>> medialAxisCurve2SwitchPointList = new Dictionary<Curve, List<SwitchPoint>>();
    foreach (Curve medialAxisCurve in MedialAxisCurveList)
    {

      // Initialize.
      medialAxisCurve2SwitchPointList[medialAxisCurve] = new List<SwitchPoint>();

      // Get sublist of classified points parameters and types.
      Tuple<int, int> range = medialAxisCurve2Index[medialAxisCurve];
      List<double> currParams = queryParams.GetRange(range.Item1, range.Item2);
      List<int> currTypes = typeList.GetRange(range.Item1, range.Item2);

      // Now we will construct all switch points:
      // If starts at branchpoint ==> First subsegment is full-ligature.
      // If does not start at branchpoint ==> Most be normal segment starting at corner.
      int currType = 2;
      double lastParam = 0.0;
      for (int i = 0; i < currTypes.Count; i++)
      {
        bool forward = true;
        // If the types differ, there must lie a switchpoint between these two classified points.
        if (currTypes[i] != currType)
        {
          if (i == 0)
          {
            forward = true;
          }
          else
          {
            forward = false;
          }
          double switchParam = FindSwitchPoint(medialAxisCurve, CornerPointList, BoundaryCurveList, lastParam, currParams[i], currType, forward);
          if (i == 0)
          {
            Print(switchParam.ToString());
          }
          medialAxisCurve2SwitchPointList[medialAxisCurve].Add(new SwitchPoint(switchParam, currType, currTypes[i]));
          currType = currTypes[i];
          lastParam = currParams[i];
        }
      }

      if (medialAxisCurve2SwitchPointList[medialAxisCurve].Count == 0)
      {
        throw new Exception(currParams.Count.ToString() + "Switchpointlist was empty");
      }

      // Add last SwitchPoint. If segment ends in branchpoint ==> Last segment is full ligature. Otherwise normal segment ending in corner.
      if (ContainsPointParallel(medialAxisCurve.PointAtEnd, BranchPointList, 0.1))
      {
        Print("Ends in branchpoint");
        // Penultimate switchpoint.
        SwitchPoint penUlt = medialAxisCurve2SwitchPointList[medialAxisCurve].Last();
        // Check if last switchpoint was already transitioning into full ligature. In that case it is not necessary to add final full ligature.
        if (penUlt.nextType == 2)
        {
          continue;
        }

        // Otherwise we need to append a last switchpoint transitioning into a full ligature.
        double openCurveEnd = medialAxisCurve.Domain.Max - 0.01;
        double switchParam = FindSwitchPoint(medialAxisCurve, CornerPointList, BoundaryCurveList, penUlt.param, openCurveEnd, penUlt.nextType, false);
        Print(switchParam.ToString() + ", " + penUlt.param.ToString());
        Print("Type: " + penUlt.nextType);
        medialAxisCurve2SwitchPointList[medialAxisCurve].Add(new SwitchPoint(switchParam, penUlt.prevType, 2));
      }
    }

    ClassifiedPointList = queryPoints;
    TypeList = types;
    List<Point3d> switchPoints = new List<Point3d>();
    List<Line> chords = new List<Line>();
    foreach (KeyValuePair<Curve, List<SwitchPoint>> keyVal in medialAxisCurve2SwitchPointList)
    {
      foreach (SwitchPoint switchPoint in keyVal.Value)
      {
        switchPoints.Add(keyVal.Key.PointAt(switchPoint.param));
        Tuple<Point3d, Point3d> chord = GetChordEndsParallel(keyVal.Key.PointAt(switchPoint.param), BoundaryCurveList);
        chords.Add(new Line(chord.Item1, chord.Item2));
      }
    }

    foreach (KeyValuePair<Curve, List<SwitchPoint>> keyVal in medialAxisCurve2SwitchPointList)
    {

    }
    SwitchPointChords = chords;
    SwitchPointLocations = switchPoints;
  }
  #endregion
  #region Additional
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
  private int ClassifyMedialAxisPoint(Point3d medialAxisPoint, List<Point3d> CornerPointList, List<Curve> BoundaryCurveList)
  {
    // Find the two closest points on the boundary. The connecting line is called "chord", thus chordEnds.
    Tuple<Point3d, Point3d> chordEnds = GetChordEndsParallel(medialAxisPoint, BoundaryCurveList);

    // There are three types of medial-axis points:
    // 1. Chord-ends are both on a normal boundary segment, so not on a corner.
    // 2. Just one chord-end is a corner of the boundary, the other one is a normal segment.
    // 3. Both chord-ends are corners.
    bool isCorner0 = ContainsPointParallel(chordEnds.Item1, CornerPointList, RhinoMath.SqrtEpsilon);
    bool isCorner1 = ContainsPointParallel(chordEnds.Item2, CornerPointList, RhinoMath.SqrtEpsilon);
    if (!isCorner0 && !isCorner1)
    {
      return 0;
    }
    else if (isCorner0 && !isCorner1 || !isCorner0 && isCorner1)
    {
      return 1;
    }
    else
    {
      return 2;
    }
  }

  private double FindSwitchPoint(Curve medialAxisCurve, List<Point3d> cornerPointList, List<Curve> boundaryCurveList, double lowerParam, double higherParam, int lowerType, bool forward)
  {
    // We test in the middle of the interval delimited by lowerParam and higherParam.
    double queryParam = 0.5 * (lowerParam + higherParam);

    // We have reached reasonable accuracy.
    if (queryParam - lowerParam <= RhinoMath.SqrtEpsilon)
    {
      if (forward)
      {
        return higherParam + 0.01;
      }
      else
      {
        return lowerParam;
      }
    }
    else
    {
      // Classify the query-point.
      int queryType = ClassifyMedialAxisPoint(medialAxisCurve.PointAt(queryParam), cornerPointList, boundaryCurveList);

      // If the queryType is different than the type associated with the lower interval parameter, then we have overshot the type-switchpoint and we
      // must sample in the interval [lowerParam, queryParam].
      if (queryType != lowerType)
      {
        return FindSwitchPoint(medialAxisCurve, cornerPointList, boundaryCurveList, lowerParam, queryParam, lowerType, forward);
      }

        // If the queryType is equal to the lowerType, we sample in the upper interval.
      else
      {
        return FindSwitchPoint(medialAxisCurve, cornerPointList, boundaryCurveList, queryParam, higherParam, lowerType, forward);
      }
    }
  }

  private Tuple<Point3d, Point3d> GetChordEndsParallel(Point3d medialAxisPoint, List<Curve> BoundaryCurveList)
  {
    Point3d[] closestPoints = new Point3d[BoundaryCurveList.Count];
    double[] distances = new double[BoundaryCurveList.Count];
    System.Threading.Tasks.Parallel.For(0, BoundaryCurveList.Count, i =>
      {
      double closestParam = 0.0;
      BoundaryCurveList[i].ClosestPoint(medialAxisPoint, out closestParam);
      closestPoints[i] = BoundaryCurveList[i].PointAt(closestParam);
      distances[i] = medialAxisPoint.DistanceTo(closestPoints[i]);
      });
    Array.Sort(distances, closestPoints);
    Point3d c1 = closestPoints[0];
    Point3d c2 = new Point3d();
    int idx = 1;
    while (idx < closestPoints.Length && c1.DistanceTo(closestPoints[idx]) < 0.1)
    {
      idx++;
    }
    c2 = closestPoints[idx];
    return new Tuple<Point3d, Point3d>(c1, c2);
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
  #endregion
}