// The five game modes, all playable (the 2012 free edition offered only Time Trial). The number is the one stored in
// PlayerPrefs "gameMode" and the item's place on the menu's mode page.
public enum GameMode
{
	Classic = 0,
	TimeTrial = 1,
	Survival = 2,
	Combo20 = 3,       // 20 combos to use; a combo = a swap that makes no chunk, kept until the next move
	ComboMode = 4,     // combos earned with points (one more every 25000, at most 2 at a time)
}

// Everything that differs per mode, in one place (the old code had an if-chain for each of these).
public class ModeInfo
{
	public string saveKey;        // suffix of the PlayerPrefs keys: save<k>, playerScore<k>, highScores<k>, ...
	public string background;     // Resources texture
	public string music;          // Resources clip
	public bool timed;            // has the time bar
	public int timeBarSeconds;    // how long the full bar lasts

	public static bool HasCombos(GameMode m) => m == GameMode.Combo20 || m == GameMode.ComboMode;

	public static ModeInfo Of(GameMode m)
	{
		switch (m)
		{
			case GameMode.TimeTrial: return new ModeInfo { saveKey = "Timed", background = "back2", music = "musicTime", timed = true, timeBarSeconds = 120 };
			case GameMode.Survival: return new ModeInfo { saveKey = "Survival", background = "back2", music = "musicSurvival", timed = true, timeBarSeconds = 30 };
			case GameMode.Combo20: return new ModeInfo { saveKey = "Combo", background = "back3", music = "musicCombo20" };
			case GameMode.ComboMode: return new ModeInfo { saveKey = "Combo2", background = "back3", music = "musicCombo" };
			default: return new ModeInfo { saveKey = "Original", background = "back1", music = "musicClassic" };
		}
	}
}
