using UnityEngine;

// The piece that follows the end of the time bar (was pstBar.js, on pBarP): always one bar width right of it.
[RequireComponent(typeof(LegacyGUITexture))]
public class TimeBarTail : MonoBehaviour
{
	public LegacyGUITexture bar;

	LegacyGUITexture _self;

	void Start()
	{
		_self = GetComponent<LegacyGUITexture>();
		if (!bar)
		{
			var go = GameObject.Find("pBarL");
			if (go) bar = go.GetComponent<LegacyGUITexture>();
		}
	}

	void Update()
	{
		if (!bar) return;
		var r = _self.pixelInset;
		r.x = bar.pixelInset.x + LegacyGUI.Reference.x;
		_self.pixelInset = r;
	}
}
