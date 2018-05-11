using System.Collections;
using DoozyUI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroController : MonoBehaviour
{

	public TextMeshProUGUI infoText;
	public AudioSource introAudioSource;
	private bool loaded;
	private bool fadingOut;
	private bool textFadingIn;
	
	private void Update() {
		if (CytoidApplication.IsReloadingLevels)
		{
			if (CytoidApplication.LoadingLevelIndex == 0)
			{
				infoText.SetText("LOADING");
			}
			else
			{
				infoText.SetText("LOADING " + CytoidApplication.LoadingLevelIndex + "/" +
				                 CytoidApplication.TotalLevelsToLoad + "");
			}
		}
		else
		{
			if (!loaded)
			{
				loaded = true;
				StartCoroutine(TextAnim());
				introAudioSource.Play();
			}
		}
		if (loaded && !fadingOut)
		{
			infoText.text = "TOUCH TO START";
			if (Input.GetMouseButtonDown(0))
			{
				fadingOut = true;
				StopAllCoroutines();
				UIManager.ShowUiElement("Mask", "Intro");
				StartCoroutine(FadeOutSound());
				StartCoroutine(StartLevelSelection());
			}
		}
	}
	
	private IEnumerator TextAnim()
	{
		if (textFadingIn)
		{
			infoText.AlterColor(a: 0.01f);
			if (infoText.color.a >= 1)
			{
				textFadingIn = false;
			}
		}
		else
		{
			infoText.AlterColor(a: -0.01f);
			if (infoText.color.a <= 0)
			{
				textFadingIn = true;
				yield return new WaitForSeconds(0.2f);
				yield return StartCoroutine(TextAnim());
			}
		}
		yield return new WaitForSeconds(0.01f);
		yield return StartCoroutine(TextAnim());
	}

	private IEnumerator StartLevelSelection()
	{
		yield return new WaitForSeconds(3f);
		SceneManager.LoadScene("LevelSelection");
	}
	
	private IEnumerator FadeOutSound()
	{
		introAudioSource.volume -= 0.01f;
		yield return new WaitForSeconds(0.01f);
		yield return StartCoroutine(FadeOutSound());
	}
	
}
