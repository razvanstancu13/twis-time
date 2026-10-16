using UnityEngine;

// The saved game of one mode (to continue later) and its high score table, in PlayerPrefs under the same keys as
// in 2012. A saved score of 0 means "no game to continue".
public class SaveGame
{
	public const int TableSize = 10;
	static readonly int[] DefaultScores = { 1000, 900, 800, 700, 600, 500, 400, 300, 200, 100 };
	const string DefaultName = "TwisTime";

	readonly GameMode _mode;
	readonly string _k;

	public int score;
	public int[] board;            // 64 cells, when there is a game to continue
	public int[] colorCount = new int[Board.Colors];
	public int timeLeft;           // time bar position (pixels, 0 = full) for the timed modes
	public int combosLeft;         // Combo 20
	public int comboModeCombos;    // Combo Mode: combos in hand
	public int comboModeNextAt;    // Combo Mode: the score at which the last combo was earned

	public int[] highScores;
	public string[] highScoreNames;

	public SaveGame(GameMode mode) { _mode = mode; _k = ModeInfo.Of(mode).saveKey; }

	public bool HasGame => score != 0;

	public static int SavedScore(GameMode mode) => PlayerPrefs.GetInt("playerScore" + ModeInfo.Of(mode).saveKey);

	public void Load()
	{
		highScores = PrefsArrays.GetIntArray("highScores" + _k, -1, TableSize);
		highScoreNames = PrefsArrays.GetStringArray("highScoreNames" + _k, "DEFAULT", TableSize);
		score = PlayerPrefs.GetInt("playerScore" + _k);
		if (HasGame)
		{
			board = PrefsArrays.GetIntArray("save" + _k, -1, 64);
			if (_mode == GameMode.Classic || _mode == GameMode.ComboMode)
				colorCount = PrefsArrays.GetIntArray("colorCount" + _k, 0, Board.Colors);
			if (_mode == GameMode.TimeTrial || _mode == GameMode.Survival)
				timeLeft = PlayerPrefs.GetInt("playerTime" + _k);
			if (_mode == GameMode.Combo20)
				combosLeft = PlayerPrefs.GetInt("numberOfCombos");
			if (_mode == GameMode.ComboMode)
			{
				comboModeCombos = PlayerPrefs.GetInt("nrCombos");
				comboModeNextAt = PlayerPrefs.GetInt("scoreAdd");
			}
		}
		for (int i = 0; i < TableSize; i++)
			if (i >= highScores.Length || highScores[i] == -1)
			{
				EnsureTable();
				highScores[i] = DefaultScores[i];
				highScoreNames[i] = DefaultName;
			}
		EnsureTable();
	}

	void EnsureTable()
	{
		if (highScores.Length < TableSize) System.Array.Resize(ref highScores, TableSize);
		if (highScoreNames.Length < TableSize) System.Array.Resize(ref highScoreNames, TableSize);
	}

	public void Save()
	{
		PrefsArrays.SetIntArray("save" + _k, board);
		PlayerPrefs.SetInt("playerScore" + _k, score);
		if (_mode == GameMode.Classic || _mode == GameMode.ComboMode)
			PrefsArrays.SetIntArray("colorCount" + _k, colorCount);
		if (_mode == GameMode.TimeTrial || _mode == GameMode.Survival)
			PlayerPrefs.SetInt("playerTime" + _k, timeLeft);
		if (_mode == GameMode.Combo20)
			PlayerPrefs.SetInt("numberOfCombos", combosLeft);
		if (_mode == GameMode.ComboMode)
		{
			PlayerPrefs.SetInt("nrCombos", comboModeCombos);
			PlayerPrefs.SetInt("scoreAdd", comboModeNextAt);
		}
	}

	// forget the game in progress (the high scores stay)
	public static void Clear(GameMode mode) => PlayerPrefs.SetInt("playerScore" + ModeInfo.Of(mode).saveKey, 0);

	// if the score makes the table: puts it in, saves the table and returns its place (0 = best); else -1
	public int AddHighScore(int newScore, string name)
	{
		for (int i = 0; i < TableSize; i++)
			if (highScores[i] <= newScore)
			{
				for (int j = TableSize - 1; j > i; j--) { highScores[j] = highScores[j - 1]; highScoreNames[j] = highScoreNames[j - 1]; }
				highScores[i] = newScore;
				highScoreNames[i] = name;
				PrefsArrays.SetIntArray("highScores" + _k, highScores);
				PrefsArrays.SetStringArray("highScoreNames" + _k, highScoreNames);
				return i;
			}
		return -1;
	}

	// the table as the menu shows it: the stored one, or the defaults when nothing was ever stored
	public static void ReadTable(GameMode mode, out int[] scores, out string[] names)
	{
		string k = ModeInfo.Of(mode).saveKey;
		scores = PrefsArrays.GetIntArray("highScores" + k, -1, TableSize);
		names = PrefsArrays.GetStringArray("highScoreNames" + k, "DEFAULT", TableSize);
		System.Array.Resize(ref scores, TableSize);
		System.Array.Resize(ref names, TableSize);
		bool empty = true;
		foreach (int s in scores) if (s != -1) { empty = false; break; }
		if (empty)
			for (int i = 0; i < TableSize; i++) { scores[i] = DefaultScores[i]; names[i] = DefaultName; }
	}

	public static void ClearAll(GameMode mode)
	{
		Clear(mode);
		string k = ModeInfo.Of(mode).saveKey;
		PrefsArrays.DeleteKey("highScores" + k);
		PrefsArrays.DeleteKey("highScoreNames" + k);
	}
}
