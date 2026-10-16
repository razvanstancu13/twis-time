using UnityEngine;

// One line of the main menu (was menuItemLogic.js, on menuItem/menuX): its number on the page and the little
// spinning wheel next to it. The panel (menuPanel) is what the pointer hits; its text is menuText.
public class MenuItem : MonoBehaviour
{
	public int id;

	bool _animating;

	public void Spin(float secondsPerFrame)
	{
		if (_animating) return;
		_animating = true;
		StartCoroutine(Run(secondsPerFrame));
	}

	System.Collections.IEnumerator Run(float s)
	{
		yield return SpriteSheet.Play(GetComponent<Renderer>(), s);
		_animating = false;
	}

	public Renderer Panel => transform.parent.Find("menuPanel").GetComponent<Renderer>();
	public TextMesh Label => transform.parent.Find("menuText").GetComponent<TextMesh>();
}
