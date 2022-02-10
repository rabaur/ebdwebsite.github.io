using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;



/// <summary>
/// This class will be instantiated on demand by the Script component.
/// </summary>
public abstract class Script_Instance_3a969 : GH_ScriptInstance
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
  private void RunScript(List<Curve> medialAxisCurveList, List<Point3d> branchPointList, ref object splitMedialAxisCurveList)
  {
    List<Curve> res = new List<Curve>();
    foreach (Curve curve in medialAxisCurveList)
    {
      res.AddRange(SplitByBranchPoint(curve, branchPointList, 0));
    }
    splitMedialAxisCurveList = res;
  }
  #endregion
  #region Additional
  private List<Curve> SplitByBranchPoint(Curve curve, List<Point3d> branchPoints, int recursionDepth)
  {
    if (recursionDepth == 20)
    {
      Print("Hit max recursion-depth.");
      return new List<Curve>();
    }

    // Get parameters of closest points.
    List<double> closestParams = new List<double>();
    double param = 0.0;
    foreach (Point3d branchPoint in branchPoints)
    {
      if (curve.ClosestPoint(branchPoint, out param, RhinoMath.SqrtEpsilon))
      {
        closestParams.Add(param);
      }
    }
    Print(String.Format("Number of branchpoints on curve: {0}", closestParams.Count.ToString()));
    foreach (double closestParam in closestParams)
    {
      Print(closestParam.ToString());
    }

    // Split the curve by the parameters.
    Curve[] splitCurves = curve.Split(closestParams);
    Print(String.Format("Number of split curves: {0}", splitCurves.Length.ToString()));
    foreach (Curve splitCurve in splitCurves)
    {
      Print(splitCurve.Domain.ToString());
    }

    if (splitCurves.Length <= 1)
    {
      return new List<Curve>(splitCurves);
    }
    
    List<Curve> result = new List<Curve>();    
    foreach (Curve splitCurve in splitCurves)
    {
      result.AddRange(SplitByBranchPoint(splitCurve, branchPoints, recursionDepth++));
    }
    return result;
  }
  #endregion
}