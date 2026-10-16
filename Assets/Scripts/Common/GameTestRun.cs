using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

// Test run, only when started with  -tt-test <folder> [mode 0..4]  (does nothing in normal play; mode default 1 =
// Time Trial). Drives the real game code: menu pages (scores, help, options), Play -> the mode -> New Game, the hinted
// move (swap, chunk, cascade), the pause menu; then for a timed mode the clock run down to game over / high score,
// for the others Quit (the game is saved) and back on the mode's Continue page. Screenshots + log lines "TTTEST ...".
public class GameTestRun : MonoBehaviour
{
	string _dir;
	int _errors;
	GameMode _mode = GameMode.TimeTrial;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	static void Start0()
	{
		var args = System.Environment.GetCommandLineArgs();
		int i = System.Array.IndexOf(args, "-tt-test");
		if (i < 0 || i + 1 >= args.Length) return;
		var go = new GameObject("GameTestRun");
		DontDestroyOnLoad(go);
		var t = go.AddComponent<GameTestRun>();
		t._dir = args[i + 1];
		if (i + 2 < args.Length && int.TryParse(args[i + 2], out int m)) t._mode = (GameMode)m;
	}

	void Awake()
	{
		Application.logMessageReceived += (msg, stack, type) =>
		{
			if (type != LogType.Error && type != LogType.Exception) return;
			_errors++;
			if (_errors <= 25) Debug.Log("TTTEST error: " + msg + " | " + stack.Split('\n')[0]);
		};
		SceneManager.sceneLoaded += (s, m) => Debug.Log("TTTEST loaded " + s.name + " at " + Time.time.ToString("F1"));
	}

	static object Call(object target, string method, params object[] args) =>
		target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Invoke(target, args);

	IEnumerator Choose(int id, string shot = null)
	{
		Call(FindAnyObjectByType<MainMenuController>(), "Choose", id);
		yield return new WaitForSeconds(4);
		if (shot != null) yield return Shot(shot);
	}

	IEnumerator Start()
	{
		Directory.CreateDirectory(_dir);
		int mode = (int)_mode;
		SaveGame.Clear(_mode);
		yield return new WaitForSeconds(3);
		yield return Shot("1-menu");
		yield return Choose(3, "2-score-modes");
		yield return Choose(mode, "2-scores");
		yield return Choose(5);                     // back to the score modes
		yield return Choose(5);                     // back to the main page
		yield return Choose(1, "3-help");
		yield return Choose(5);
		yield return Choose(2, "4-options");
		yield return Choose(5);
		yield return Choose(0, "5-modes");
		yield return Choose(mode, "5-play");
		Call(FindAnyObjectByType<MainMenuController>(), "Choose", 0);     // New Game (no saved game -> straight in)
		yield return new WaitForSeconds(4.5f);
		yield return Shot("6-board");

		var game = GameController.Instance;
		Debug.Log("TTTEST game mode " + game.Mode + ", time bar " + (FindAnyObjectByType<TimeBar>() != null) + ", combo text " +
		          (game.comboText && game.comboText.isActiveAndEnabled ? game.comboText.text : "-"));
		var board = (Board)typeof(GameController).GetField("_board", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
		if (board.FindMove(out int a, out int b))
		{
			Debug.Log("TTTEST move " + a + " <-> " + b + ", score before " + game.Score);
			game.OnGemPressed(a / 8, a % 8);
			yield return new WaitForSeconds(0.3f);
			game.OnGemPressed(b / 8, b % 8);
			yield return new WaitForSeconds(1.6f);
			// every gem of the chunk, the swapped one included, must be on its Z-spin sheet now
			var gems = (GameObject[])typeof(GameController).GetField("_gems", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
			foreach (bool big in new[] { true, false })
				foreach (int cell in board.FindChunk(big))
					Debug.Log("TTTEST chunk cell " + cell + (cell == a || cell == b ? " (swapped)" : "") + " sheet " + gems[cell].GetComponent<Renderer>().material.mainTexture.name);
			yield return Shot("7-chunk");
			yield return new WaitForSeconds(4);
			Debug.Log("TTTEST after the move: score " + game.Score + ", input locked " + game.InputLocked +
			          (game.comboText && game.comboText.isActiveAndEnabled ? ", combo text " + game.comboText.text : ""));
			yield return Shot("8-after-move");
		}
		else Debug.Log("TTTEST no move found");

		var pause = game.GetComponent<PauseMenu>();
		typeof(PauseMenu).GetProperty("Paused").SetValue(pause, true);
		pause.StartCoroutine((IEnumerator)Call(pause, "Pause"));
		yield return new WaitForSeconds(1.5f);
		yield return Shot("9-pause");

		var bar = FindAnyObjectByType<TimeBar>();
		if (bar)
		{
			pause.StartCoroutine((IEnumerator)Call(pause, "Unpause"));
			yield return new WaitForSeconds(1.5f);
			bar.AddTime(-200);                       // run the clock out
			yield return new WaitForSeconds(2.5f);
			yield return Shot("10-game-over");
			yield return new WaitForSeconds(3.5f);
			yield return Shot("11-high-score");
			yield return new WaitForSeconds(6);
			SaveGame.ReadTable(_mode, out int[] hs, out string[] hn);
			Debug.Log("TTTEST high scores: " + string.Join(", ", System.Linq.Enumerable.Select(System.Linq.Enumerable.Range(0, hs.Length), k => hn[k] + " " + hs[k])));
		}
		else
		{
			int score = game.Score;
			Call(pause, "Yes");                      // Quit, as from the confirmation: the game is saved
			yield return new WaitForSeconds(4);
			Debug.Log("TTTEST saved score " + SaveGame.SavedScore(_mode) + " (was " + score + ")");
			yield return Choose(0);
			yield return Choose(mode, "10-continue");
			SaveGame.Clear(_mode);
		}
		Debug.Log("TTTEST end: scene " + SceneManager.GetActiveScene().name + ", errors " + _errors);
		yield return Shot("12-end");
		Application.Quit();
	}

	IEnumerator Shot(string name)
	{
		yield return new WaitForEndOfFrame();
		ScreenCapture.CaptureScreenshot(Path.Combine(_dir, name + ".png"));
		yield return null;
	}
}
