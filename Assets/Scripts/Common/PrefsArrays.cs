using UnityEngine;

// The part of PlayerPrefsX (a 2012 UnityScript helper) that the game uses: int and string arrays in PlayerPrefs.
// Get(key, default, size): the stored array, or `size` copies of `default` when the key is not there.
public static class PrefsArrays
{
	const char Sep = '\u001f';      // unit separator: cannot appear in a name typed on a keyboard

	public static void SetIntArray(string key, int[] values) => PlayerPrefs.SetString(key, string.Join(",", values));

	public static int[] GetIntArray(string key, int defaultValue, int defaultSize)
	{
		if (!PlayerPrefs.HasKey(key)) return Filled(defaultValue, defaultSize);
		var parts = PlayerPrefs.GetString(key).Split(',');
		var result = new int[parts.Length];
		for (int i = 0; i < parts.Length; i++)
			result[i] = int.TryParse(parts[i], out int v) ? v : defaultValue;
		return result;
	}

	public static void SetStringArray(string key, string[] values) => PlayerPrefs.SetString(key, string.Join(Sep.ToString(), values));

	public static string[] GetStringArray(string key, string defaultValue, int defaultSize)
	{
		if (!PlayerPrefs.HasKey(key)) { var r = new string[defaultSize]; for (int i = 0; i < r.Length; i++) r[i] = defaultValue; return r; }
		return PlayerPrefs.GetString(key).Split(Sep);
	}

	public static void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);

	static int[] Filled(int value, int size)
	{
		var r = new int[size];
		for (int i = 0; i < size; i++) r[i] = value;
		return r;
	}
}
