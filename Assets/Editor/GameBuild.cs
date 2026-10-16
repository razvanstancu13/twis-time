using System.Linq;
using UnityEditor;
using UnityEngine;

// Builds TwisTime for Windows: menu "TwisTime > Build Windows", or -executeMethod GameBuild.Windows.
// Output: Builds/Windows/TwisTime.exe. Portrait 640 x 960 window, resizable (the picture keeps 2:3 with black bars).
public static class GameBuild
{
	static readonly string[] Scenes = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/GameOriginal.unity" };

	[UnityEditor.MenuItem("TwisTime/Build Windows")]      // full name: the game has its own MenuItem class
	public static void Windows()
	{
		// the menu loads scene 1 (the game), the game goes back to scene 0
		EditorBuildSettings.scenes = Scenes.Select(s => new EditorBuildSettingsScene(s, true)).ToArray();
		PlayerSettings.productName = "TwisTime";
		PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
		PlayerSettings.defaultScreenWidth = 640;
		PlayerSettings.defaultScreenHeight = 960;
		PlayerSettings.resizableWindow = true;
		var r = BuildPipeline.BuildPlayer(new BuildPlayerOptions
		{
			scenes = Scenes,
			locationPathName = "Builds/Windows/TwisTime.exe",
			target = BuildTarget.StandaloneWindows64,
		});
		Debug.Log("TT build: " + r.summary.result + ", " + r.summary.totalErrors + " errors, " + (r.summary.totalSize / 1048576f).ToString("F1") + " MB");
		if (Application.isBatchMode)
			EditorApplication.Exit(r.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
	}
}
