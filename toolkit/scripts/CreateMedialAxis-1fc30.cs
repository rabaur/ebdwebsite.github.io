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
public abstract class Script_Instance_1fc30 : GH_ScriptInstance
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
  private void RunScript(List<Curve> voronoiDiagramCurveList, List<Curve> boundaryCurveList, Brep walkableSurfaceBrep, double boundaryProximityTolerance, ref object medialAxisCurveList)
  {
    // First "explode" all curves from the Voronoi-diagram into segments and eventually save them in array for parallel processing.
    Line[] allSegmentArray = CreateSegmentsSequential(voronoiDiagramCurveList); // For some reason, the sequential method is slightly faster here.

    // Create mask for valid segments.
    BoundingBox[] bbs = new BoundingBox[boundaryCurveList.Count];
    bool[] mask = SelectValidSegmentsParallel(allSegmentArray, boundaryCurveList, bbs, walkableSurfaceBrep, boundaryProximityTolerance); // Parallel clearly faster.

    // Only select valid medial axis segments.
    List<Line> result = new List<Line>();
    for (int i = 0; i < allSegmentArray.Length; i++)
    {
      if (mask[i])
      {
        result.Add(allSegmentArray[i]);
      }
    }
    medialAxisCurveList = result;
  }
  #endregion
  #region Additional

  private Line[] CreateSegmentsSequential(List<Curve> voronoiDiagramCurveList)
  {
    List<Line> allSegments = new List<Line>();
    foreach (Curve curve in voronoiDiagramCurveList)
    {
      if (curve == null)
      {
        continue;
      }
      if (!curve.IsPolyline())
      {
        // throw new Exception("Input curve was not polyline.");
        continue;
      }
      Polyline polyline = new Polyline();
      curve.TryGetPolyline(out polyline);
      Line[] segments = polyline.GetSegments();
      foreach (Line segment in segments)
      {
        allSegments.Add(segment);
      }
    }
    return allSegments.ToArray();
  }

  private Line[] CreateSegmentsParallel(List<Curve> voronoiDiagramCurveList)
  {
    Line[][] segments = new Line[voronoiDiagramCurveList.Count][];
    System.Threading.Tasks.Parallel.For(0, segments.Length, i =>
      {
      Curve curve = voronoiDiagramCurveList[i];
      if (curve == null)
      {
        throw new Exception("Input curve was null.");
      }
      if (!curve.IsPolyline())
      {
        throw new Exception("Input curve was not polyline.");
      }
      Polyline polyline = new Polyline();
      curve.TryGetPolyline(out polyline);
      segments[i] = polyline.GetSegments();
      });
    return segments.SelectMany(a => a).ToArray();
  }

  /// <summary>
  /// Parallel implementation: Selects all Voronoi-diagram segments that approximate the medial axis.
  /// </summary>
  /// <param name="segments">Segments of the Voronoi-diagram.</param>
  /// <param name="boundaryCurveList">Boundary curves of the shape for which the medial axis is computed.</param>
  /// <param name="walkableSurfaceBrep">Surface inside of the shape.</param>
  /// <param name="boundaryProximityTolerance">If a segment is closer than that to the shape-boundary, it is discarded.</param>
  /// <returns>A boolean array of the same length as <paramref name="segments"/>, acting as a mask for the valid segments.</returns>
  private bool[] SelectValidSegmentsParallel(Line[] segments, List<Curve> boundaryCurveList, BoundingBox[] bbs, Brep walkableSurfaceBrep, double boundaryProximityTolerance)
  {
    bool[] mask = new bool[segments.Length];

    for (int i = 0; i < boundaryCurveList.Count; i++)
    {
      if (boundaryCurveList[i] == null)
      {
        bbs[i] = BoundingBox.Empty;
        continue;
      }
      bbs[i] = boundaryCurveList[i].GetBoundingBox(accurate:false);
    }
    System.Threading.Tasks.Parallel.For(0, mask.Length, i =>
      {
      mask[i] = IsValidSegment(segments[i], boundaryCurveList, bbs, walkableSurfaceBrep, boundaryProximityTolerance);
      });
    return mask;
  }

  /// <summary>
  /// Sequential implementation: Selects all Voronoi-diagram segments that approximate the medial axis.
  /// </summary>
  /// <param name="segments">Segments of the Voronoi-diagram.</param>
  /// <param name="boundaryCurveList">Boundary curves of the shape for which the medial axis is computed.</param>
  /// <param name="walkableSurfaceBrep">Surface inside of the shape.</param>
  /// <param name="boundaryProximityTolerance">If a segment is closer than that to the shape-boundary, it is discarded.</param>
  /// <returns>A boolean array of the same length as <paramref name="segments"/>, acting as a mask for the valid segments.</returns>
  private bool[] SelectValidSegmentsSequential(Line[] segments, List<Curve> boundaryCurveList, BoundingBox[] bbs, Brep walkableSurfaceBrep, double boundaryProximityTolerance)
  {
    bool[] mask = new bool[segments.Length];
    for (int i = 0; i < segments.Length; i++)
    {
      mask[i] = IsValidSegment(segments[i], boundaryCurveList, bbs, walkableSurfaceBrep, boundaryProximityTolerance);
    }
    return mask;
  }

  /// <summary>
  /// Checks if a segment belongs to the subgraph of the Voronoi-diagram that approximates the medial axis.
  /// </summary>
  /// <param name="segment">The segment for which to check.</param>
  /// <param name="boundaryCurves">The curves describing the boundary for which to find the medial axis.</param>
  /// <returns>true if is medial axis segment, false otherwise.</returns>
  private bool IsValidSegment(Line segment, List<Curve> boundaryCurves, BoundingBox[] bbs, Brep walkableSurfaceBrep, double closenessTolerance)
  {
    bool isIntersecting = SegmentIntersectsBoundary(segment, boundaryCurves, bbs, RhinoMath.SqrtEpsilon);
    bool isWalkable = SegmentIsOnWalkableSurface(segment, walkableSurfaceBrep, RhinoMath.SqrtEpsilon);
    bool isTooCloseToBoundary = SegmentIsTooCloseToBoundary(segment, boundaryCurves, closenessTolerance);
    return !isIntersecting && isWalkable && !isTooCloseToBoundary;
  }

  /// <summary>
  /// Checks whether a segment crosses the boundary or is close enough (within a tolerance) to it.
  /// </summary>
  /// <param name="segment">The segment for which to check whether it crosses the boundary.</param>
  /// <param name="boundaryCurves">The curves that define the boundary.</param>
  /// <param name="tolerance">The distance to the boundary must be larger than this to be non-crossing.</param>
  /// <returns>true if the segment crosses the boundary or is too close to it. false otherwise.</returns>
  private bool SegmentIntersectsBoundary(Line segment, List<Curve> boundaryCurves, BoundingBox[] bbs, double tolerance)
  {
    bool intersects = false;
    BoundingBox currBB = segment.BoundingBox;
    for (int i = 0; i < boundaryCurves.Count; i++)
    {
      Curve boundaryCurve = boundaryCurves[i];
      if (currBB.IsValid && bbs[i].IsValid && !BoundingBoxesIntersect2D(currBB, bbs[i]))
      {
        continue;
      }
      if (Rhino.Geometry.Intersect.Intersection.CurveCurve(boundaryCurve, segment.ToNurbsCurve(), tolerance, 0.0).Count != 0)
      {
        intersects = true;
        break;
      }
    }
    return intersects;
  }

  /// <summary>
  /// Checks whether a segment lies on the walkable surface.
  /// </summary>
  /// <param name="segment">The segment for which the property should be checked.</param>
  /// <param name="tolerance">Distance from curve to walkable surface must be smaller than this to count as "on walkable surface".</param>
  /// <param name="walkableSurfaceBrep">The walkable surface.</param>
  /// <returns>true if segment is on walkable surface (within tolerance), false otherwise.</returns>
  private bool SegmentIsOnWalkableSurface(Line segment, Brep walkableSurfaceBrep, double tolerance)
  {
    Curve segmentCurve = segment.ToNurbsCurve();
    Curve[] overlapCurves;
    Point3d[] intersectionPoints;
    Rhino.Geometry.Intersect.Intersection.CurveBrep(segmentCurve, walkableSurfaceBrep, tolerance, out overlapCurves, out intersectionPoints);
    return overlapCurves.Length > 0 || intersectionPoints.Length > 0;
  }

  /// <summary>
  /// Checks for a segment if it is too close to the boundary. It does that by comparing the distance of the endpoints of the segment to all boundary curves.
  /// </summary>
  /// <param name="segment">The segment to check for.</param>
  /// <param name="boundaryCurves">The curves that define the boundary.</param>
  /// <param name="tolerance">If the distance is smaller than this, the segment is considered to be too close.</param>
  /// <returns>true if is too close, false otherwise.</returns>
  private bool SegmentIsTooCloseToBoundary(Line segment, List<Curve> boundaryCurves, double tolerance)
  {
    Curve segmentCurve = segment.ToNurbsCurve();
    Point3d startPoint = segmentCurve.PointAtStart;
    Point3d endPoint = segmentCurve.PointAtEnd;
    for (int i = 0; i < boundaryCurves.Count; i++)
    {
      Curve currCurve = boundaryCurves[i];
      double startParam = 0.1f;
      currCurve.ClosestPoint(startPoint, out startParam);
      if (startPoint.DistanceTo(currCurve.PointAt(startParam)) < tolerance)
      {
        return true;
      }
      double endParam = 0.1f;
      currCurve.ClosestPoint(endPoint, out endParam);
      if (endPoint.DistanceTo(currCurve.PointAt(endParam)) < tolerance)
      {
        return true;
      }
    }
    return false;
  }

  private bool BoundingBoxesIntersect2D(BoundingBox b0, BoundingBox b1)
  {
    return (b0.Min.X < b1.Max.X) && (b0.Max.X > b1.Min.X) && (b0.Min.Y < b1.Max.Y) && (b0.Max.Y > b1.Min.Y);
  }
  #endregion
}