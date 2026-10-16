using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Unity 4 drew GUITexture / GUIText elements with a GUILayer on each camera, right after that camera rendered,
// only the elements on layers the camera sees. The game relies on it: the back camera draws the background and the
// board tiles, the main camera then draws the 3D gems and after them the score, buttons and menus.
// These classes do the same with LegacyGUITexture / LegacyGUIText (Unity 6 removed the old ones):
// - a camera with a LegacyGUILayer draws, after rendering, the enabled elements its culling mask sees: first the
//   textures, then the texts, each lowest z first;
// - an element's position is a point on the screen (0..1), pixelInset / pixelOffset are pixels from it, y up,
//   in the 640 x 960 frame the game was made for (iPhone 4, portrait);
// - that frame is kept at 2:3 on any window: every camera renders into the same centred rectangle (black bars
//   around it), so the 3D gems stay on their GUI tiles.
public static class LegacyGUI
{
	public static readonly Vector2 Reference = new Vector2(640, 960);

	static readonly List<LegacyGUIElement> Elements = new List<LegacyGUIElement>();
	static readonly List<LegacyGUIElement> Sorted = new List<LegacyGUIElement>();

	public static void Register(LegacyGUIElement e) { if (!Elements.Contains(e)) Elements.Add(e); }
	public static void Unregister(LegacyGUIElement e) { Elements.Remove(e); }

	// the 2:3 rectangle on the screen, in pixels, origin bottom-left
	public static Rect ViewRect
	{
		get
		{
			float w = Screen.width, h = Screen.height, aspect = Reference.x / Reference.y;
			if (w / h > aspect) { float vw = h * aspect; return new Rect((w - vw) / 2, 0, vw, h); }
			float vh = w / aspect;
			return new Rect(0, (h - vh) / 2, w, vh);
		}
	}

	// a screen point (Input.mousePosition, a touch) -> the 640 x 960 frame
	public static Vector2 ScreenToReference(Vector2 p)
	{
		Rect v = ViewRect;
		return new Vector2((p.x - v.x) * Reference.x / v.width, (p.y - v.y) * Reference.y / v.height);
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	static void Init()
	{
		Camera.onPreCull += c => { if (c.name != BarsCamera) c.pixelRect = ViewRect; };
		Camera.onPostRender += Draw;
		SceneManager.sceneLoaded += (s, m) => AddBarsCamera();
	}

	const string BarsCamera = "LetterboxBars";

	// clears the whole window to black first, so whatever is outside the 2:3 rectangle is black
	static void AddBarsCamera()
	{
		if (GameObject.Find(BarsCamera)) return;
		var cam = new GameObject(BarsCamera).AddComponent<Camera>();
		cam.depth = -100;
		cam.cullingMask = 0;
		cam.clearFlags = CameraClearFlags.SolidColor;
		cam.backgroundColor = Color.black;
	}

	static int _preparedFrame = -1;

	static void Draw(Camera cam)
	{
		if (!cam.GetComponent<LegacyGUILayer>()) return;
		// a font texture is rebuilt when a text asks for characters it does not hold yet, which spoils every text drawn
		// before in that frame: ask for all characters of all texts once, before the first one is drawn
		if (_preparedFrame != Time.frameCount)
		{
			_preparedFrame = Time.frameCount;
			foreach (var e in Elements)
				if (e && e.isActiveAndEnabled && e is LegacyGUIText t) t.RequestCharacters();
		}
		Sorted.Clear();
		foreach (var e in Elements)
			if (e && e.isActiveAndEnabled && (cam.cullingMask & (1 << e.gameObject.layer)) != 0)
				Sorted.Add(e);
		if (Sorted.Count == 0) return;
		// Unity 4's GUILayer drew all GUITextures (by z), then all GUITexts (by z): a text is never under a picture.
		// The game counts on it (the score sits on a black box placed higher; it hides the score itself when paused).
		Sorted.Sort((a, b) =>
		{
			int byKind = (a is LegacyGUIText).CompareTo(b is LegacyGUIText);
			return byKind != 0 ? byKind : a.transform.position.z.CompareTo(b.transform.position.z);
		});
		GL.PushMatrix();
		GL.LoadPixelMatrix(0, Reference.x, 0, Reference.y);      // draw in the 640 x 960 frame; the camera's viewport scales it
		foreach (var e in Sorted)
			e.Draw();
		GL.PopMatrix();
	}
}

// base of LegacyGUITexture and LegacyGUIText
public abstract class LegacyGUIElement : MonoBehaviour
{
	protected virtual void OnEnable() => LegacyGUI.Register(this);
	protected virtual void OnDisable() => LegacyGUI.Unregister(this);
	public abstract void Draw();
}
