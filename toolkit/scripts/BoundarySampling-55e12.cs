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
public abstract class Script_Instance_55e12 : GH_ScriptInstance
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
  private void RunScript(List<Curve> BoundarySegmentCurveList, double StepSize, ref object SamplePoints)
  {
    // Each element is a list of points where the boundary should be sampled.
    List<Point3d> sampleLocations = new List<Point3d>();
    foreach (Curve boundarySegment in BoundarySegmentCurveList)
    {
      double curveLength = boundarySegment.GetLength();
      int regularlySpacedSampleNum = (int)Math.Floor(curveLength / (2 * StepSize));
      boundarySegment.Domain.MakeIncreasing();
      Interval boundaryDomain = boundarySegment.Domain;
      double stepSizeParamSpace = boundaryDomain.Length * (StepSize / curveLength);
      for (int i = 1; i < regularlySpacedSampleNum + 1; i++)
      {
        sampleLocations.Add(boundarySegment.PointAt(boundaryDomain.Min + i * stepSizeParamSpace));
        sampleLocations.Add(boundarySegment.PointAt(boundaryDomain.Max - i * stepSizeParamSpace));
      }
      sampleLocations.Add(boundarySegment.PointAt(boundaryDomain.Mid));
    }
    SamplePoints = sampleLocations;
  }
  #endregion
  #region Additional
  DataTree<T> ListOfListsToTree<T>(List<List<T>> list)
  {
    DataTree<T> tree = new DataTree<T>();
    int i = 0;
    foreach (List<T> innerList in list)
    {
      tree.AddRange(innerList, new GH_Path(new int[] { 0, i }));
      i++;
    }
    return tree;
  }
  #endregion
}