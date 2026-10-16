using UnityEngine;

// A GUI button the game handles itself (press highlights it, release over it triggers): its picture and the
// Resources textures for idle and pressed.
[System.Serializable]
public class HudButton
{
	public LegacyGUITexture image;
	public string idle, pressed;

	public HudButton(LegacyGUITexture image, string idle, string pressed) { this.image = image; this.idle = idle; this.pressed = pressed; }

	public bool Hit(Vector2 screenPoint) => image && image.HitTest(screenPoint);
	public void Idle() => Show(idle);
	public void Pressed() => Show(pressed);
	public void Show(string texture) { if (image) image.texture = Resources.Load<Texture>(texture); }

	// the label on it (child "buttonText")
	public void Label(string text)
	{
		var t = image ? image.transform.Find("buttonText") : null;
		var label = t ? t.GetComponent<LegacyGUIText>() : null;
		if (label) label.text = text;
	}
}
