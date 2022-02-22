using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using Rhino.Geometry.Intersect;
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
  private void RunScript(List<Curve> MedialAxisCurveList, double InitialStepSize, List<Curve> BoundaryCurveList, List<Point3d> CornerPointList, List<Point3d> BranchPointList, int idx, double t, ref object ClassifiedPointList, ref object TypeList, ref object initialEvaluationPointsOut, ref object SwitchPointLocations, ref object SwitchPointTypes, ref object Chords, ref object BranchPointBoundaries, ref object chord1, ref object chord2, ref object qPoint, ref object radius1, ref object radius2, ref object SwitchPointChords, ref object crv1, ref object crv2, ref object ElementarySurfacesList, ref object ElementarySurfaceTypeList, ref object CheckLines, ref object FaultyCurves, ref object segsToSurf, ref object typesToSurf, ref object line0ToSurf, ref object line1ToSurf)
  {
    Curve selectedCurve = MedialAxisCurveList[idx];
    qPoint = selectedCurve.PointAt(t);
    Chord testChord = GetChordParallel(selectedCurve.PointAt(t), BoundaryCurveList);
    chord1 = testChord.points.Item1;
    chord2 = testChord.points.Item2;
    crv1 = testChord.curves.Item1;
    crv2 = testChord.curves.Item2;
    radius1 = selectedCurve.PointAt(t).DistanceTo(testChord.points.Item1);
    radius2 = selectedCurve.PointAt(t).DistanceTo(testChord.points.Item2);

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

      // Find first point that is not classified as full-ligature. Everything before that point will be assumend to be a full-ligature.
      int startIdx = 0;
      while (startIdx < currTypes.Count && currTypes[startIdx] == 2)
      {
        startIdx++;
      }

      if (startIdx == currTypes.Count)
      {
        // This segment only consists of full ligatures, so we can set the startIdx back to 0.
        startIdx = 0;
      }

      double lastFullLigParam = startIdx > 1 ? currParams[startIdx - 1] : 0.0;

      // Find first switch-point. Work from inside to outside.
      double startSwitchParam = 0.0;
      if (startIdx == 0)
      {
        Chord initChord = GetChordParallel(medialAxisCurve.PointAt(currParams[startIdx]), BoundaryCurveList);
        startSwitchParam = FindStartSwitchPoint(medialAxisCurve, CornerPointList, BoundaryCurveList, lastFullLigParam, currParams[startIdx], currTypes[startIdx], initChord);
      }
      else
      {
        startSwitchParam = FindSwitchPoint(medialAxisCurve, CornerPointList, BoundaryCurveList, lastFullLigParam, currParams[startIdx], 2);
      }
      medialAxisCurve2SwitchPointList[medialAxisCurve].Add(new SwitchPoint(startSwitchParam, 2, currTypes[startIdx]));

      // Now we will construct all switch points:
      int currType = currTypes[startIdx];
      double lastParam = currParams[startIdx];
      int endIdx = currTypes.Count - 1;
      while (endIdx >= 0 && currTypes[endIdx] == 2)
      {
        endIdx--;
      }

      if (endIdx == -1)
      {
        // We went all the way to the beginning. We can set the index to the last element.
        endIdx = currTypes.Count - 1;
      }
      for (int i = startIdx + 1; i < endIdx + 1; i++)
      {
        // If the types differ, there must lie a switchpoint between these two classified points.
        if (currTypes[i] != currType)
        {
          double switchParam = FindSwitchPoint(medialAxisCurve, CornerPointList, BoundaryCurveList, lastParam, currParams[i], currType);
          medialAxisCurve2SwitchPointList[medialAxisCurve].Add(new SwitchPoint(switchParam, currType, currTypes[i]));
          currType = currTypes[i];
          lastParam = currParams[i];
        }
      }

      double firstFullLigParam = endIdx < currTypes.Count - 1 ? currParams[endIdx + 1] : 1.0;

      // Find last switch-point. Work from inside to outside.
      double endSwitchParam = 0.0;
      if (endIdx == currTypes.Count - 1)
      {
        Chord lastChord = GetChordParallel(medialAxisCurve.PointAt(currParams[endIdx]), BoundaryCurveList);
        endSwitchParam = FindEndSwitchPoint(medialAxisCurve, CornerPointList, BoundaryCurveList, currParams[endIdx], firstFullLigParam, currTypes[endIdx], lastChord, 0, selectedCurve);
      }
      else
      {
        // In this case we are far enough inside the curve (for reasonable initial sampling resolutions) s.t. we do not need to use the special treatment of the last case.
        endSwitchParam = FindSwitchPoint(medialAxisCurve, CornerPointList, BoundaryCurveList, currParams[endIdx], firstFullLigParam, currTypes[endIdx]);
      }
      medialAxisCurve2SwitchPointList[medialAxisCurve].Add(new SwitchPoint(endSwitchParam, currTypes[endIdx], 2));
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
        Chord chord = GetChordParallel(keyVal.Key.PointAt(switchPoint.param), BoundaryCurveList);
        chords.Add(new Line(chord.points.Item1, chord.points.Item2));
      }
    }
    SwitchPointChords = chords;
    SwitchPointLocations = switchPoints;

    // Create surfaces.
    List<Brep> elementaryBreps = new List<Brep>();
    List<int> elementaryBrepTypes = new List<int>();
    List<Curve> faultySegs = new List<Curve>();
    List<Curve> segmentForEachSurf = new List<Curve>();
    List<string> typesForEachSurf = new List<string>();
    List<Curve> line0ForEachSurf = new List<Curve>();
    List<Curve> line1ForEachSurf = new List<Curve>();
    foreach (KeyValuePair<Curve, List<SwitchPoint>> keyVal in medialAxisCurve2SwitchPointList)
    {
      Curve medaxCurve = keyVal.Key;
      List<SwitchPoint> switches = keyVal.Value;
      if (switches[0].prevType != 2)
      {
        throw new Exception("First ligature type was not 2");
      }
      if (switches[switches.Count - 1].nextType != 2)
      {
        throw new Exception("Last ligature type was not 2");
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
        typesForEachSurf.Add(switches[i].prevType + ", " + switches[i].nextType + ", " + switches[i + 1].prevType + ", " + switches[i + 1].nextType );
        if (currTrim != null)
        {
          if (currTrim.GetLength() < 0.1 && switches[i].nextType == 1 && ContainsPointParallel(currTrim.PointAtStart, BranchPointList, 0.1))
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
              // throw new Exception("Connecting lines between chords cross both ways");
            }

            edges.Add(diffA);
            edges.Add(diffB);
          }
        }
        elementaryBreps.Add(Brep.CreateEdgeSurface(edges));
        if (switches[i].nextType != switches[i+1].prevType)
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
    FaultyCurves = faultySegs;
    segsToSurf = segmentForEachSurf;
    typesToSurf = typesForEachSurf;
    line0ToSurf = line0ForEachSurf;
    line1ToSurf = line1ForEachSurf;

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
          adjChords.Add(GetChordParallel(adjMedaxs[i].PointAt(medialAxisCurve2SwitchPointList[adjMedaxs[i]].First().param), BoundaryCurveList));
        }
        else
        {
          adjChords.Add(GetChordParallel(adjMedaxs[i].PointAt(medialAxisCurve2SwitchPointList[adjMedaxs[i]].Last().param), BoundaryCurveList));
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

  private int ClassifyMedialAxisPoint(Point3d medialAxisPoint, List<Point3d> CornerPointList, List<Curve> BoundaryCurveList)
  {
    // Find the two closest points on the boundary. The connecting line is called "chord", thus chordEnds.
    Chord chord = GetChordParallel(medialAxisPoint, BoundaryCurveList);

    // There are three types of medial-axis points:
    // 1. Chord-ends are both on a normal boundary segment, so not on a corner.
    // 2. Just one chord-end is a corner of the boundary, the other one is a normal segment.
    // 3. Both chord-ends are corners.
    bool isCorner0 = ContainsPointParallel(chord.points.Item1, CornerPointList, RhinoMath.SqrtEpsilon);
    bool isCorner1 = ContainsPointParallel(chord.points.Item2, CornerPointList, RhinoMath.SqrtEpsilon);
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

  private double FindSwitchPoint(Curve medialAxisCurve, List<Point3d> cornerPointList, List<Curve> boundaryCurveList, double prevParam, double nextParam, int prevType)
  {
    // We test in the middle of the interval delimited by lowerParam and higherParam.
    double queryParam = 0.5 * (prevParam + nextParam);

    // We have reached reasonable accuracy.
    if (Math.Abs(queryParam - prevParam) <= RhinoMath.SqrtEpsilon)
    {
      return prevParam;
    }
    else
    {
      // Classify the query-point.
      int queryType = ClassifyMedialAxisPoint(medialAxisCurve.PointAt(queryParam), cornerPointList, boundaryCurveList);

      // If the queryType is different than the type associated with the lower interval parameter, then we have overshot the type-switchpoint and we
      // must sample in the interval [lowerParam, queryParam].
      if (queryType != prevType)
      {
        return FindSwitchPoint(medialAxisCurve, cornerPointList, boundaryCurveList, prevParam, queryParam, prevType);
      }

      // If the queryType is equal to the lowerType, we sample in the upper interval.
      else
      {
        return FindSwitchPoint(medialAxisCurve, cornerPointList, boundaryCurveList, queryParam, nextParam, prevType);
      }
    }
  }

  private double FindStartSwitchPoint(
    Curve medialAxisCurve, 
    List<Point3d> cornerPointList, 
    List<Curve> boundaryCurveList, 
    double prevParam, 
    double nextParam, 
    int nextType, 
    Chord oldChord)
  {
    // We test in the middle of the interval delimited by lowerParam and higherParam.
    double queryParam = 0.5 * (prevParam + nextParam);

    // We have reached reasonable accuracy.
    if (Math.Abs(nextParam - queryParam) <= RhinoMath.SqrtEpsilon)
    {
      return nextParam;
    }
    else
    {
      Point3d queryPoint = medialAxisCurve.PointAt(queryParam);
      Chord chord = GetChordParallel(queryPoint, boundaryCurveList);
      LineCurve chordLine = chord.line;

      // Condition 1: Medial axis crosses the segment in question.
      bool isIntersecting = Intersection.CurveCurve(medialAxisCurve, chordLine, RhinoMath.SqrtEpsilon, RhinoMath.SqrtEpsilon).Count > 0;

      // Condition 2: Types are the same.
      int currType = ClassifyMedialAxisPoint(queryPoint, cornerPointList, boundaryCurveList);
      bool typesAreSame = currType == nextType;

      // Condition 3: The closest boundary segments are the same.
      Curve old1 = oldChord.curves.Item1;
      Curve old2 = oldChord.curves.Item2;
      Curve new1 = chord.curves.Item1;
      Curve new2 = chord.curves.Item2;
      bool sameBoundary = old1 == new1 && old2 == new2 || old1 == new2 && old2 == new1;

      // If any of the conditions is not fulfilled, we are too close to the branchpoints and need to sample in the halfinterval which is farther away from branchpoint.
      if (!isIntersecting || !typesAreSame || !sameBoundary)
      {
        // We are too early on the line and need to subsample in the upper halfinterval.
        return FindStartSwitchPoint(medialAxisCurve, cornerPointList, boundaryCurveList, queryParam, nextParam, nextType, oldChord);
      }
      else
      {
        // The chord is still crossing the medial axis, so we can sample the lower halfinterval.
        return FindStartSwitchPoint(medialAxisCurve, cornerPointList, boundaryCurveList, prevParam, queryParam, nextType, chord);
      }
    }
  }
  private double FindEndSwitchPoint(
    Curve medialAxisCurve,
    List<Point3d> cornerPointList,
    List<Curve> boundaryCurveList,
    double prevParam,
    double nextParam,
    int nextType,
    Chord oldChord,
    int depth,
    Curve selectedCurve)
  {
    if (depth == 0)
    {
      Point3d queryPoint = medialAxisCurve.PointAt(prevParam);
      Chord chord = GetChordParallel(queryPoint, boundaryCurveList);
      CheckConditions(medialAxisCurve, queryPoint, chord, boundaryCurveList, cornerPointList, oldChord, nextType, selectedCurve);
    }

    // We test in the middle of the interval delimited by lowerParam and higherParam.
    double queryParam = 0.5 * (prevParam + nextParam);

    // We have reached reasonable accurary.
    if (Math.Abs(queryParam - prevParam) <= RhinoMath.SqrtEpsilon)
    {
      Point3d queryPoint = medialAxisCurve.PointAt(prevParam);
      Chord chord = GetChordParallel(queryPoint, boundaryCurveList);
      CheckConditions(medialAxisCurve, queryPoint, chord, boundaryCurveList, cornerPointList, oldChord, nextType, selectedCurve);
      return prevParam;
    }
    else
    {
      Point3d queryPoint = medialAxisCurve.PointAt(queryParam);
      Chord chord = GetChordParallel(queryPoint, boundaryCurveList);
      if (!CheckConditions(medialAxisCurve, queryPoint, chord, boundaryCurveList, cornerPointList, oldChord, nextType, selectedCurve))
      {
        return FindEndSwitchPoint(medialAxisCurve, cornerPointList, boundaryCurveList, prevParam, queryParam, nextType, oldChord, depth + 1, selectedCurve);
      }
      else
      {
        return FindEndSwitchPoint(medialAxisCurve, cornerPointList, boundaryCurveList, queryParam, nextParam, nextType, chord, depth + 1, selectedCurve);
      }
    }
  }

  private bool CheckConditions(Curve medialAxisCurve, Point3d queryPoint, Chord newChord, List<Curve> boundaryCurveList, List<Point3d> cornerPointList, Chord oldChord, int nextType, Curve selectedCurve)
  {
    LineCurve chordLine = newChord.line;

    // Condition 1: Medial axis crosses the segment in question.
    bool isIntersecting = Intersection.CurveCurve(medialAxisCurve, chordLine, RhinoMath.SqrtEpsilon, RhinoMath.SqrtEpsilon).Count > 0;

    // Condition 2: Types are the same.
    int currType = ClassifyMedialAxisPoint(queryPoint, cornerPointList, boundaryCurveList);
    bool typesAreSame = currType == nextType;

    // Condition 3: The closest boundary segments are the same.
    Curve old1 = oldChord.curves.Item1;
    Curve old2 = oldChord.curves.Item2;
    Curve new1 = newChord.curves.Item1;
    Curve new2 = newChord.curves.Item2;
    bool sameBoundary = old1 == new1 && old2 == new2 || old1 == new2 && old2 == new1;
    return isIntersecting && typesAreSame && sameBoundary;
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
    while (idx < closestPoints.Length && c0.DistanceTo(closestPoints[idx].point) < 0.1)
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