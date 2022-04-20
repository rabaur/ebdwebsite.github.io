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
public abstract class Script_Instance_49288 : GH_ScriptInstance
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
  private void RunScript(List<Line> LineList, double AnglePerBin, ref object A)
  {
    // Get the line orientations wrt. the x unit vector on the xy-plane.
    List<double> angles = new List<double>();
    foreach (Line line in LineList)
    {
      double angle_rad= Vector3d.VectorAngle(line.Direction, Vector3d.XAxis, Plane.WorldXY);
      double angle_deg = (180.0 / Math.PI) * angle_rad;
      angle_deg %= 180.0;  // Directions are same up to 180 degrees.
      angles.Add(angle_deg);
    }

    // The bins are distributed such that the x-axis lies firmly inside a bin, because I assume that many people model their building along the xy-grid.
    // Example: If <AnglePerBin> = 45, then the resulting bins are:
    // * [-22.5,  22.5[
    // * [ 22.5,  67.5[
    // * [ 67.5, 112.5[
    // * [112.5, 157.5[
    // Because of that change, I need to subtract 180 deg from the angles that would have lied in the range that is not covered with the new bins.
    for (int i = 0; i < angles.Count; i++)
    {
      if (angles[i] > 180.0 - AnglePerBin / 2.0)
      {
        angles[i] -= 180.0;
      }
      Print(angles[i].ToString());
    }

    // Now I can bin the lines according to their angle.
  }
  #endregion
  #region Additional

  #endregion
}