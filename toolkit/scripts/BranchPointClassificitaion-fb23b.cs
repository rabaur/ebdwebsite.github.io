using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using System.Diagnostics;
using Rhino.Geometry.Intersect;


/// <summary>
/// This class will be instantiated on demand by the Script component.
/// </summary>
public abstract class Script_Instance_fb23b : GH_ScriptInstance
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
  private void RunScript(List<Curve> SegmentCurveList, double LowerTolerance, double UpperTolerance, ref object BranchPointList, ref object BranchPointDelimitedCurveList, ref object NewBranchPoints)
  {
    List<List<Curve>> contiguousSegmentList = PartitionSegmentsByAdjacencyParallel(SegmentCurveList); // Empirically, parallel method was substantially faster.

    // If a branchpoint is exactly at the start/end of a contiguous segment, we can only detect it by comparing to other starts or end of segments.
    List<Point3d> branchPoints = new List<Point3d>();
    List<Point3d> newBranchPoints = new List<Point3d>();
    for (int i = 0; i < contiguousSegmentList.Count; i++)
    {
      List<Curve> contiguous0 = contiguousSegmentList[i];
      Curve start0 = contiguous0[0];
      Curve end0 = contiguous0[contiguous0.Count - 1];
      for (int j = i + 1; j < contiguousSegmentList.Count; j++)
      {
        List<Curve> contiguous1 = contiguousSegmentList[j];
        Curve start1 = contiguous1[0];
        Curve end1 = contiguous1[contiguous1.Count - 1];
        CurveIntersections intersects0 = Intersection.CurveCurve(start0, start1, 0.1, 0.1);
        CurveIntersections intersects1 = Intersection.CurveCurve(start0, end1, 0.1, 0.1);
        CurveIntersections intersects2 = Intersection.CurveCurve(end0, start1, 0.1, 0.1);
        CurveIntersections intersects3 = Intersection.CurveCurve(end0, end1, 0.1, 0.1);
        if (intersects0.Count != 0)
        {
          double[] angles = CalculateAngles(new List<Curve>() { start0, start1 });
          double diff = Math.Abs(angles[0]);
          if (LowerTolerance <= diff && diff <= UpperTolerance)
          {
            branchPoints.Add(intersects0[0].PointA);
            newBranchPoints.Add(intersects0[0].PointA);
          }
        }
        else if (intersects1.Count != 0)
        {
          double[] angles = CalculateAngles(new List<Curve>() { start0, end1 });
          double diff = Math.Abs(angles[0]);
          if (LowerTolerance <= diff && diff <= UpperTolerance)
          {
            branchPoints.Add(intersects1[0].PointA);
            newBranchPoints.Add(intersects1[0].PointA);
          }
        }
        else if (intersects2.Count != 0)
        {
          double[] angles = CalculateAngles(new List<Curve>() { end0, start1 });
          double diff = Math.Abs(angles[0]);
          if (LowerTolerance <= diff && diff <= UpperTolerance)
          {
            branchPoints.Add(intersects2[0].PointA);
            newBranchPoints.Add(intersects2[0].PointA);
          }
        }
        else if (intersects3.Count != 0)
        {
          double[] angles = CalculateAngles(new List<Curve>() { end0, end1 });
          double diff = Math.Abs(angles[0]);
          if (LowerTolerance <= diff && diff <= UpperTolerance)
          {
            branchPoints.Add(intersects3[0].PointA);
            newBranchPoints.Add(intersects3[0].PointA);
          }
        }
      }
    }
    NewBranchPoints = newBranchPoints;

    foreach (List<Curve> contiguousSegments in contiguousSegmentList)
    {
      double[] angles = CalculateAngles(contiguousSegments);
      for (int i = 0; i < angles.Length; i++)
      {
        double currAngle = angles[i];
        bool branchPointDetected = ContainsPointParallel(contiguousSegments[i].PointAtEnd, branchPoints, RhinoMath.SqrtEpsilon);
        if (LowerTolerance <= currAngle && currAngle <= UpperTolerance || branchPointDetected)
        {
          if (!branchPointDetected)
          {
            branchPoints.Add(contiguousSegments[i].PointAtEnd);
          }
        }
      }
    }

    // Find branchpoints for each list of contiguous segments.
    List<Curve> branchPointDelimitedSegments = new List<Curve>();
    foreach (List<Curve> contiguousSegments in contiguousSegmentList)
    {
      List<Curve> currSegs = new List<Curve>();
      for (int i = 0; i < contiguousSegments.Count; i++)
      {
        currSegs.Add(contiguousSegments[i]);
        bool branchPointDetected = ContainsPointParallel(contiguousSegments[i].PointAtEnd, branchPoints, RhinoMath.SqrtEpsilon);
        if (branchPointDetected)
        {
          Curve[] joinedCurves = Curve.JoinCurves(currSegs, RhinoMath.SqrtEpsilon);
          if (joinedCurves.Length == 0)
          {
            throw new Exception("Joining curves was not successful.");
          }
          branchPointDelimitedSegments.AddRange(joinedCurves);
          currSegs = new List<Curve>();
        }
      }

      // Join the remaining unjoined segments.
      Curve[] lastJoinedCurves = Curve.JoinCurves(currSegs);
      if (lastJoinedCurves.Length == 0)
      {
        // throw new Exception("Joining curves at end was not successful.");
      }

      branchPointDelimitedSegments.AddRange(lastJoinedCurves);
    }
    BranchPointDelimitedCurveList = branchPointDelimitedSegments;
    BranchPointList = branchPoints;
  }
  #endregion
  #region Additional
  private List<List<Curve>> PartitionSegmentsByAdjacencySequential(List<Curve> segmentCurveList)
  {
    // The medial axis comes in form of many line segments, which are not necessarily all consecutive. First, partition consecutive segments into sublists.
    List<List<Curve>> contiguousSegments = new List<List<Curve>>();
    List<Curve> contiguousSegment = new List<Curve>();
    for (int i = 0; i < segmentCurveList.Count - 1; i++)
    {
      // Always add the current segment.
      Curve currSeg = segmentCurveList[i];
      contiguousSegment.Add(currSeg);

      // Check for an intersect (then the segments are contiguous).
      Curve nextSeg = segmentCurveList[i + 1];
      CurveIntersections intersects = Intersection.CurveCurve(currSeg, nextSeg, RhinoMath.SqrtEpsilon, RhinoMath.SqrtEpsilon);
      if (intersects == null)
      {
        continue;
      }

      // No intersection, therefore the segments are not contiguous.
      if (intersects.Count == 0)
      {
        contiguousSegments.Add(contiguousSegment);
        contiguousSegment = new List<Curve>();
      }
    }
    contiguousSegment.Add(segmentCurveList[segmentCurveList.Count - 1]);
    contiguousSegments.Add(contiguousSegment);
    return contiguousSegments;
  }

  private List<List<Curve>> PartitionSegmentsByAdjacencyParallel(List<Curve> segmentCurveList)
  {
    // Duplicate lists to avoid read/write conflicts.
    List<Curve> duplicateSegmentCurveList = new List<Curve>(segmentCurveList); // The copying might be more costly than the sequential check.
    bool[] notAdjacent = new bool[segmentCurveList.Count - 1]; // Entry is true whenever the consecutive segment is not adjacent.
    System.Threading.Tasks.Parallel.For(0, notAdjacent.Length, i =>
      {
      CurveIntersections intersects = Intersection.CurveCurve(segmentCurveList[i], duplicateSegmentCurveList[i + 1], RhinoMath.SqrtEpsilon, RhinoMath.SqrtEpsilon);
      if (intersects != null)
      {
        if (intersects.Count == 0)
        {
          notAdjacent[i] = true;
        }
      }
      });

    List<List<Curve>> contiguousSegmentLists = new List<List<Curve>>();
    contiguousSegmentLists = new List<List<Curve>>();
    List<Curve> contiguousSegments = new List<Curve>();
    for (int i = 0; i < notAdjacent.Length; i++)
    {
      contiguousSegments.Add(segmentCurveList[i]);
      if (notAdjacent[i])
      {
        contiguousSegmentLists.Add(contiguousSegments);
        contiguousSegments = new List<Curve>();
      }
    }
    contiguousSegments.Add(segmentCurveList[segmentCurveList.Count - 1]);
    contiguousSegmentLists.Add(contiguousSegments);
    return contiguousSegmentLists;
  }

  private double[] CalculateAngles(List<Curve> segments)
  {
    Vector3d[] vectors = new Vector3d[segments.Count];
    for (int i = 0; i < segments.Count; i++)
    {
      Curve currSeg = segments[i];
      vectors[i] = new Vector3d(currSeg.PointAtEnd - currSeg.PointAtStart);
    }
    double[] angles = new double[segments.Count - 1];
    for (int i = 0; i < segments.Count - 1; i++)
    {
      angles[i] = RhinoMath.ToDegrees(Vector3d.VectorAngle(vectors[i], vectors[i + 1]));
    }
    return angles;
  }

  private double[] FiniteDifferences(double[] values)
  {
    double[] diffs = new double[values.Length - 1];
    for (int i = 0; i < diffs.Length; i++)
    {
      diffs[i] = values[i + 1] - values[i];
    }
    return diffs;
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