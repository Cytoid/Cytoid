using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
	
	private void Start()
	{
		if (PlayerPrefs.HasKey("level"))
		{
			GetLevelInputField().text = PlayerPrefs.GetString("level");
		}
		if (PlayerPrefs.HasKey("user_offset"))
		{
			GetUserOffsetInputField().text = PlayerPrefs.GetFloat("user_offset").ToString();
		}
		if (PlayerPrefs.HasKey("autoplay"))
		{
			GetAutoplayToggle().isOn = ItB(PlayerPrefs.GetInt("autoplay", 0));
		}
	}

	public void Play()
	{
		PlayerPrefs.SetString("level", GetLevelInputField().text);
		PlayerPrefs.SetFloat("user_offset", float.Parse(GetUserOffsetInputField().text));
		PlayerPrefs.SetInt("autoplay", BtI(GetAutoplayToggle().isOn));
		PlayerPrefs.Save();
		SceneManager.LoadScene("Game");
	}

	private InputField GetLevelInputField()
	{
		return GameObject.FindGameObjectWithTag("LevelInput").GetComponent<InputField>();
	}
	
	private InputField GetUserOffsetInputField()
	{
		return GameObject.FindGameObjectWithTag("UserOffsetInput").GetComponent<InputField>();
	}

	private Toggle GetAutoplayToggle()
	{
		return GameObject.FindGameObjectWithTag("AutoplayToggle").GetComponent<Toggle>();
	}

	private static bool ItB(int i)
	{
		return i == 1;
	}

	private static int BtI(bool b)
	{
		return b ? 1 : 0;
	}
	
}