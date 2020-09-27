using System;
using UnityEngine;
using System.Collections;
using System.IO;

public static class Paths {

	/// <summary>
	/// The root of the Unity project.
	/// </summary>
	public static string ProjectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

	/// <summary>
	/// The path to the ProjectSettings of the project.
	/// </summary>
	public static string ProjectSettings = Path.Combine(ProjectRoot, "ProjectSettings");

	/// <summary>
	/// The path to the Shared Assets folder
	/// </summary>
	public static string SharedAssets = Path.Combine(Path.Combine(Path.Combine(ProjectRoot, "Assets"), "Altspace"), "Shared Assets");

	/// <summary>
	/// The path to the Shared Settings folder
	/// </summary>
	public static string SharedSettings = Path.Combine(SharedAssets, "Settings");

	public static string Desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
}
