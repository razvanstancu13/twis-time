using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

// The game scene (was gameLogic.js, one class of 1500 lines). It owns the board on screen and the move sequence;
// the rules are in Board, the saved game and high scores in SaveGame, the buttons and menus in PauseMenu,
// the time bar in TimeBar, the per-mode settings in ModeInfo.
//
// A move: select a gem, then a neighbour; they swap. If that makes a chunk, chunks spin and disappear (big ones
// first), the gems above fall, new ones drop in, and again until no chunk is left; every extra chunk of the same
// move is worth 100 more. If not, they swap back (in the combo modes the first such swap is kept as a "combo"
// waiting for the next move instead).
public class GameController : MonoBehaviour
{
	public static GameController Instance { get; private set; }

	[Header("Board")]
	[FormerlySerializedAs("jType")] public GameObject[] gemPrefabs;          // one per colour
	[FormerlySerializedAs("aText")] public Texture[] spinZSheets;            // per colour, the sheet for the vanish spin
	[FormerlySerializedAs("effect")] public Transform explosion;
	public Transform selectionPlane, selectionPlane2;                         // the cursors on the selected gems
	public Transform cursorC, cursorC2;                                       // the cursors on a waiting combo

	[Header("HUD")]
	public LegacyGUIText scoreText;
	public LegacyGUIText comboText;

	[Header("Combo modes (start values)")]
	[FormerlySerializedAs("numberOfCombos")] public int combosLeft = 20;     // Combo 20
	[FormerlySerializedAs("nrCombos")] public int comboModeCombos = 1;       // Combo Mode
	[FormerlySerializedAs("scoreAdd")] public int comboModeNextAt;

	public GameMode Mode { get; private set; }
	public bool InputLocked { get; set; }
	public int Score => _score;

	readonly Board _board = new Board();
	readonly GameObject[] _gems = new GameObject[64];
	readonly List<GameObject> _dropping = new List<GameObject>();   // new gems dropping in, until the board is redrawn
	int[] _colorCount = new int[Board.Colors];
	int _score;
	bool _gameLocked, _timeOverOn, _continuing;
	int _selRow = -10, _selCol = -10;            // the selected gem (-10 = none)
	bool _comboWaiting;                          // a swap that made no chunk is held as a combo
	int _comboR1, _comboC1, _comboR2, _comboC2;
	SaveGame _save;
	ModeInfo _info;
	TimeBar _timeBar;
	PauseMenu _menu;
	AudioSource _music;

	static Vector3 CellPosition(int row, int col) => new Vector3(col - 4 + 0.1f * col, 1, row - 4 + 0.1f * row);
	Gem GemAt(int i) => _gems[i] ? _gems[i].GetComponent<Gem>() : null;

	void Awake()
	{
		Instance = this;
		Application.targetFrameRate = 60;
		TextManager.EnsureLoaded();
		Mode = (GameMode)PlayerPrefs.GetInt("gameMode", 0);
		_info = ModeInfo.Of(Mode);
		_gameLocked = false;

		// the free edition shipped only Time Trial's background and music; the other modes borrow them
		var timeTrial = ModeInfo.Of(GameMode.TimeTrial);
		var background = GameObject.Find("backgroundImage");
		if (background)
		{
			var tex = Resources.Load<Texture>(_info.background);
			if (!tex) tex = Resources.Load<Texture>(timeTrial.background);
			background.GetComponent<LegacyGUITexture>().texture = tex;
		}

		_music = GetComponent<AudioSource>();
		_music.clip = Resources.Load<AudioClip>(_info.music);
		if (!_music.clip) _music.clip = Resources.Load<AudioClip>(timeTrial.music);
		Music(GameOptions.Sound);

		if (!_info.timed)
			foreach (var n in new[] { "pBar", "pBarL", "pBarP" }) Destroy(GameObject.Find(n));
		if (ModeInfo.HasCombos(Mode))
		{
			Instantiate(Resources.Load<GameObject>("comboIcon"));
			comboText.enabled = true;
		}

		_save = new SaveGame(Mode);
		_save.Load();
		_continuing = _save.HasGame;
		if (_continuing)
		{
			_score = _save.score;
			System.Array.Copy(_save.board, _board.cells, 64);
			_colorCount = _save.colorCount;
			if (Mode == GameMode.Combo20) combosLeft = _save.combosLeft;
			if (Mode == GameMode.ComboMode) { comboModeCombos = _save.comboModeCombos; comboModeNextAt = _save.comboModeNextAt; }
		}
		if (_info.timed)
		{
			_timeBar = GameObject.Find("pBarL").GetComponent<TimeBar>();
			_timeBar.Setup(_info.timeBarSeconds, _continuing, _save.timeLeft);
		}
		_menu = GetComponent<PauseMenu>();
		_menu.Setup(this);
	}

	IEnumerator Start()
	{
		InputLocked = true;
		if (!_continuing) _board.FillRandom();
		StartCoroutine(DrawBoardAnimated());
		UpdateScore();
		yield return new WaitForSeconds(3);
		InputLocked = false;
	}

	void Update()
	{
		if (Mode == GameMode.Combo20) comboText.text = "x" + combosLeft;
		else if (Mode == GameMode.ComboMode) comboText.text = "x" + comboModeCombos;

		if (_info.timed && !_timeOverOn && !_gameLocked && !InputLocked && _timeBar.TimeOut)
		{
			_gameLocked = true;
			StartCoroutine(TimeOver());
		}

		var p = Pointer.Read();
		if (InputLocked) return;
		if (p.active) _menu.Handle(p);
		else if (p.multiTouch) _menu.ClearButtons(true);
	}

	// ---- pointer on the board ----

	// raycast from the pointer; true when it was over a gem
	public bool PressGemAt(Vector2 screenPoint)
	{
		if (Physics.Raycast(Camera.main.ScreenPointToRay(screenPoint), out RaycastHit hit, 100))
		{
			var gem = hit.transform.GetComponent<Gem>();
			if (gem) { gem.Pressed(); return true; }
		}
		return false;
	}

	public void OnGemPressed(int row, int col) => StartCoroutine(Select(row, col));

	public void ClearSelection()
	{
		if (_selRow == -10 && _selCol == -10) return;
		selectionPlane.position = new Vector3(-10, 0, -10);
		_selRow = _selCol = -10;
	}

	// ---- a move ----

	IEnumerator Select(int row, int col)
	{
		if (_selRow == row && _selCol == col) yield break;
		GameOptions.PlaySound("soundMenuClick");
		int pr = _selRow, pc = _selCol, sr = row, sc = col;
		bool neighbour = ((sr == pr + 1 || sr == pr - 1) && sc == pc) || ((sc == pc + 1 || sc == pc - 1) && sr == pr);
		if (!neighbour)
		{
			_selRow = sr; _selCol = sc;
			selectionPlane.position = _gems[sr * 8 + sc].transform.position;
			yield break;
		}

		InputLocked = true;
		selectionPlane2.position = _gems[sr * 8 + sc].transform.position;
		yield return new WaitForSeconds(0.1f);
		_board.Swap(pr, pc, sr, sc);
		GameOptions.PlaySound("soundSwap");
		AnimateSwap(pr, pc, sr, sc);
		selectionPlane.position = selectionPlane2.position = new Vector3(-10, 0, -10);
		yield return new WaitForSeconds(1);

		bool scored = _board.RowRun(pr, pc) > 2 || _board.ColRun(pr, pc) > 2 || _board.RowRun(sr, sc) > 2 || _board.ColRun(sr, sc) > 2;
		if (scored)
		{
			if (Mode == GameMode.Combo20)
			{
				combosLeft--;
				if (_comboWaiting) { ClearComboCursor(); _comboWaiting = false; }
			}
			if (Mode == GameMode.ComboMode && _comboWaiting)
			{
				comboModeCombos--;
				_comboWaiting = false;
				ClearComboCursor();
			}
			yield return Cascade();
			EndOfMoveChecks();
		}
		else if (Mode == GameMode.Combo20 || (Mode == GameMode.ComboMode && comboModeCombos > 0))
		{
			if (!_comboWaiting)
			{
				// keep this swap as a combo: the next move may complete it
				_comboWaiting = true;
				RedrawBoard();
				yield return null;
				cursorC.position = _gems[sr * 8 + sc].transform.position;
				cursorC2.position = _gems[pr * 8 + pc].transform.position;
				_comboR1 = pr; _comboC1 = pc; _comboR2 = sr; _comboC2 = sc;
				yield return null;
			}
			else
			{
				// a second swap that makes nothing: both go back
				_comboWaiting = false;
				ClearComboCursor();
				yield return SwapBack(pr, pc, sr, sc);
				yield return SwapBack(_comboR1, _comboC1, _comboR2, _comboC2);
				RedrawBoard();
				yield return null;
			}
		}
		else
			yield return SwapBack(pr, pc, sr, sc);

		if (!_gameLocked) InputLocked = false;
		_selRow = _selCol = -10;
	}

	IEnumerator SwapBack(int r1, int c1, int r2, int c2)
	{
		_board.Swap(r1, c1, r2, c2);
		GameOptions.PlaySound("soundSwapBack");
		AnimateSwap(r2, c2, r1, c1);
		yield return new WaitForSeconds(0.5f);
	}

	// chunks disappear, gems fall, new ones drop in, until the board has no chunk left
	IEnumerator Cascade()
	{
		int bonus = 0;
		yield return new WaitForSeconds(0.3f);
		while (_board.HasChunk())
		{
			foreach (bool big in new[] { true, false })
				while (_board.HasChunk(big))
				{
					var cells = _board.FindChunk(big);
					GameOptions.PlaySound("soundChunkRotate");
					foreach (int i in cells) GemAt(i).SpinZ(spinZSheets[_board.cells[i]]);
					yield return new WaitForSeconds(0.7f);
					RemoveChunk(cells, big);
					yield return null;
					_score += bonus;
					bonus += 100;
					UpdateScore();
				}
			yield return new WaitForSeconds(0.2f);
			foreach (var (from, to) in _board.Falls())
			{
				Tween.MoveTo(_gems[from], _gems[to].transform.position, 0.5f);
				GemAt(from).Spin(0.01f);
			}
			yield return new WaitForSeconds(0.5f);
			_board.Collapse();
			DropNewGems(_board.FillGaps());
			yield return new WaitForSeconds(0.5f);
			RedrawBoard();
			yield return null;
			if (Mode == GameMode.ComboMode && _score - comboModeNextAt >= 25000 && comboModeCombos < 2)
			{
				comboModeNextAt += 25000;     // one more combo for every 25000 points, at most 2 in hand
				comboModeCombos++;
			}
		}
	}

	void RemoveChunk(List<int> cells, bool big)
	{
		foreach (int i in cells)
		{
			var gem = GemAt(i);
			_board.cells[i] = Board.Empty;
			gem.Hide();
			_colorCount[gem.color]++;
			Explode(i);
		}
		if (Mode == GameMode.Survival) _timeBar.AddTime(cells.Count + (big ? 1 : 0));
		_score += Board.ChunkScore(cells.Count);
		GameOptions.PlaySound("soundExplode");
		GameOptions.Vibrate();
		UpdateScore();
	}

	void Explode(int i)
	{
		var p = _gems[i].transform.position;
		p.y = 2;
		Instantiate(explosion, p, Quaternion.Euler(90, 0, 0));
	}

	void EndOfMoveChecks()
	{
		bool timeOut = _info.timed && _timeBar.TimeOut;
		if (_board.HasMove() && !timeOut && combosLeft != 0) return;
		if ((_info.timed && !timeOut) || (Mode == GameMode.Combo20 && combosLeft > 0))
		{
			if (Mode != GameMode.Combo20) Restart();           // no move left, but time left: a new board
		}
		else
		{
			if (!(Mode == GameMode.ComboMode && comboModeCombos > 0)) { _gameLocked = true; StartCoroutine(TimeOver()); }
			if (Mode == GameMode.ComboMode && _score >= 500000) { _gameLocked = true; StartCoroutine(TimeOver()); }
		}
	}

	// the hint button: the first gem that can make a chunk spins
	public void ShowHint()
	{
		if (_board.FindMove(out int a, out _)) GemAt(a).Spin(0.001f);
	}

	// pausing with a combo waiting puts it back first
	public IEnumerator UndoPendingCombo()
	{
		if (!ModeInfo.HasCombos(Mode) || !_comboWaiting) yield break;
		_comboWaiting = false;
		ClearComboCursor();
		_board.Swap(_comboR1, _comboC1, _comboR2, _comboC2);
		AnimateSwap(_comboR2, _comboC2, _comboR1, _comboC1);
		yield return new WaitForSeconds(0.5f);
		RedrawBoard();
		yield return null;
	}

	void ClearComboCursor() => cursorC.position = cursorC2.position = new Vector3(-10, 0, -10);

	// ---- drawing the board ----

	void AnimateSwap(int r1, int c1, int r2, int c2)
	{
		int a = r1 * 8 + c1, b = r2 * 8 + c2;
		Vector3 pa = _gems[a].transform.position, pb = _gems[b].transform.position;
		GemAt(a).Spin(0.001f);
		GemAt(b).Spin(0.001f);
		Tween.MoveTo(_gems[a], pb, 0.25f);
		Tween.MoveTo(_gems[b], pa, 0.25f);
		(_gems[a], _gems[b]) = (_gems[b], _gems[a]);
	}

	GameObject MakeGem(int row, int col, Vector3 at)
	{
		var go = Instantiate(gemPrefabs[_board[row, col]], at, Quaternion.identity);
		go.GetComponent<Gem>().Set(row, col, _board[row, col]);
		return go;
	}

	void DropNewGems(List<int> cells)
	{
		foreach (int i in cells)
		{
			int row = i / 8, col = i % 8;
			var go = MakeGem(row, col, new Vector3(col - 4 + 0.1f * col, 1, -5 - col));
			Tween.MoveTo(go, CellPosition(row, col), 0.5f);
			go.GetComponent<Gem>().Spin(0.01f);
			_dropping.Add(go);
		}
	}

	// throws away every gem object and makes them again from the board, all in place
	void RedrawBoard()
	{
		foreach (var go in _dropping) Destroy(go);
		_dropping.Clear();
		for (int i = 0; i < 64; i++) { if (_gems[i]) Destroy(_gems[i]); _gems[i] = null; }
		for (int r = 0; r < 8; r++)
			for (int c = 0; c < 8; c++)
				if (_board[r, c] != Board.Empty) _gems[r * 8 + c] = MakeGem(r, c, CellPosition(r, c));
	}

	// the gems fly in from the right, one per frame
	IEnumerator DrawBoardAnimated()
	{
		yield return new WaitForSeconds(0.5f);
		GameOptions.PlaySound("soundRegenerate");
		for (int r = 0; r < 8; r++)
			for (int c = 0; c < 8; c++)
				if (_board[r, c] != Board.Empty)
				{
					_gems[r * 8 + c] = MakeGem(r, c, CellPosition(r, c) + new Vector3(10, 0, 0));
					Tween.MoveTo(_gems[r * 8 + c], CellPosition(r, c), 1);
					yield return null;
				}
	}

	// no move left: a new board. (In 2012 a continued game kept its stuck board here; it now always gets a new one.)
	void Restart()
	{
		InputLocked = true;
		foreach (var go in _dropping) Destroy(go);
		_dropping.Clear();
		for (int i = 0; i < 64; i++) { if (_gems[i]) Destroy(_gems[i]); _gems[i] = null; }
		_board.FillRandom();
		StartCoroutine(DrawBoardAnimated());
		InputLocked = false;
	}

	// ---- end of the game ----

	IEnumerator TimeOver()
	{
		InputLocked = true;
		_timeOverOn = true;
		int place = _save.AddHighScore(_score, SystemInfo.deviceName);
		StartCoroutine(Popup("gameOver", "tit_game_over", "goText", _info.timed ? "txt_game_over_timed" : "txt_game_over", new Vector3(1, 0, 0)));
		yield return new WaitForSeconds(3.5f);
		if (place != -1)
			StartCoroutine(Popup("newHighscore", "tit_new_high_score", "scoreText", "txt_new_high_score", new Vector3(-1, 0, 0), " " + _score));
		yield return new WaitForSeconds(3.5f);
		SaveGame.Clear(Mode);
		SceneManager.LoadScene(0);
	}

	// a panel slides in, stays 2 s, slides out to `away`
	IEnumerator Popup(string panel, string titleKey, string textChild, string textKey, Vector3 away, string extra = "")
	{
		yield return new WaitForSeconds(0.5f);
		var go = GameObject.Find(panel);
		go.transform.Find("titleText").GetComponent<LegacyGUIText>().text = TextManager.Get(titleKey);
		go.transform.Find(textChild).GetComponent<LegacyGUIText>().text = TextManager.Get(textKey) + extra;
		Tween.MoveTo(go, Vector3.zero, 1);
		yield return new WaitForSeconds(2);
		Tween.MoveTo(go, away, 1);
		yield return new WaitForSeconds(1);
	}

	// Quit from the pause menu: keep the game to continue it later
	public void SaveForLater()
	{
		_save.score = _score;
		_save.board = (int[])_board.cells.Clone();
		_save.colorCount = _colorCount;
		if (_info.timed) _save.timeLeft = _timeBar.TimeLeft;
		_save.combosLeft = combosLeft;
		_save.comboModeCombos = comboModeCombos;
		_save.comboModeNextAt = comboModeNextAt;
		_save.Save();
	}

	// ---- small things the menu uses ----

	public void PauseTimer(bool pause) { if (_timeBar) _timeBar.Pause(pause); }
	public void ScoreVisible(bool on) => scoreText.enabled = on;
	public void Music(bool on) { if (on) _music.Play(); else _music.Stop(); }
	void UpdateScore() => scoreText.text = "" + _score;

	public IEnumerator LockInput(float seconds)
	{
		InputLocked = true;
		yield return new WaitForSeconds(seconds);
		InputLocked = false;
	}
}
