using UnityEngine;

// Stand-in for Unity 4's GUITexture (see LegacyGUI). Same fields and the same HitTest.
public class LegacyGUITexture : LegacyGUIElement
{
	public Texture texture;
	public Color color = new Color(0.5f, 0.5f, 0.5f, 0.5f);      // 0.5 grey = the texture unchanged
	public Rect pixelInset;

	// the area in the 640 x 960 frame, y up (what GUITexture.GetScreenRect gave on the iPhone)
	public Rect ReferenceRect()
	{
		Vector3 p = transform.position, s = transform.lossyScale;
		float w = LegacyGUI.Reference.x, h = LegacyGUI.Reference.y;
		return new Rect(p.x * w - s.x * w / 2 + pixelInset.x, p.y * h - s.y * h / 2 + pixelInset.y,
		                s.x * w + pixelInset.width, s.y * h + pixelInset.height);
	}

	// like GUITexture.HitTest: is the screen point (Input.mousePosition, a touch position) on the texture?
	public bool HitTest(Vector2 screenPoint) => ReferenceRect().Contains(LegacyGUI.ScreenToReference(screenPoint));

	public override void Draw()
	{
		if (!texture) return;
		Rect r = ReferenceRect();
		// pixel matrix is y up, DrawTexture fills the rect from its top edge: hand it the rect flipped
		Graphics.DrawTexture(new Rect(r.x, r.yMax, r.width, -r.height), texture, new Rect(0, 0, 1, 1), 0, 0, 0, 0, color);
	}
}
