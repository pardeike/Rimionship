using System.IO;
using UnityEditor;

public class CreateAssetBundles
{
	[MenuItem("Assets/Create AssetBundles")]
	static void BuildStandaloneAssetBundles()
	{
		string path;

		path = "Assets/AssetBundlesWin";
		PreBuildDirectoryCheck(path);
		_ = BuildPipeline.BuildAssetBundles(path, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);

		path = "Assets/AssetBundlesMac";
		PreBuildDirectoryCheck(path);
		_ = BuildPipeline.BuildAssetBundles(path, BuildAssetBundleOptions.None, BuildTarget.StandaloneOSX);

		path = "Assets/AssetBundlesLinux";
		PreBuildDirectoryCheck(path);
		_ = BuildPipeline.BuildAssetBundles(path, BuildAssetBundleOptions.None, BuildTarget.StandaloneLinux64);

		File.Copy("Assets/AssetBundlesWin/rimionship", "../../Resources/rimionship-win");
		File.Copy("Assets/AssetBundlesMac/rimionship", "../../Resources/rimionship-mac");
		File.Copy("Assets/AssetBundlesLinux/rimionship", "../../Resources/rimionship-linux");
	}


	static void PreBuildDirectoryCheck(string directory)
	{
		if (!Directory.Exists(directory))
			_ = Directory.CreateDirectory(directory);
	}
}
