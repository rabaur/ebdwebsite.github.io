using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;



/// <summary>
/// This class will be instantiated on demand by the Script component.
/// </summary>
public abstract class Script_Instance_e843e : GH_ScriptInstance
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
  private void RunScript(List<Curve> SegmentCurveList, double LowerTolerance, double UpperTolerance, ref object BranchPointList, ref object BranchPointDelimitedSegmentCurveList)
  {

  }
  #endregion
  #region Additional

  /// <summary>
  /// The script receives a list of explodes medial axis segments as input. In order to reduce complexity, branchpoint-classification is done on 
  /// sublists of segments that are contiguous.
  /// </summary>
  /// <param name="segments">Non-empty list of possibly discontiguous medial axis segments.</param>
  /// <returns>List of lists of contiguous medial axis segments.</returns>
  private List<List<Curve>> PartitionInputIntoContiguousSubLists(List<Curve> segments)
  {
    List<Curve> subList = new List<Curve>();
    subList.Add(segments[0]);
    for (int i = 1; i < segments.Count; i++)
    {
      Rhino.Geometry.Intersect.CurveIntersections intersect = Rhino.
      if (Rhino.InsubList[subList.Count - 1].)
    }
  }
  #endregion
}