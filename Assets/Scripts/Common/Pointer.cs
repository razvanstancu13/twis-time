using UnityEngine;

// One finger or the mouse, as a touch: the game was written for touch (Began / Moved / Stationary / Ended).
// In 2012 the mouse only half worked (editor debugging); here the mouse drives exactly the same touch code.
public struct Pointer
{
	public bool active;          // exactly one finger, or the mouse button is involved this frame
	public bool multiTouch;      // more than one finger (the game then clears its buttons)
	public TouchPhase phase;
	public Vector2 position;

	static Vector2 _lastMouse;

	public static Pointer Read()
	{
		if (Input.touchCount == 1)
		{
			var t = Input.GetTouch(0);
			return new Pointer { active = true, phase = t.phase, position = t.position };
		}
		if (Input.touchCount > 1)
			return new Pointer { multiTouch = true };

		Vector2 m = Input.mousePosition;
		var p = new Pointer { position = m };
		if (Input.GetMouseButtonDown(0)) { p.active = true; p.phase = TouchPhase.Began; }
		else if (Input.GetMouseButtonUp(0)) { p.active = true; p.phase = TouchPhase.Ended; }
		else if (Input.GetMouseButton(0)) { p.active = true; p.phase = m != _lastMouse ? TouchPhase.Moved : TouchPhase.Stationary; }
		_lastMouse = m;
		return p;
	}

	public bool Began => active && phase == TouchPhase.Began;
	public bool Ended => active && phase == TouchPhase.Ended;
}
