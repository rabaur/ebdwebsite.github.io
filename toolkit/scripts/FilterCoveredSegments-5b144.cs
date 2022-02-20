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
public abstract class Script_Instance_5b144 : GH_ScriptInstance
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
  private void RunScript(List<Curve> SegmentCurveList, ref object UncoveredSegmentCurves)
  {
    // Sort segments by length.
    // Lustig lustig trallalala
    double[] lengths = new double[SegmentCurveList.Count];
    System.Threading.Tasks.Parallel.For(0, SegmentCurveList.Count, i =>
    {
      lengths[i] = SegmentCurveList[i].GetLength();
    });
    Curve[] segmentCurveArray = SegmentCurveList.ToArray();
    Array.Sort(lengths, segmentCurveArray);
    Array.Reverse(segmentCurveArray);

    // Create mask indicating if segment is covered.
    bool[] covered = new bool[segmentCurveArray.Length];

    // Test for each pair if the segment of greater or equal length completely covers the smaller segment.
    for (int i = 0; i < segmentCurveArray.Length; i++)
    {
      if (covered[i])
      {
        continue;
      }
      Curve largeSeg = segmentCurveArray[i];
      for (int j = i + 1; j < segmentCurveArray.Length; j++)
      {
        if (covered[j])
        {
          continue;
        }

        Curve smallSeg = segmentCurveArray[j];
        // Check if the smaller curve is covered completely by the larger curve, i.e.:
        // 1. There is an intersection.
        // 2. This intersection is an overlap.
        // 3. Up to some tolerance, the overlap domain is equivalent to the domain of the smaller curve.
        CurveIntersections intersects = Intersection.CurveCurve(largeSeg, smallSeg, RhinoMath.SqrtEpsilon, RhinoMath.SqrtEpsilon);
        if (intersects == null)
        {
          // No intersections.
          continue;
        }
        foreach (IntersectionEvent intersect in intersects)
        {
          if (!intersect.IsOverlap)
          {
            continue;
          }
          Interval smallDomain = smallSeg.Domain;
          if (intersect.OverlapB.Min <= smallDomain.Min + RhinoMath.SqrtEpsilon && smallDomain.Max - RhinoMath.SqrtEpsilon <= intersect.OverlapB.Max)
          {
            covered[j] = true;
          }
        }
      }
    }

    // Filter curves that are covered.
    List<Curve> notCovered = new List<Curve>();
    for (int i = 0; i < segmentCurveArray.Length; i++)
    {
      if (!covered[i])
      {
        notCovered.Add(segmentCurveArray[i]);
      }
    }
    UncoveredSegmentCurves = notCovered;
  }
  #endregion
  #region Additional

  #endregion
}