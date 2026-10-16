using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// The in-game buttons and menus (was part of gameLogic.js): the pause and hint buttons, the pause menu (restart,
// help, sound, vibration, quit), the yes / no confirmation and the help page. The panels slide in and out.
// Buttons work like on the phone: press highlights, release over the same button triggers.
public class PauseMenu : MonoBehaviour
{
	public LegacyGUITexture pauseButton, hintButton;
	public LegacyGUITexture restartButton, helpButton, soundButton, vibrationButton, quitButton;
	public LegacyGUITexture yesButton, noButton, helpBackButton;
	public LegacyGUITexture soundLight, vibrationLight;

	public bool Paused { get; private set; }

	GameController _game;
	int _lastPressed = -1;
	bool _confirmRestart, _helpOpen;

	HudButton _pause, _hint, _restart, _help, _sound, _vibration, _quit, _yes, _no, _helpBack;

	const string MenuIdle = "menuPauseButtonIdle", MenuPressed = "menuPauseButtonHigh";

	static GameObject Find(string name) => GameObject.Find(name);
	static Transform Child(string panel, string child) { var p = Find(panel); return p ? p.transform.Find(child) : null; }
	static T Part<T>(string panel, string child) where T : Component { var t = Child(panel, child); return t ? t.GetComponent<T>() : null; }

	public void Setup(GameController game)
	{
		_game = game;
		_pause = new HudButton(pauseButton, "pButton", "pButtonHigh");
		_hint = new HudButton(hintButton, "hButton", "hButtonHigh");
		_restart = new HudButton(restartButton, MenuIdle, MenuPressed);
		_help = new HudButton(helpButton, MenuIdle, MenuPressed);
		_sound = new HudButton(soundButton, MenuIdle, MenuPressed);
		_vibration = new HudButton(vibrationButton, MenuIdle, MenuPressed);
		_quit = new HudButton(quitButton, MenuIdle, MenuPressed);
		_yes = new HudButton(yesButton, MenuIdle, MenuPressed);
		_no = new HudButton(noButton, MenuIdle, MenuPressed);
		_helpBack = new HudButton(helpBackButton, "bButton", "bButtonHigh");

		if (GameOptions.Sound) soundLight.texture = Resources.Load<Texture>("lightOn");
		if (GameOptions.Vibration) vibrationLight.texture = Resources.Load<Texture>("lightOn");
		_vibration.Label(TextManager.Get("tit_vibration_ingame"));
		_restart.Label(TextManager.Get("tit_restart"));
		_help.Label(TextManager.Get("tit_help_ingame"));
		_sound.Label(TextManager.Get("tit_sound_ingame"));
		_quit.Label(TextManager.Get("tit_quit"));
		_yes.Label(TextManager.Get("tit_yes"));
		_no.Label(TextManager.Get("tit_no"));
	}

	// one pointer event (finger or mouse) while the game takes input
	public void Handle(Pointer p)
	{
		if (_pause.Hit(p.position))
		{
			if (p.Ended && _lastPressed == 1)
			{
				_pause.Idle();
				if (!Paused) { Paused = true; StartCoroutine(Pause()); }
				else StartCoroutine(Unpause());
				_lastPressed = -1;
			}
			else if (p.Began)
			{
				_pause.Show(Paused ? "bButtonHigh" : "pButtonHigh");
				_lastPressed = 1;
				GameOptions.PlaySound("soundMenuClick");
			}
		}
		else if (_hint.Hit(p.position))
		{
			if (p.Ended && _lastPressed == 2 && !Paused)
			{
				_hint.Idle();
				_game.ShowHint();
				_lastPressed = -1;
			}
			else if (p.Began && !Paused)
			{
				_hint.Pressed();
				_lastPressed = 2;
				GameOptions.PlaySound("soundMenuClick");
			}
		}
		else if (!Paused)
		{
			// the board: pressing or dragging over a gem
			if (p.phase != TouchPhase.Stationary && p.phase != TouchPhase.Ended && !_game.PressGemAt(p.position))
				ClearButtons(true);
		}
		else if (Button(p, _restart, 3)) { _confirmRestart = true; Part<LegacyGUIText>("confirmMenu", "confirmText").text = TextManager.Get("txt_confirm_new"); StartCoroutine(PopConfirm()); }
		else if (Button(p, _help, 4)) { _helpOpen = true; StartCoroutine(PopHelp()); }
		else if (Button(p, _sound, 5)) ToggleSound();
		else if (Button(p, _vibration, 6)) ToggleVibration();
		else if (Button(p, _quit, 7)) { Part<LegacyGUIText>("confirmMenu", "confirmText").text = TextManager.Get("txt_confirm_quit"); StartCoroutine(PopConfirm()); }
		else if (!_helpOpen && Button(p, _yes, 8)) Yes();
		else if (!_helpOpen && Button(p, _no, 9)) { _confirmRestart = false; StartCoroutine(UnpopConfirm()); }
		else if (_helpOpen && Button(p, _helpBack, 10)) { _helpOpen = false; StartCoroutine(UnpopHelp()); }
		else if (!HitsAny(p.position))
			ClearButtons(true);

		if (p.Ended) ClearButtons(false);
	}

	// over a button (even one that does nothing right now): the old code then left the highlights alone
	bool HitsAny(Vector2 pos) => _restart.Hit(pos) || _help.Hit(pos) || _sound.Hit(pos) || _vibration.Hit(pos) || _quit.Hit(pos) ||
	                             _yes.Hit(pos) || _no.Hit(pos) || _helpBack.Hit(pos);

	// press: highlight and remember; release over the same button: true (the caller acts)
	bool Button(Pointer p, HudButton b, int id)
	{
		if (!b.Hit(p.position)) return false;
		if (p.Began)
		{
			b.Pressed();
			_lastPressed = id;
			GameOptions.PlaySound("soundMenuClick");
		}
		else if (p.Ended)
		{
			b.Idle();
			if (_lastPressed == id) { _lastPressed = -1; return true; }
		}
		return false;
	}

	public void ClearButtons(bool clearSelection)
	{
		if (_game.InputLocked) return;
		if (Paused)
		{
			foreach (var b in new[] { _restart, _help, _sound, _vibration, _quit, _yes, _no }) b.Idle();
			_pause.Show("bButton");
			_helpBack.Show("bButton");
		}
		else
		{
			if (clearSelection) _game.ClearSelection();
			_hint.Idle();
			_pause.Idle();
		}
		_lastPressed = -1;
	}

	void ToggleSound()
	{
		GameOptions.Sound = !GameOptions.Sound;
		soundLight.texture = Resources.Load<Texture>(GameOptions.Sound ? "lightOn" : "lightOff");
		_game.Music(GameOptions.Sound);
		StartCoroutine(_game.LockInput(0.3f));
	}

	void ToggleVibration()
	{
		GameOptions.Vibration = !GameOptions.Vibration;
		if (GameOptions.Vibration) GameOptions.Vibrate();
		vibrationLight.texture = Resources.Load<Texture>(GameOptions.Vibration ? "lightOn" : "lightOff");
		StartCoroutine(_game.LockInput(0.3f));
	}

	void Yes()
	{
		if (_confirmRestart)
		{
			SaveGame.Clear(_game.Mode);
			SceneManager.LoadScene(1);
		}
		else
		{
			_game.SaveForLater();
			SceneManager.LoadScene(0);
		}
	}

	static void Slide(string panel, Vector3 to) { var go = Find(panel); if (go) Tween.MoveTo(go, to, 0.5f); }
	static void Enable(string panel, string child, bool on)
	{
		var t = Child(panel, child);
		if (!t) return;
		foreach (var e in t.GetComponents<LegacyGUIElement>()) e.enabled = on;
	}

	IEnumerator Pause()
	{
		_game.InputLocked = true;
		yield return _game.UndoPendingCombo();
		_game.ClearSelection();
		_game.PauseTimer(true);
		_pause.Show("bButton");
		Enable("pauseMenu", "hintButton", false);
		_game.ScoreVisible(false);
		Slide("pauseMenu", new Vector3(0, -0.042f, 1));
		yield return new WaitForSeconds(0.5f);
		_game.InputLocked = false;
	}

	IEnumerator Unpause()
	{
		_game.InputLocked = true;
		_pause.Show("pButton");
		Enable("pauseMenu", "hintButton", true);
		Slide("pauseMenu", new Vector3(0, -0.76f, 0));
		yield return new WaitForSeconds(0.5f);
		_game.PauseTimer(false);
		_game.ScoreVisible(true);
		_game.InputLocked = false;
		Paused = false;
	}

	IEnumerator PopConfirm()
	{
		_game.InputLocked = true;
		Slide("pauseMenu", new Vector3(0, -1, 0));
		yield return new WaitForSeconds(0.5f);
		Slide("confirmMenu", new Vector3(0, -0.023f, 0));
		yield return new WaitForSeconds(0.5f);
		_game.InputLocked = false;
	}

	IEnumerator UnpopConfirm()
	{
		_game.InputLocked = true;
		Slide("confirmMenu", new Vector3(0, -1, 0));
		yield return new WaitForSeconds(0.5f);
		Slide("pauseMenu", new Vector3(0, -0.023f, 0));
		yield return new WaitForSeconds(0.5f);
		_game.InputLocked = false;
	}

	// the help page reuses the confirmation panel: its yes / no and question are hidden, the help text and a back button shown
	void ShowHelpParts(bool help)
	{
		Enable("confirmMenu", "confirmText", !help);
		Enable("confirmMenu", "yesButton", !help);
		Enable("confirmMenu", "yesButton/buttonText", !help);
		Enable("confirmMenu", "noButton", !help);
		Enable("confirmMenu", "noButton/buttonText", !help);
		Enable("confirmMenu", "helpBackButton", help);
		Enable("confirmMenu", "helpText", help);
	}

	IEnumerator PopHelp()
	{
		_game.InputLocked = true;
		Slide("pauseMenu", new Vector3(0, -1, 0));
		yield return new WaitForSeconds(0.5f);
		ShowHelpParts(true);
		Part<LegacyGUIText>("confirmMenu", "helpText").text = TextManager.Get("txt_help");
		Slide("confirmMenu", new Vector3(0, -0.023f, 0));
		yield return new WaitForSeconds(0.5f);
		_game.InputLocked = false;
	}

	IEnumerator UnpopHelp()
	{
		_game.InputLocked = true;
		Slide("confirmMenu", new Vector3(0, -1, 0));
		yield return new WaitForSeconds(0.5f);
		ShowHelpParts(false);
		Slide("pauseMenu", new Vector3(0, -0.023f, 0));
		yield return new WaitForSeconds(0.5f);
		_game.InputLocked = false;
	}
}
