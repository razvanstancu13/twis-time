using System.Collections;
using UnityEngine;

// The gems, the menu items and the menu wheel animate by walking a 5 x 5 sprite sheet through the material's
// texture offset (top row first). The three copies of that loop in the old scripts are this one.
// The game ran at 20 frames per second (the menu set Application.targetFrameRate = 20 and nothing changed it), and
// the waits in the game logic were tuned to that, so one picture lasts at least 1/20 s here too.
public static class SpriteSheet
{
	public const float FrameTime = 1f / 20f;
	const float Step = 0.2f;

	public static IEnumerator Play(Renderer r, float secondsPerFrame)
	{
		var wait = new WaitForSeconds(Mathf.Max(secondsPerFrame, FrameTime));
		var offset = new Vector2(0, 0.8f);
		while (offset.y >= 0)
		{
			r.material.SetTextureOffset("_MainTex", offset);
			yield return wait;
			offset.x += Step;
			if (offset.x >= 0.999f) { offset.x = 0; offset.y -= Step; }
		}
		r.material.SetTextureOffset("_MainTex", new Vector2(offset.x, 0.8f));
	}
}
