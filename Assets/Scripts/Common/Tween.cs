using UnityEngine;

// Replaces iTween (2.0.45, 280 KB), of which the game used only iTween.MoveTo(object, position, seconds),
// with iTween's defaults: world position, "easeOutExpo", a new move cancels the object's previous one,
// and the object ends exactly on the target.
public class Tween : MonoBehaviour
{
	Vector3 _from, _to;
	float _time, _duration;

	public static void MoveTo(GameObject target, Vector3 position, float seconds)
	{
		if (!target) return;
		var t = target.GetComponent<Tween>();
		if (!t) t = target.AddComponent<Tween>();
		t._from = target.transform.position;
		t._to = position;
		t._time = 0;
		t._duration = Mathf.Max(seconds, 0.0001f);
		t.enabled = true;
	}

	static float EaseOutExpo(float a, float b, float t) => (b - a) * (-Mathf.Pow(2, -10 * t) + 1) + a;

	void Update()
	{
		_time += Time.deltaTime;
		float k = Mathf.Clamp01(_time / _duration);
		transform.position = k >= 1 ? _to : new Vector3(EaseOutExpo(_from.x, _to.x, k), EaseOutExpo(_from.y, _to.y, k), EaseOutExpo(_from.z, _to.z, k));
		if (k >= 1) enabled = false;
	}
}
