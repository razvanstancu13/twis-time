using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// The main menu (was mainMenu.js). Pages of 3D menu items that fly in and out; the main page has the big wheel,
// the others a header with a sentence. The score table and the help / credits page are GUI panels that slide up.
// Play and Scores first ask for the game mode (the 2012 free edition was locked to Time Trial). The App Store and
// "Legal" (Apple licence) buttons are gone.
public class MainMenuController : MonoBehaviour
{
	enum Page { Main = 0, Play = 1, Continue = 2, Options = 3, ConfirmQuit = 4, ConfirmNew = 5, ScoreModes = 6, Scores = 7, ConfirmClear = 8, Help = 9 }

	// text keys of the items on each page (by Page number)
	static readonly string[][] Items =
	{
		new[] { "tit_play", "tit_help", "tit_options", "tit_scores" },
		new[] { "tit_original", "tit_time_trial", "tit_survival", "tit_combo_20", "tit_combo_mode" },
		new[] { "tit_new_game", "tit_continue" },
		new[] { "tit_sound", "tit_vibration", "tit_clear_data" },
		new[] { "tit_no", "tit_yes" },
		new[] { "tit_no", "tit_yes" },
		new[] { "tit_original", "tit_time_trial", "tit_survival", "tit_combo_20", "tit_combo_mode" },
		new string[0],
		new[] { "tit_no", "tit_yes" },
		new string[0],
	};

	public LegacyGUITexture backButton, creditsButton, menuHeader;
	public string[] menuTitles;            // header sentence per page (scene data)

	const int Back = 5;                     // the action number of the back button
	GameMode _mode = GameMode.TimeTrial;     // the mode picked on the Play / Scores page

	Page _page = Page.Main, _prevPage = Page.Main;
	bool _inputLocked, _hasContinue;
	readonly MenuItem[] _items = new MenuItem[5];
	int _itemCount;
	GameObject _wheel;
	AudioSource _music;
	int _lastPressed = -1;
	bool _backPressed, _creditsPressed;
	HudButton _back, _credits;

	void Awake()
	{
		Application.targetFrameRate = 60;
		TextManager.EnsureLoaded();
		_music = GetComponent<AudioSource>();
		if (GameOptions.Sound) _music.Play();
		_back = new HudButton(backButton, "bButtonMenuIdle", "bButtonMenuHigh");
		_credits = new HudButton(creditsButton, "menuPauseButtonIdle", "menuPauseButtonHigh");
	}

	IEnumerator Start()
	{
		_inputLocked = true;
		backButton.enabled = false;
		CreateItems(Page.Main);
		yield return new WaitForSeconds(0.1f);
		_wheel = Instantiate(Resources.Load<GameObject>("menuWheel"), new Vector3(6, 0, -3), Quaternion.identity);
		Tween.MoveTo(_wheel, new Vector3(0, 0, -3), 1);
		yield return FlyItemsIn(0.5f);
		_inputLocked = false;
	}

	// ---- what a choice does ----

	void Choose(int id)
	{
		bool animate = true;
		switch (_page)
		{
			case Page.Main:
				if (id == 0) Go(Page.Play);
				else if (id == 1) { animate = false; Go(Page.Help); StartCoroutine(ShowHelp()); }
				else if (id == 2) Go(Page.Options);
				else if (id == 3) Go(Page.ScoreModes);
				else return;
				break;
			case Page.Play:
				if (id >= 0 && id <= 4)
				{
					_mode = (GameMode)id;
					PlayerPrefs.SetInt("gameMode", id);
					_hasContinue = SaveGame.SavedScore(_mode) > 0;
					Go(Page.Continue);
				}
				else if (id == Back) Go(Page.Main);
				else return;
				break;
			case Page.ScoreModes:
				if (id >= 0 && id <= 4) { _mode = (GameMode)id; animate = false; Go(Page.Scores); StartCoroutine(ShowScores()); }
				else if (id == Back) Go(Page.Main);
				else return;
				break;
			case Page.Continue:
				if (id == 0)
				{
					if (_hasContinue) Go(Page.ConfirmNew);
					else { animate = false; NewGame(); }
				}
				else if (id == 1) { animate = false; if (_hasContinue) SceneManager.LoadScene(1); }
				else if (id == Back) Go(Page.Play);
				else return;
				break;
			case Page.Options:
				if (id == Back) Go(Page.Main);
				else if (id == 2) Go(Page.ConfirmClear);
				else if (id == 0 || id == 1)
				{
					animate = false;
					if (id == 0) { GameOptions.Sound = !GameOptions.Sound; if (GameOptions.Sound) _music.Play(); else _music.Stop(); }
					else { GameOptions.Vibration = !GameOptions.Vibration; if (GameOptions.Vibration) GameOptions.Vibrate(); }
					_items[id].Label.text = ItemText(Page.Options, id);
					StartCoroutine(LockBriefly());
				}
				else return;
				break;
			case Page.ConfirmQuit:
				if (id == 0) Go(Page.Main);
				else if (id == 1) Application.Quit();
				else return;
				break;
			case Page.ConfirmNew:
				if (id == 0) Go(Page.Continue);
				else if (id == 1) { animate = false; NewGame(); }
				else return;
				break;
			case Page.Scores:
				if (id != Back) return;
				animate = false; Go(Page.ScoreModes); StartCoroutine(HideScores());
				break;
			case Page.ConfirmClear:
				if (id == 0) Go(Page.Options);
				else if (id == 1) { foreach (GameMode m in System.Enum.GetValues(typeof(GameMode))) SaveGame.ClearAll(m); Go(Page.Options); }
				else return;
				break;
			case Page.Help:
				if (id != Back) return;
				animate = false; Go(Page.Main); StartCoroutine(HideHelp());
				break;
			default:
				return;
		}
		if (animate) StartCoroutine(ChangePage());
	}

	void Go(Page p) { _prevPage = _page; _page = p; }

	void NewGame()
	{
		SaveGame.Clear(_mode);
		SceneManager.LoadScene(1);
	}

	string ItemText(Page page, int i)
	{
		string t = TextManager.Get(Items[(int)page][i]);
		if (page == Page.Options && i != 2)
			t += " " + TextManager.Get((i == 0 ? GameOptions.Sound : GameOptions.Vibration) ? "tit_on" : "tit_off");
		return t;
	}

	// ---- page animations ----

	IEnumerator LockBriefly()
	{
		_inputLocked = true;
		yield return new WaitForSeconds(0.3f);
		_inputLocked = false;
	}

	IEnumerator ChangePage()
	{
		yield return HidePage(20);
		yield return ShowPage();
	}

	// the current items fly back; the wheel (main page) rolls out slowly or the header leaves
	IEnumerator HidePage(float wheelSeconds)
	{
		GameOptions.PlaySound("soundMenuTransitionDown");
		_inputLocked = true;
		backButton.enabled = false;
		if (_prevPage == Page.Main && _wheel) Tween.MoveTo(_wheel, new Vector3(6, 0, -3), wheelSeconds);
		else Tween.MoveTo(menuHeader.gameObject, new Vector3(-1, 0, 0), 1);
		for (int i = _itemCount - 1; i >= 0; i--)
		{
			Tween.MoveTo(_items[i].transform.parent.gameObject, _items[i].transform.parent.position + new Vector3(0, 0, 6), 1);
			_items[i].Spin(0.03f);
			yield return new WaitForSeconds(0.1f);
		}
		yield return new WaitForSeconds(0.5f);
		if (_prevPage == Page.Main && _wheel) Destroy(_wheel);
		for (int i = 0; i < _itemCount; i++) Destroy(_items[i].transform.parent.gameObject);
		_itemCount = 0;
	}

	// the items of the current page fly in; the wheel (main page) or the header with the page sentence comes in
	IEnumerator ShowPage()
	{
		GameOptions.PlaySound("soundMenuTransitionUp");
		CreateItems(_page);
		yield return null;
		if (_page == Page.Main)
		{
			_wheel = Instantiate(Resources.Load<GameObject>("menuWheel"), new Vector3(6, 0, -3), Quaternion.identity);
			Tween.MoveTo(_wheel, new Vector3(0, 0, -3), 1);
		}
		else
		{
			menuHeader.transform.Find("headerText").GetComponent<LegacyGUIText>().text = TextManager.Get(menuTitles[(int)_page]);
			Tween.MoveTo(menuHeader.gameObject, Vector3.zero, 1);
		}
		yield return FlyItemsIn();
		backButton.enabled = !(_page == Page.Main || _page == Page.ConfirmQuit || _page == Page.ConfirmNew || _page == Page.ConfirmClear);
		_inputLocked = false;
	}

	void CreateItems(Page page)
	{
		_itemCount = Items[(int)page].Length;
		for (int i = 0; i < _itemCount; i++)
		{
			var go = Instantiate(Resources.Load<GameObject>("menuItem"), new Vector3(0.85f, 0, 10.25f + 1.1f * i), Quaternion.identity);
			var item = go.GetComponentInChildren<MenuItem>();
			item.id = i;
			if (page == Page.Continue && i == 1 && !_hasContinue)
				item.Panel.material.mainTexture = Resources.Load<Texture>("buttonLocked");
			item.Label.text = ItemText(page, i);
			_items[i] = item;
		}
	}

	IEnumerator FlyItemsIn(float thenWait = 1)
	{
		for (int i = 0; i < _itemCount; i++)
		{
			Tween.MoveTo(_items[i].transform.parent.gameObject, _items[i].transform.parent.position - new Vector3(0, 0, 10), 1);
			_items[i].Spin(0.03f);
			yield return new WaitForSeconds(0.1f);
		}
		yield return new WaitForSeconds(thenWait);
	}

	IEnumerator ShowScores()
	{
		GameOptions.PlaySound("soundMenuTransition");
		yield return HidePage(1);
		SaveGame.ReadTable(_mode, out int[] scores, out string[] names);
		var ui = GameObject.Find("scoreUI").transform;
		for (int i = 0; i < SaveGame.TableSize; i++)
		{
			string n = i == 9 ? "X" : (i + 1).ToString();
			string name = names[i] ?? "";
			ui.Find("name" + n).GetComponent<LegacyGUIText>().text = name.Length > 10 ? name.Substring(0, 10) : name;
			ui.Find("score" + n).GetComponent<LegacyGUIText>().text = "" + scores[i];
		}
		Tween.MoveTo(ui.gameObject, new Vector3(0, 0, 1), 2);
		yield return new WaitForSeconds(2);
		backButton.enabled = true;
		_inputLocked = false;
	}

	IEnumerator HideScores()
	{
		_inputLocked = true;
		backButton.enabled = false;
		Tween.MoveTo(GameObject.Find("scoreUI"), new Vector3(0, -1, 1), 1);
		yield return new WaitForSeconds(0.5f);
		yield return ShowPage();
	}

	static LegacyGUIText HelpText => GameObject.Find("helpMenu").transform.Find("helpText").GetComponent<LegacyGUIText>();

	IEnumerator ShowHelp()
	{
		yield return HidePage(20);
		HelpText.text = TextManager.Get("txt_help");
		_credits.Label(TextManager.Get("tit_credits"));
		Tween.MoveTo(GameObject.Find("helpMenu"), new Vector3(0, 0, 1), 2);
		yield return new WaitForSeconds(2);
		backButton.enabled = true;
		_inputLocked = false;
	}

	IEnumerator HideHelp()
	{
		_inputLocked = true;
		backButton.enabled = false;
		Tween.MoveTo(GameObject.Find("helpMenu"), new Vector3(0, -1, 1), 1);
		yield return new WaitForSeconds(0.5f);
		_credits.Label(TextManager.Get("tit_credits"));
		HelpText.text = TextManager.Get("txt_help");
		HelpText.alignment = TextAlignment.Left;
		yield return ShowPage();
	}

	// ---- pointer (finger or mouse, as touch) ----

	void Update()
	{
		var p = Pointer.Read();
		if (_inputLocked) return;
		if (p.multiTouch) { ClearButtons(); return; }
		if (!p.active) return;

		if (backButton.enabled && _back.Hit(p.position))
		{
			if (p.Ended && _backPressed) { _back.Idle(); _backPressed = false; Choose(Back); }
			else if (p.Began) { _back.Pressed(); _backPressed = true; GameOptions.PlaySound("soundMenuClick"); }
		}
		else if (creditsButton.enabled && _credits.Hit(p.position))
		{
			if (p.Ended && _creditsPressed) { _credits.Idle(); _creditsPressed = false; ToggleCredits(); }
			else if (p.Began) { _credits.Pressed(); _creditsPressed = true; GameOptions.PlaySound("soundMenuClick"); }
		}
		else if (Physics.Raycast(Camera.main.ScreenPointToRay(p.position), out RaycastHit hit, 100))
		{
			var item = hit.transform.parent ? hit.transform.parent.GetComponentInChildren<MenuItem>() : null;
			if (item && !(_page == Page.Continue && !_hasContinue && item.id == 1))
			{
				var panel = hit.transform.GetComponent<Renderer>();
				if (p.Ended)
				{
					panel.material.mainTexture = Resources.Load<Texture>("buttonIdle");
					if (_lastPressed == item.id) { _lastPressed = -1; Choose(item.id); }
				}
				else if (p.Began)
				{
					ClearButtons();
					panel.material.mainTexture = Resources.Load<Texture>("buttonHigh");
					GameOptions.PlaySound("soundMenuClick");
					_lastPressed = item.id;
				}
				else if (_lastPressed != item.id) ClearButtons();
			}
		}
		else ClearButtons();

		if (p.Ended) ClearButtons();
	}

	void ToggleCredits()
	{
		bool showCredits = _credits.image.transform.Find("buttonText").GetComponent<LegacyGUIText>().text == TextManager.Get("tit_credits");
		_credits.Label(TextManager.Get(showCredits ? "tit_help" : "tit_credits"));
		HelpText.text = TextManager.Get(showCredits ? "txt_credits" : "txt_help");
		HelpText.alignment = showCredits ? TextAlignment.Center : TextAlignment.Left;
	}

	void ClearButtons()
	{
		if (_page != Page.Scores && _page != Page.Help && !_inputLocked)
			for (int i = 0; i < _itemCount; i++)
				if (!(_page == Page.Continue && i == 1 && !_hasContinue))
					_items[i].Panel.material.mainTexture = Resources.Load<Texture>("buttonIdle");
		if (backButton.enabled) _back.Idle();
		_credits.Idle();
		_lastPressed = -1;
		_backPressed = _creditsPressed = false;
	}
}
