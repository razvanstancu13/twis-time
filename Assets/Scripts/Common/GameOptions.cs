using UnityEngine;

// Sound and vibration switches, kept in PlayerPrefs "options" as before ([0] sound, [1] vibration, 1 = on).
// Also plays the short sound effects (Resources/<name>), and vibrates where the device can.
public static class GameOptions
{
	const string Key = "options";

	static int[] Values
	{
		get
		{
			var v = PrefsArrays.GetIntArray(Key, 1, 3);
			if (v.Length < 3) { var w = new int[] { 1, 1, 1 }; v.CopyTo(w, 0); v = w; }
			return v;
		}
	}

	public static bool Sound { get => Values[0] == 1; set => Set(0, value); }
	public static bool Vibration { get => Values[1] == 1; set => Set(1, value); }

	static void Set(int i, bool on)
	{
		var v = Values;
		v[i] = on ? 1 : 0;
		PrefsArrays.SetIntArray(Key, v);
	}

	public static void PlaySound(string name)
	{
		if (!Sound) return;
		var clip = Resources.Load<AudioClip>(name);
		if (!clip) return;                              // some names in the code never had a file (e.g. soundMenuTransition)
		var go = new GameObject("Audio: " + name);
		var source = go.AddComponent<AudioSource>();
		source.clip = clip;
		source.Play();
		Object.Destroy(go, clip.length);
	}

	public static void Vibrate()
	{
#if UNITY_IOS || UNITY_ANDROID
		if (Vibration) Handheld.Vibrate();
#endif
	}
}
