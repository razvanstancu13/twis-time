using UnityEngine;

// The time bar of the timed modes (was progressBar.js, on pBarL): the liquid slides left, out of the screen;
// when it is all gone the time is up. Chunks in Survival push it back. Its x (pixels, 0 = full) is the time left
// that gets saved. The old code counted in Screen.width, which on the iPhone was the 640 of the layout.
[RequireComponent(typeof(LegacyGUITexture))]
public class TimeBar : MonoBehaviour
{
	LegacyGUITexture _bar;
	float _pixelsPerSecond;
	bool _paused;

	public bool TimeOut { get; private set; }
	public int TimeLeft => (int)_bar.pixelInset.x;

	float Width => LegacyGUI.Reference.x;

	public void Setup(int seconds, bool continuing, int savedTimeLeft)
	{
		_bar = GetComponent<LegacyGUITexture>();
		_pixelsPerSecond = Width / seconds;
		if (continuing) SetX(savedTimeLeft);
	}

	public void Pause(bool pause) => _paused = pause;

	public void AddTime(int seconds)
	{
		if (TimeOut) return;
		SetX(Mathf.Min(_bar.pixelInset.x + _pixelsPerSecond * seconds, 0));
	}

	void SetX(float x)
	{
		var r = _bar.pixelInset;
		r.x = x;
		_bar.pixelInset = r;
	}

	void Update()
	{
		if (_bar == null || _paused || TimeOut) return;
		SetX(_bar.pixelInset.x - _pixelsPerSecond * Time.deltaTime);
		if (_bar.pixelInset.x < -Width) TimeOut = true;
	}
}
