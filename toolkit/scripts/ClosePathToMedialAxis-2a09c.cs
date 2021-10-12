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
public abstract class Script_Instance_2a09c : GH_ScriptInstance
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
  private void RunScript(List<Curve> medialAxisCurveList, List<Point3d> nodePoint3dList, ref object connectingCurveList)
  {
    List<Curve> result = new List<Curve>();
    foreach (Point3d node in nodePoint3dList)
    {
      if (node == null)
      {
        continue;
      }
      Point3d closestPoint = new Point3d();
      double minDist = double.MaxValue;
      double minParam = -1.0;
      Curve minCurve = medialAxisCurveList[0];

      // Find the closest point over all medial axis curves.
      foreach (Curve medialAxisCurve in medialAxisCurveList)
      {
        if (medialAxisCurve == null)
        {
          continue;
        }
        double closeParam;
        medialAxisCurve.ClosestPoint(node, out closeParam);
        Point3d closePoint = medialAxisCurve.PointAt(closeParam);
        double dist = node.DistanceTo(closePoint);
        if (dist < minDist)
        {
          closestPoint = closePoint;
          minDist = dist;
          minParam = closeParam;
          minCurve = medialAxisCurve;
        }
      }

      // We need to split the curve and add the split segment to the list as well.
      Curve[] splitMedialAxisCurves = minCurve.Split(minParam);
      if (splitMedialAxisCurves == null)
      {
        continue;
      }
      foreach (Curve splitCurve in splitMedialAxisCurves)
      {
        result.Add(splitCurve);
      }

      // Construct connecting curve to closest point.
      result.Add(new LineCurve(node, closestPoint));
    }
    connectingCurveList = result;
  }
  #endregion
  #region Additional

  #endregion
}