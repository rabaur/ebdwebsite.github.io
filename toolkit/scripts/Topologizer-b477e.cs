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


/// <summary>
/// This class will be instantiated on demand by the Script component.
/// </summary>
public abstract class Script_Instance_b477e : GH_ScriptInstance
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
  private void RunScript(List<Curve> SegmentCurveList, List<Point3d> BranchPointList, List<Point3d> CornerPointList, double CornerTol, ref object BranchPointDelimitedCurvesList, ref object SplitSegments, ref object crv1, ref object crv2, ref object specialIntersects)
  {
    List<Curve> splitSegments = new List<Curve>();
    foreach (Curve seg in SegmentCurveList)
    {
      bool wasSplit = false;
      foreach (Point3d corner in CornerPointList)
      {
        double closestCornerParam;
        seg.ClosestPoint(corner, out closestCornerParam);
        Interval domain = seg.Domain;
        if (corner.DistanceTo(seg.PointAt(closestCornerParam)) > 3 * CornerTol)
        {
          continue;
        }
        // At this point we assume that only a segment that reaches into a corner can come closes enough to fulfill this condition.
        // There are two cases:
        // 1. Either the closest point to a corner lies somewhere within the segment, in which case we simply split it and choose the longer split segment and discard the other.
        // 2. The closest point is a start or end point of the segment. In this case we need to find the closest branchpoint and split the segment there, again discarding the shorter one.
        if (IsCurveEndPoint(seg, closestCornerParam, RhinoMath.SqrtEpsilon))
        {
          foreach (Point3d branchpoint in BranchPointList)
          {
            double closestBranchpointParam;
            seg.ClosestPoint(branchpoint, out closestBranchpointParam);
            if (seg.PointAt(closestBranchpointParam).DistanceTo(branchpoint) < RhinoMath.SqrtEpsilon)
            {
              if (IsCurveEndPoint(seg, closestBranchpointParam, RhinoMath.SqrtEpsilon))
              {
                // In this case the segment is already well-formed, as the brachpoint lies on the other end of the segment.
                break;
              }

              Curve[] splitAtBranchPoint = seg.Split(closestBranchpointParam);

              // Null check.
              if (splitAtBranchPoint == null)
              {
                throw new Exception("Attempted to split corner-segment at branchpoint: " + closestCornerParam.ToString() + ", " + closestBranchpointParam.ToString());
              }

              // Identify longer segment.
              Curve longerSplit = splitAtBranchPoint[0].GetLength() > splitAtBranchPoint[1].GetLength() ? splitAtBranchPoint[0] : splitAtBranchPoint[1];
              splitSegments.Add(longerSplit);
              wasSplit = true;
              break;
            }
          }
        }
        else
        {
          Curve[] splitAtCorner = seg.Split(closestCornerParam);
          if (splitAtCorner == null)
          {
            throw new Exception("Attempted to split corner-segment at corner.");
          }
          Curve longerSplit = splitAtCorner[0].GetLength() > splitAtCorner[1].GetLength() ? splitAtCorner[0] : splitAtCorner[1];
          splitSegments.Add(longerSplit);
          wasSplit = true;
        }
        break;
      }
      if (!wasSplit)
      {
        splitSegments.Add(seg);
      }
    }

    List<Point3d> specInter = new List<Point3d>();
    for (int i = 0; i < splitSegments.Count; i++)
    {
      Curve seg0 = splitSegments[i];
      for (int j = i + 1; j < splitSegments.Count; j++)
      {
        Curve seg1 = splitSegments[j];

        // Intersection test.
        CurveIntersections intersects = Intersection.CurveCurve(seg0, seg1, RhinoMath.SqrtEpsilon, RhinoMath.SqrtEpsilon);

        // No intersections.
        if (intersects == null)
        {
          continue;
        }
        // Check for overlaps.
        foreach (IntersectionEvent intersect in intersects)
        {
          if (intersect.IsPoint)
          {
            if (!IsCurveEndPoint(seg0, intersect.ParameterA, RhinoMath.SqrtEpsilon))
            {
              Curve[] split0 = seg0.Split(intersect.ParameterA);
              Curve longerSeg0 = split0[0].GetLength() > split0[1].GetLength() ? split0[0] : split0[1];
              splitSegments[i] = longerSeg0;
              seg0 = longerSeg0;
            }
            if (!IsCurveEndPoint(seg1, intersect.ParameterB, RhinoMath.SqrtEpsilon))
            {
              Curve[] split1 = seg1.Split(intersect.ParameterB);
              Curve longerSeg1 = split1[0].GetLength() > split1[1].GetLength() ? split1[0] : split1[1];
              splitSegments[j] = longerSeg1;
              seg1 = longerSeg1;
            }
            continue;
          }

          // Check if the first segment is completely covered by the second one.
          if (intersect.OverlapA.Min <= seg0.Domain.Min + RhinoMath.SqrtEpsilon && seg0.Domain.Max - RhinoMath.SqrtEpsilon <= intersect.OverlapA.Max)
          {
            splitSegments[i] = null;
          }
          else
          {
            // Otherwise just remove the part from the first segment that is overlapping.
            bool minInDomain = IsInCurveDomain(seg0, intersect.OverlapA.Min, RhinoMath.SqrtEpsilon);
            bool maxInDomain = IsInCurveDomain(seg0, intersect.OverlapA.Max, RhinoMath.SqrtEpsilon);

            // Ensure that always exactly one side of the interval is in the domain, otherwise this would not be a valid overlap.
            if (!minInDomain ^ maxInDomain)
            {
              crv1 = seg0;
              crv2 = seg1;
              if (intersect.OverlapA.Max - intersect.OverlapA.Min < RhinoMath.SqrtEpsilon || intersect.OverlapB.Max - intersect.OverlapB.Min < RhinoMath.SqrtEpsilon)
              {
                // This should actually be a point-intersection, but for some reason, Rhino fucked up.
                if (!IsCurveEndPoint(seg0, intersect.ParameterA, RhinoMath.SqrtEpsilon))
                {
                  // Curves intersect at endpoints, so we can ignore that case.
                  Curve[] split0 = seg0.Split(intersect.ParameterA);
                  Curve longerSeg0 = split0[0].GetLength() > split0[1].GetLength() ? split0[0] : split0[1];
                  splitSegments[i] = longerSeg0;
                }
                if (!IsCurveEndPoint(seg1, intersect.ParameterB, RhinoMath.SqrtEpsilon))
                {
                  Curve[] split1 = seg1.Split(intersect.ParameterB);
                  Curve longerSeg1 = split1[0].GetLength() > split1[1].GetLength() ? split1[0] : split1[1];
                  splitSegments[j] = longerSeg1;
                }
                continue;
              }
              // throw new Exception(intersect.OverlapB + "Not a valid overlap: " + intersect.OverlapA.Min + ", " + intersect.OverlapA.Max);
            }

            Print(intersect.OverlapB.ToString());
            Curve segmentToSurvive;
            if (minInDomain)
            {
              segmentToSurvive = seg0.Split(intersect.OverlapA.Min)[1];
            }
            else
            {
              segmentToSurvive = seg1.Split(intersect.OverlapA.Max)[0];
            }
            splitSegments[i] = segmentToSurvive;
          }
        }
      }
    }
    specialIntersects = specInter;
    SplitSegments = splitSegments;

    // Building segment graph where segments are nodes and there is and edge between segments iff they intersect at a point which is not a branchpoint.
    Dictionary<Curve, List<Curve>> segmentGraph = new Dictionary<Curve, List<Curve>>();
    for (int i = 0; i < splitSegments.Count; i++)
    {
      Curve seg0 = splitSegments[i];
      segmentGraph[seg0] = new List<Curve> { seg0 };
      for (int j = 0; j < splitSegments.Count; j++)
      {
        if (i == j)
        {
          continue;
        }
        Curve seg1 = splitSegments[j];
        CurveIntersections intersects = Intersection.CurveCurve(seg0, seg1, RhinoMath.SqrtEpsilon, RhinoMath.SqrtEpsilon);

        // No intersections between curves.
        if (intersects == null)
        {
          continue;
        }

        // Go through each intersect and check whether curves intersect at point which is not branchpoint. In that case, we create an edge.
        foreach (IntersectionEvent intersect in intersects)
        {
          Point3d intersectionPoint = intersect.PointA;
          if (!ContainsPointParallel(intersectionPoint, BranchPointList, 0.1))
          {
            segmentGraph[seg0].Add(seg1);
          }
        }
      }
    }

    // Run BFS for each start node to find connected component.
    List<Curve> joinedSegments = new List<Curve>();
    Dictionary<Curve, bool> visited = new Dictionary<Curve, bool>(); // Tracks whether a curve was already assigned to a connected component.
    foreach (KeyValuePair<Curve, List<Curve>> keyVal in segmentGraph)
    {

      visited[keyVal.Key] = false;
    }
    foreach (KeyValuePair<Curve, List<Curve>> keyValue in segmentGraph)
    {
      Curve initSeg = keyValue.Key;
      List<Curve> neighbors = keyValue.Value;
      if (visited[initSeg])
      {
        continue;
      }

      if (neighbors.Count == 1)
      {
        joinedSegments.Add(initSeg); // If there is only one neighbor, then it is the curve itself and we can join it without any additional joining.
        continue;
      }

      // The segment has more than one neighbor, so we run BFS to find connected component.
      List<Curve> connectedComponent = new List<Curve>();
      Queue<Curve> queue = new Queue<Curve>();
      queue.Enqueue(initSeg);
      while (queue.Count != 0)
      {
        Curve currSeg = queue.Dequeue();
        if (visited[currSeg])
        {
          continue;
        }
        connectedComponent.Add(currSeg);
        visited[currSeg] = true;
        foreach (Curve neighbor in segmentGraph[currSeg])
        {
          queue.Enqueue(neighbor);
        }
      }
      Curve[] joinedConnectedComponent = Curve.JoinCurves(connectedComponent, 1.0);
      if (joinedConnectedComponent.Length > 1)
      {
        CurveIntersections intersects = Intersection.CurveCurve(joinedConnectedComponent[0], joinedConnectedComponent[1], RhinoMath.SqrtEpsilon, RhinoMath.SqrtEpsilon);
        Print(intersects.Count.ToString());
        Print(intersects[0].IsOverlap.ToString());
        Print(intersects[0].OverlapA.ToString());
        crv1 = joinedConnectedComponent[0];
        crv2 = joinedConnectedComponent[1];
      }
      joinedSegments.Add(joinedConnectedComponent[0]);
    }
    BranchPointDelimitedCurvesList = joinedSegments;

  }
  #endregion
  #region Additional
  private bool IsCurveEndPoint(Curve curve, double param, double tol)
  {
    Interval curveDomain = curve.Domain;
    return param <= curve.Domain.Min + tol || curveDomain.Max - tol <= param;
  }

  private bool IsInCurveDomain(Curve curve, double param, double tol)
  {
    Interval curveDomain = curve.Domain;
    return curveDomain.Min + tol <= param && param <= curveDomain.Max - tol;
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