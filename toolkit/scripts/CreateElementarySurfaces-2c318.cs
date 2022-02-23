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
  private void RunScript(List<int> SwitchPointMedialAxisCurveIdx, List<double> SwitchPointParameters, List<int> SwitchPointPreviousTypes, List<int> SwitchPointNextTypes, List<Curve> BoundaryCurveList, List<Point3d> BranchPointList, List<Curve> MedialAxisCurveList, ref object ElementarySurfacesList, ref object ElementarySurfaceTypeList)
  {
    // Reassemble input into mapping from medial axis curves to switchpoints.
    Dictionary<Curve, List<SwitchPoint>> medax2SwitchPoint = ReassembleInput(MedialAxisCurveList, SwitchPointMedialAxisCurveIdx, SwitchPointParameters, SwitchPointPreviousTypes, SwitchPointNextTypes);

    // Reassemble medial axis curves.
    MedialAxisCurveList = new List<Curve>();
    foreach (KeyValuePair<Curve, List<SwitchPoint>> keyVal in medax2SwitchPoint)
    {
      MedialAxisCurveList.Add(keyVal.Key);
    }

    // Create surfaces.
    List<Brep> elementaryBreps = new List<Brep>();
    List<int> elementaryBrepTypes = new List<int>();
    List<Curve> faultySegs = new List<Curve>();
    List<Curve> segmentForEachSurf = new List<Curve>();
    List<string> typesForEachSurf = new List<string>();
    List<Curve> line0ForEachSurf = new List<Curve>();
    List<Curve> line1ForEachSurf = new List<Curve>();
    foreach (KeyValuePair<Curve, List<SwitchPoint>> keyVal in medax2SwitchPoint)
    {
      Curve medaxCurve = keyVal.Key;
      List<SwitchPoint> switches = keyVal.Value;
      if (switches[0].prevType != 2)
      {
        throw new Exception("First ligature type was not 2: " + switches[0].prevType);
      }
      if (switches[switches.Count - 1].nextType != 2)
      {
        throw new Exception("Last ligature type was not 2: " + switches[switches.Count - 1].nextType);
      }
      if (switches.Count == 2)
      {
        if (switches[0].nextType == 2 && switches[1].prevType == 2)
        {
          continue;
        }
      }

      for (int i = 0; i < switches.Count - 1; i++)
      {
        Chord c0 = GetChordParallel(medaxCurve.PointAt(switches[i].param), BoundaryCurveList);
        Chord c1 = GetChordParallel(medaxCurve.PointAt(switches[i + 1].param), BoundaryCurveList);
        line0ForEachSurf.Add(c0.line);
        line1ForEachSurf.Add(c1.line);
        Curve currTrim = medaxCurve.Trim(switches[i].param, switches[i + 1].param);
        segmentForEachSurf.Add(medaxCurve.Trim(switches[i].param, switches[i + 1].param));
        typesForEachSurf.Add(switches[i].prevType + ", " + switches[i].nextType + ", " + switches[i + 1].prevType + ", " + switches[i + 1].nextType);

        // There tend to be instabilities in proximity of branchpoints. Often this results in spurious, small segments being classified as semi,
        // however these segments are delimited by faulty corners. When these surfaces of these segments are generated, they cover the surface
        // that would normally be covered by a full-ligature.
        if (currTrim != null)
        {
          if (currTrim.GetLength() < 1.0 && switches[i].nextType == 1 && ContainsPointParallel(currTrim.PointAtStart, BranchPointList, 1.0))
          {
            continue;
          }
        }

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
        elementaryBreps.Add(Brep.CreateEdgeSurface(edges));
        if (switches[i].nextType != switches[i + 1].prevType)
        {
          string allSwitches = "";
          foreach (SwitchPoint switchPoint in switches)
          {
            allSwitches += switchPoint.prevType + ", " + switchPoint.nextType + ", ";
          }
          faultySegs.Add(medaxCurve);
        }
        elementaryBrepTypes.Add(switches[i].nextType);
      }
    }

    // Now we need to add the surfaces that correspond to branchpoints.
    List<Curve> checkLines = new List<Curve>();
    foreach (Point3d branchPoint in BranchPointList)
    {
      List<Curve> adjMedaxs = new List<Curve>();
      List<double> adjParams = new List<double>();
      foreach (Curve medax in MedialAxisCurveList)
      {
        double closestParam;
        medax.ClosestPoint(branchPoint, out closestParam);
        if (branchPoint.DistanceTo(medax.PointAt(closestParam)) < 0.05)
        {
          adjMedaxs.Add(medax);
          adjParams.Add(closestParam);
        }
      }

      // Generate the chord (on the correct side of the medial axis segment) for each medial axis segment that ends in this branchpoint.
      List<Chord> adjChords = new List<Chord>();
      for (int i = 0; i < adjMedaxs.Count; i++)
      {
        if (Math.Abs(adjMedaxs[i].Domain.Min - adjParams[i]) < Math.Abs(adjMedaxs[i].Domain.Max - adjParams[i]))
        {
          adjChords.Add(GetChordParallel(adjMedaxs[i].PointAt(medax2SwitchPoint[adjMedaxs[i]].First().param), BoundaryCurveList));
        }
        else
        {
          adjChords.Add(GetChordParallel(adjMedaxs[i].PointAt(medax2SwitchPoint[adjMedaxs[i]].Last().param), BoundaryCurveList));
        }
      }

      // Checking.
      List<Curve> currChords = new List<Curve>();
      foreach (Chord chord in adjChords)
      {
        currChords.Add(chord.line);
        checkLines.Add(chord.line);
      }
      elementaryBreps.Add(Brep.CreateEdgeSurface(currChords));
      elementaryBrepTypes.Add(2);
    }
    ElementarySurfacesList = elementaryBreps;
    ElementarySurfaceTypeList = elementaryBrepTypes;
    // CheckLines = checkLines;
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
      Print(prevType + ", " + nextType);
      res[medax].Add(new SwitchPoint(param, prevType, nextType));
    }
    Print("--------------------");
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
      this.parameters = chordParams;
      this.curves = boundaryCurves;
      this.points = chordPoints;
      this.line = new LineCurve(chordPoints.Item1, chordPoints.Item2);
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
  #endregion
}