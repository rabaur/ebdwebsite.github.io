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
public abstract class Script_Instance_20516 : GH_ScriptInstance
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
  private void RunScript(List<Curve> Segments, ref object PairwiseSplitSegments)
  {
    List<Curve> pairwiseSplitCurves = new List<Curve>();
    foreach (Curve seg0 in Segments)
    {
      List<double> splitParams = new List<double>();
      foreach (Curve seg1 in Segments)
      {
        if (seg0 == seg1)
        {
          continue;
        }
        CurveIntersections intersects = Intersection.CurveCurve(seg0, seg1, 0.1, 0.1);
        if (intersects == null)
        {
          continue;
        }
        foreach (IntersectionEvent intersect in intersects)
        {
          if (intersect.IsPoint)
          {
            splitParams.Add(intersect.ParameterA);
          }
        }
      }
      Curve[] splitCurves = seg0.Split(splitParams);
      Print(splitCurves.Length.ToString());
      if (splitCurves == null)
      {
        throw new Exception("Splitting curve did not yield correct result.");
      }
      pairwiseSplitCurves.AddRange(splitCurves);
    }
    PairwiseSplitSegments = pairwiseSplitCurves;
  }
  #endregion
  #region Additional

  #endregion
}