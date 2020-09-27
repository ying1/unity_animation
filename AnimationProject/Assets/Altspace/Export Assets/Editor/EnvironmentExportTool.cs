using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class EnvironmentExportTool
{
    private static EnvironmentExportTool _instance;
    public static EnvironmentExportTool Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new EnvironmentExportTool();
            }

            return _instance;
        }
    }

    public enum Platform { PC = 0, MAC, ANDROID };

	[SerializeField]
	[HideInInspector]
	private string selectedSceneId;


	[HideInInspector]
	public bool buildPCAssetBundle = true;
	[HideInInspector]
	public bool buildMacAssetBundle = false;
	[HideInInspector]
	public bool buildAndroidAssetBundle = true;

	[HideInInspector]
	public string assetBundlePath;

	private string generatedAssetBundlePath;
	private string generatedManifestPath;
	public struct PlatformDetail
	{
		public PlatformDetail(EnvironmentExportTool.Platform platform, string filename)
		{
			this.platform = platform;
			platformFilename = filename;
		}
		public EnvironmentExportTool.Platform platform;
		public string platformFilename;
	}
	private PlatformDetail currentPlatformDetail;
	private Stack<PlatformDetail> currentPlatformDetails;

	private void HandleRemainingAssetBundles()
	{
		if (currentPlatformDetails.Count > 0)
		{
			PlatformDetail poppedPlatform = currentPlatformDetails.Pop();
			//StartCoroutine(UploadAndPublishAssetBundle(poppedPlatform));
		}
		else
		{
			StopPlayingIfEditor();
		}
	}

    public string assetBundleName;
		
	private void StopPlayingIfEditor()
	{
#if UNITY_EDITOR
		if (EditorApplication.isPlaying)
			EditorApplication.isPlaying = false;
#endif
	}
		
	private void PrintPolyCount()
	{
		var totalPolyCount = ComputeTotalPolyCount();

		if (totalPolyCount > 50000)
		{
			Debug.LogWarning("Poly count is over 50000!: " + totalPolyCount);
		}
		else if (totalPolyCount > 30000)
		{
			Debug.LogWarning("Poly count is over 30000!: " + totalPolyCount);
		}
	}

	private int ComputeTotalPolyCount()
	{
		int totalPolyCount = 0;
		MeshFilter[] allMeshFilters = GameObject.FindObjectsOfType<MeshFilter>();

		foreach (MeshFilter mf in allMeshFilters)
		{
			int meshPolyCount = mf.sharedMesh.triangles.Length / 3;
			//Debug.Log(mf.gameObject);
			//*mf.gameObject.renderer.sharedMaterials.Length;
			//Debug.Log("before: " + tmpCount.ToString() + ", after: " + (tmpCount/mf.gameObject.renderer.sharedMaterials.Length).ToString());
			totalPolyCount += meshPolyCount;
		}

		Debug.Log("num polys: " + totalPolyCount.ToString());
		return totalPolyCount;
	}
		
	private void LogException(Exception exception)
	{
		Debug.Log(exception.Data);
		Debug.Log(exception.Message);
		Debug.LogException(exception);
	}		
}

