using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

// One gem on the board (was gemLogic.js): where it is, its colour, its two spin animations, hide / show.
// Clicked through its collider: GameController raycasts and calls Pressed().
public class Gem : MonoBehaviour
{
	[FormerlySerializedAs("idx")] public int row;
	[FormerlySerializedAs("idy")] public int col;
	public int color;

	bool _animating;
	Renderer _renderer;

	Renderer Rend => _renderer ? _renderer : (_renderer = GetComponent<Renderer>());

	public void Set(int row, int col, int color)
	{
		this.row = row;
		this.col = col;
		this.color = color;
	}

	public void Pressed()
	{
		if (!_animating && GameController.Instance)
			GameController.Instance.OnGemPressed(row, col);
	}

	// spin around Y (selection, swap, fall, hint); ignored while another spin runs
	public void Spin(float secondsPerFrame = 0) { if (!_animating) _running = StartCoroutine(Run(SpriteSheet.Play(Rend, secondsPerFrame))); }

	// spin around Z, on the second sprite sheet of its colour (just before the chunk disappears). It cuts off a spin
	// still running: the gem just swapped in is usually still finishing its swap spin (25 pictures at 1/20 s) when its
	// chunk starts, and it must spin together with the others (in 2012 it was skipped and stayed still).
	public void SpinZ(Texture sheet)
	{
		if (_running != null) StopCoroutine(_running);
		_animating = false;
		if (sheet) Rend.material.mainTexture = sheet;
		_running = StartCoroutine(Run(SpriteSheet.Play(Rend, 0)));
	}

	Coroutine _running;

	IEnumerator Run(IEnumerator animation)
	{
		_animating = true;
		yield return animation;
		_animating = false;
		_running = null;
	}

	public void Hide() { foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false; }
	public void Show() { foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = true; }
}
