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
public abstract class Script_Instance_cc138 : GH_ScriptInstance
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
  private void RunScript(List<Curve> medialAxisCurveList, ref object cleanedUpMedialAxisCurveList, ref object AllJoined)
  {
    // Sort all curves by distance.
    List<Curve> orderedMedialAxisCurveList = medialAxisCurveList.OrderByDescending(curve => curve.GetLength()).ToList();

    // Create mask where each element indicates whether a curve is covered.
    bool[] covered = new bool[medialAxisCurveList.Count];

    // Check for all curves if they are covered.
    for (int i = 0; i < orderedMedialAxisCurveList.Count; i++)
    {
      // Do not check if a curve covers other curves if it is already covered (because the curves covered by this curve are covered by transitivity).
      if (covered[i])
      {
        continue;
      }
      for (int j = i + 1; j < orderedMedialAxisCurveList.Count; j++)
      {
        Curve curve1 = orderedMedialAxisCurveList[i];
        Curve curve2 = orderedMedialAxisCurveList[j];
        if (CurveCoversCurve(curve1, curve2))
        {
          covered[j] = true;
        }
      }
    }

    // Add all non-covered curves.
    List<Curve> notCovered = new List<Curve>();
    for (int i = 0; i < orderedMedialAxisCurveList.Count; i++)
    {
      if (!covered[i])
      {
        notCovered.Add(orderedMedialAxisCurveList[i]);
      }
    }

    Print((orderedMedialAxisCurveList.Count - notCovered.Count).ToString() + " curves were covered.");

    
    // Build a graph based on remaining overlaps.
    List<List<int>> adjacencyList = new List<List<int>>();
    for (int i = 0; i < notCovered.Count; i++)
    {
      adjacencyList.Add(new List<int>());
    }
    for (int i = 0; i < notCovered.Count; i++)
    {
      Curve curveA = notCovered[i];
      for (int j = 0; j < notCovered.Count; j++)
      {
        if (i == j)
        {
          continue;
        }
        Curve curveB = notCovered[j];
        if (AreCurvesMutuallyIntersectingByOverlap(curveA, curveB))
        {
          adjacencyList[i].Add(j);
        }
      }
    }

    // Used to track across BFS runs which nodes where visited. A curve does only need to be joined once, we do not need all different variations.
    List<int> globalVisited = new List<int>();

    // All indices in a sublist correspond to curves that will be joined.
    List<List<int>> connectedComponents = new List<List<int>>();

    // Start a BFS at each curve.
    for (int i = 0; i < notCovered.Count; i++)
    {
      List<int> visited = new List<int>();
      List<int> queue = new List<int> { i };
      while (queue.Count > 0)
      {
        // Pop first element.
        int curr = queue[0];
        queue.RemoveAt(0);

        // Check if this node has already been visited.
        if (globalVisited.Contains(curr))
        {
          continue;
        }

        // Remember that we have visited this node.
        globalVisited.Add(curr);
        visited.Add(curr);

        // Add neighbors to queue.
        foreach (int neighbor in adjacencyList[curr])
        {
          List<Rhino.Geometry.Intersect.IntersectionEvent> overlaps = AllOverlaps(notCovered[curr], notCovered[neighbor]);
          if (overlaps.Count == 0)
          {
            throw new Exception("Nodes are neighboring but curves don't overlap.");
          }

          // Do not add if has already been visited.
          if (globalVisited.Contains(neighbor))
          {
            continue;
          }

          queue.Add(neighbor);
        }
      }
      connectedComponents.Add(visited);
    }

    // Join curves for each connected component that has a size larger than 1.
    List<Curve> joinedCurves = new List<Curve>();

    // Remember curves that were removed in the process.
    List<int> killedCurves = new List<int>();
    foreach (List<int> connectedComponent in connectedComponents)
    {
      if (connectedComponent.Count < 2)
      {
        continue;
      }

      // Find the corresponding curves.
      List<Curve> toJoin = new List<Curve>();
      foreach (int index in connectedComponent)
      {
        killedCurves.Add(index);
        toJoin.Add(notCovered[index]);
      }

      // Join curves.
      joinedCurves.AddRange(JoinOverlappingCurves(toJoin));
    }
    Print("Has joined " + joinedCurves.Count.ToString() + " curves");

    // Remove overlapping curves and append joined ones.
    List<Curve> finalCurves = new List<Curve>();
    for (int i = 0; i < notCovered.Count; i++)
    {
      if (!killedCurves.Contains(i))
      {
        finalCurves.Add(notCovered[i]);
      }
    }
    finalCurves.AddRange(joinedCurves);
    Print("All curves are non-overlapping: " + AreAllCurvesNonOverlapping(finalCurves));
    Print(AreAllCurvesNonOverlapping(notCovered).ToString());
    cleanedUpMedialAxisCurveList = finalCurves;
  }
  #endregion
  #region Additional

  const double DIST_TOL = RhinoMath.DefaultDistanceToleranceMillimeters;

  /// <summary>
  /// Checks wether two curves are intersecting by overlap.
  /// </summary>
  /// <remarks>I have implemented this function since it seems that Intersection.CurveCurve is not symmetric.</remarks>
  /// <param name="curveA">First curve to check.</param>
  /// <param name="curveB">Second curve to check.</param>
  /// <returns><see langword="true"> if curves are truly overlapping, <see langword="false"> otherwise.</returns>
  private bool AreCurvesMutuallyIntersectingByOverlap(Curve curveA, Curve curveB)
  {
    var overlapsAB = AllOverlaps(curveA, curveB);
    var overlapsBA = AllOverlaps(curveB, curveA);
    return overlapsAB.Count != 0 && overlapsBA.Count != 0;
  }

  /// <summary>
  /// Splits a curve into segments delimited by the boundaries of the intervals in overlaps.
  /// </summary>
  /// <remarks>It is assumed that <paramref name="curve"/> corresponds to the first curve in the intersection-event of <paramref name="overlaps"/>.</remarks>
  /// <param name="curve">The curve to be split.</param>
  /// <param name="overlaps">The overlap events.</param>
  /// <returns>The split curve.</returns>
  private Curve[] SplitCurveFromOverlaps(Curve curve, List<Rhino.Geometry.Intersect.IntersectionEvent> overlaps)
  {
    List<double> parameters = new List<double>();
    foreach (Rhino.Geometry.Intersect.IntersectionEvent overlap in overlaps)
    {
      Rhino.Geometry.Interval interval = overlap.OverlapA;
      parameters.Add(interval.Min);
      parameters.Add(interval.Max);
    }
    parameters.Sort();
    return curve.Split(parameters);
  }

  /// <summary>
  /// Checks whether <paramref name="curveA"/> covers <paramref name="curveB"/>.
  /// </summary>
  /// <remarks>A curve A covers a curve B if there is an intersection and the intersection-domain is equal to the full domain of curve B.</remarks>
  /// <param name="curveA">Check for this curve if it is covering <paramref name="curveB"/>.</param>
  /// <param name="curveB">Check for this curve if it is covered by <paramref name="curveA"/>.</param>
  /// <returns><see langword="true"/> if <paramref name="curveA"></paramref> covers <paramref name="curveB"/>, else <see langword="false"/>.</returns>
  private bool CurveCoversCurve(Curve curveA, Curve curveB)
  {
    // Attempt to intersect the curves.
    Rhino.Geometry.Intersect.CurveIntersections intersections = Rhino.Geometry.Intersect.Intersection.CurveCurve(curveA, curveB, DIST_TOL, DIST_TOL);

    // If there is not intersection-event, curve1 does not cover curve2.
    if (intersections.Count == 0)
    {
      return false;
    }

    // Check for each intersection-event if it is an overlap.
    foreach (Rhino.Geometry.Intersect.IntersectionEvent intersection in intersections)
    {
      if (!intersection.IsOverlap)
      {
        continue;
      }

      // If the intersection-domain is equivalent to the whole domain of the curve2, then curve2 must be completely covered by curve1.
      if (CompareInterval(intersection.OverlapB, curveB.Domain))
      {
        return true;
      }
    }
    return false;
  }

  /// <summary>
  /// Compares intervals disregarding whether they are ascending or descending.
  /// </summary>
  /// <param name="intervalA">The first interval to compare.</param>
  /// <param name="intervalB">The second interval to compare.</param>
  /// <returns><see langword="true"/> if intervals are equal, <see langword="false"/> otherwise.</returns>
  private bool CompareInterval(Interval intervalA, Interval intervalB)
  {
    return intervalA.Min == intervalB.Min && intervalA.Max == intervalB.Max;
  }

  /// <summary>
  /// Joins partially overlapping curves.
  /// </summary>
  /// <param name="curves">The curves to join.</param>
  /// <returns>The joined curves,</returns>
  private Curve[] JoinOverlappingCurves(List<Curve> curves)
  {
    // Split curves at each position into overlapping and non-overlapping intervals.
    List<Curve> segments = new List<Curve>();

    for (int i = 0; i < curves.Count; i++)
    {
      List<Rhino.Geometry.Intersect.IntersectionEvent> allOverlaps = new List<Rhino.Geometry.Intersect.IntersectionEvent>();
      Curve curveA = curves[i];
      for (int j = 0; j < curves.Count; j++)
      {
        if (i == j)
        {
          continue;
        }
        Curve curveB = curves[j];
        allOverlaps.AddRange(AllOverlaps(curveA, curveB));
      }
      if (allOverlaps.Count == 0)
      {
        throw new Exception("In JoinOverlappingCurves: Curve was added to be joined but is not intersecting with any other curve.");
      }
      segments.AddRange(SplitCurveFromOverlaps(curveA, allOverlaps));
    }


    // Make segments unique.
    List<Curve> uniqueSegments = new List<Curve> { segments[0] };
    for (int i = 1; i < segments.Count; i++)
    {
      Curve currSegment = segments[i];
      bool alreadyAdded = false;
      for (int j = 0; j < uniqueSegments.Count; j++)
      {
        Curve addedSegment = uniqueSegments[j];
        if (AreCurvesIdentical(currSegment, addedSegment))
        {
          alreadyAdded = true;
          break;
        }
      }
      if (!alreadyAdded)
      {
        uniqueSegments.Add(currSegment);
      }
    }

    Print("Lost " + (segments.Count - uniqueSegments.Count).ToString() + " segments when making unique");

    // Join the unique segments.
    Curve[] joinedCurves = Curve.JoinCurves(uniqueSegments, DIST_TOL, false);

    // Check if the created curves are non-overlapping.
    if (!AreAllCurvesNonOverlapping(joinedCurves.ToList()))
    {
      // throw new Exception("Curves are still overlapping after joining.");
    }
    return joinedCurves;
  }

  /// <summary>
  /// Checks if two curves are identical.
  /// </summary>
  /// <param name="curveA">The first curve to compare.</param>
  /// <param name="curveB">The second curve to compare.</param>
  /// <returns><see langword="true"/> if the curves are identical, <see langword="false"/> otherwise.</returns>
  private bool AreCurvesIdentical(Curve curveA, Curve curveB)
  {
    Rhino.Geometry.Intersect.CurveIntersections intersections = Rhino.Geometry.Intersect.Intersection.CurveCurve(curveA, curveB, DIST_TOL, 0.0);
    if (intersections.Count == 0)
    {
      return false;
    }
    foreach (Rhino.Geometry.Intersect.IntersectionEvent intersection in intersections)
    {
      if (intersection.IsOverlap)
      {
        if (CompareInterval(intersection.OverlapA, curveA.Domain) && CompareInterval(intersection.OverlapB, curveB.Domain))
        {
          Print("curves are equal");
          return true;
        }
      }
    }
    return false;
  }

  /// <summary>
  /// Checks whether any of the curves overlap anywhere else than at their endpoints.
  /// </summary>
  /// <param name="curves"></param>
  private bool AreAllCurvesNonOverlapping(List<Curve> curves)
  {
    for (int i = 0; i < curves.Count; i++)
    {
      for (int j = i + 1; j < curves.Count; j++)
      {
        Curve curveA = curves[i];
        Curve curveB = curves[j];
        Rhino.Geometry.Intersect.CurveIntersections intersections = Rhino.Geometry.Intersect.Intersection.CurveCurve(curveA, curveB, DIST_TOL, 0.0);
        foreach (Rhino.Geometry.Intersect.IntersectionEvent intersection in intersections)
        {
          if (intersection.IsOverlap)
          {
            Print("Distance in overlap: " + intersection.PointA.DistanceTo(intersection.PointA2).ToString());
            Print(intersection.OverlapA.ToString());
            return false;
          }
        }
      }
    }
    return true;
  }

  /// <summary>
  /// Returns a list of intersection-events that corresponds to overlaps between <paramref name="curveA"/> and <paramref name="curveB"/>.
  /// </summary>
  /// <param name="curveA">First curve to check for intersections. </param>
  /// <param name="curveB">econd curve to check for intersections.</param>
  /// <returns>List of intersection-events corresponding to overlaps. List is empty if there are no intersections of type overlap.</returns>
  private List<Rhino.Geometry.Intersect.IntersectionEvent> AllOverlaps(Curve curveA, Curve curveB)
  {
    Rhino.Geometry.Intersect.CurveIntersections intersections = Rhino.Geometry.Intersect.Intersection.CurveCurve(curveA, curveB, DIST_TOL, DIST_TOL);
    List<Rhino.Geometry.Intersect.IntersectionEvent> overlaps = new List<Rhino.Geometry.Intersect.IntersectionEvent>();
    foreach (Rhino.Geometry.Intersect.IntersectionEvent intersection in intersections)
    {
      if (intersection.IsOverlap)
      {
        overlaps.Add(intersection);
      }
    }
    return overlaps;
  }
  #endregion
}