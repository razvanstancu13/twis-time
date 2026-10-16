using UnityEngine;

// Stand-in for Unity 4's GUIText (see LegacyGUI). Same fields; drawn glyph by glyph from the font texture.
// anchor: where the point sits on the text block (MiddleCenter = the block is centred on it);
// alignment: how the lines line up inside the block. Size 0 = the size the font was imported with.
public class LegacyGUIText : LegacyGUIElement
{
	[TextArea] public string text = "";
	public Font font;
	public int fontSize;
	public FontStyle fontStyle = FontStyle.Normal;
	public TextAnchor anchor = TextAnchor.UpperLeft;
	public TextAlignment alignment = TextAlignment.Left;
	public float lineSpacing = 1f;
	public Color color = Color.white;
	public Vector2 pixelOffset;

	int Size => font.dynamic ? (fontSize > 0 ? fontSize : (font.fontSize > 0 ? font.fontSize : 16)) : 0;

	public void RequestCharacters()
	{
		if (!string.IsNullOrEmpty(text) && font) font.RequestCharactersInTexture(text, Size, fontStyle);
	}

	public override void Draw()
	{
		if (string.IsNullOrEmpty(text) || !font) return;
		int size = Size;
		float scale = font.dynamic || fontSize <= 0 || font.fontSize <= 0 ? 1f : (float)fontSize / font.fontSize;
		font.RequestCharactersInTexture(text, size, fontStyle);       // normally already there (LegacyGUI asked first)

		string[] lines = text.Replace("\r", "").Split('\n');
		float lineHeight = font.lineHeight * lineSpacing * scale;
		float[] widths = new float[lines.Length];
		float blockWidth = 0;
		for (int i = 0; i < lines.Length; i++)
		{
			foreach (char ch in lines[i])
				if (font.GetCharacterInfo(ch, out CharacterInfo ci, size, fontStyle))
					widths[i] += ci.advance * scale;
			blockWidth = Mathf.Max(blockWidth, widths[i]);
		}
		float blockHeight = lines.Length * lineHeight;

		Vector3 p = transform.position;
		float ax = p.x * LegacyGUI.Reference.x + pixelOffset.x, ay = p.y * LegacyGUI.Reference.y + pixelOffset.y;
		int col = (int)anchor % 3, row = (int)anchor / 3;              // 0 left/upper, 1 centre/middle, 2 right/lower
		float left = ax - blockWidth * col * 0.5f;
		float top = ay + blockHeight * row * 0.5f;
		float ascent = (font.ascent > 0 ? font.ascent : font.lineHeight * 0.8f) * scale;

		font.material.SetPass(0);
		GL.Begin(GL.QUADS);
		GL.Color(color);
		for (int i = 0; i < lines.Length; i++)
		{
			float x = left + (alignment == TextAlignment.Center ? (blockWidth - widths[i]) / 2 : alignment == TextAlignment.Right ? blockWidth - widths[i] : 0);
			float baseline = top - ascent - i * lineHeight;
			foreach (char ch in lines[i])
			{
				if (!font.GetCharacterInfo(ch, out CharacterInfo ci, size, fontStyle)) continue;
				float x0 = x + ci.minX * scale, x1 = x + ci.maxX * scale;
				float y0 = baseline + ci.minY * scale, y1 = baseline + ci.maxY * scale;
				GL.TexCoord(ci.uvBottomLeft); GL.Vertex3(x0, y0, 0);
				GL.TexCoord(ci.uvTopLeft); GL.Vertex3(x0, y1, 0);
				GL.TexCoord(ci.uvTopRight); GL.Vertex3(x1, y1, 0);
				GL.TexCoord(ci.uvBottomRight); GL.Vertex3(x1, y0, 0);
				x += ci.advance * scale;
			}
		}
		GL.End();
	}
}
