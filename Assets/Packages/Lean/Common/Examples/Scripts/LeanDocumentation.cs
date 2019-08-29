using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

namespace Lean.Common.Examples
{
	[CustomEditor(typeof(LeanDocumentation))]
	public class LeanDocumentation_Inspector : Editor
	{
		private static GUIStyle titleStyle;

		private static GUIStyle headerStyle;

		private static GUIStyle bodyStyle;

		private static GUIStyle rateStyle;

		public static void UpdateStyles()
		{
			if (bodyStyle == null)
			{
				bodyStyle = new GUIStyle(EditorStyles.label);
				bodyStyle.wordWrap = true;
				bodyStyle.fontSize = 14;

				titleStyle = new GUIStyle(bodyStyle);
				titleStyle.fontSize = 26;
				titleStyle.alignment = TextAnchor.MiddleCenter;

				headerStyle = new GUIStyle(bodyStyle);
				headerStyle.fontSize = 18 ;

				rateStyle = new GUIStyle(EditorStyles.toolbarButton);

				rateStyle.fontSize = 20;
			}
		}

		public override void OnInspectorGUI()
		{
			var Target = (LeanDocumentation)target;

			UpdateStyles();

			EditorGUI.EndDisabledGroup();

			EditorGUILayout.LabelField("Thank You For Using " + Target.Title + "!", headerStyle);
			EditorGUILayout.LabelField("The documentation is in HTML format. You can open it by clicking below.", bodyStyle);

			if (GUILayout.Button(new GUIContent("Local Documentation", "Open In Web Browser")) == true)
			{
				var path = System.IO.Path.Combine(Application.temporaryCachePath, Target.Link + ".html");

				Debug.Log("Generating and opening documentation in: " + path);

				System.IO.File.WriteAllText(path, Target.HTML);

				System.Diagnostics.Process.Start(path);
			}

			if (GUILayout.Button(new GUIContent("Online Documentation", "Open In Web Browser")) == true)
			{
				Application.OpenURL("http://CarlosWilkes.com/Documentation/" + Target.Link);
			}

			EditorGUILayout.Separator();
			EditorGUILayout.Separator();

			EditorGUILayout.LabelField("Need Help?", headerStyle);
			EditorGUILayout.LabelField("If you read the documentation and still have questions, feel free to ask!", bodyStyle);

			if (string.IsNullOrEmpty(Target.Forum) == false)
			{
				if (GUILayout.Button(new GUIContent("Forum Thread", "Unity Forums")) == true)
				{
					Application.OpenURL(Target.Forum);
				}
			}

			if (string.IsNullOrEmpty(Target.YouTube) == false)
			{
				if (GUILayout.Button(new GUIContent("YouTube Channel", "YouTube")) == true)
				{
					Application.OpenURL(Target.YouTube);
				}
			}
				
			if (GUILayout.Button(new GUIContent("E-Mail Me", "carlos.wilkes@gmail.com")) == true)
			{
				Application.OpenURL("mailto:carlos.wilkes@gmail.com");
			}

			if (GUILayout.Button(new GUIContent("Private Message", "Unity Forum Profile")) == true)
			{
				Application.OpenURL("http://forum.unity.com/members/41960");
			}

			EditorGUILayout.Separator();
			EditorGUILayout.Separator();

			EditorGUILayout.LabelField("You're Awesome!", headerStyle);
			EditorGUILayout.LabelField("If you haven't already, please consider rating this asset. It really helps me out!", bodyStyle);

			if (GUILayout.Button(new GUIContent("Rate This Asset", Target.Title + " Asset Page")) == true)
			{
				Application.OpenURL("http://CarlosWilkes.com/Get/" + Target.Link);
			}

			EditorGUILayout.Separator();
			EditorGUILayout.Separator();

			EditorGUILayout.LabelField("Made Something Cool?", headerStyle);
			EditorGUILayout.LabelField("If you've finished a project using " + Target.Title + " then let me know! I can shout you out, link to you from my website, and much more!", bodyStyle);

			if (GUILayout.Button(new GUIContent("E-Mail Me", "carlos.wilkes@gmail.com")) == true)
			{
				Application.OpenURL("mailto:carlos.wilkes@gmail.com");
			}

			EditorGUILayout.Separator();
			EditorGUILayout.Separator();

			EditorGUILayout.LabelField("Want More?", headerStyle);
			EditorGUILayout.LabelField("I've released a range of assets to speed up your project development, check them out!", bodyStyle);

			if (GUILayout.Button(new GUIContent("My Website", "CarlosWilkes.com")) == true)
			{
				Application.OpenURL("http://CarlosWilkes.com");
			}
		}

		protected override void OnHeaderGUI()
		{
			var Target = (LeanDocumentation)target;

			UpdateStyles();

			GUILayout.BeginHorizontal("In BigTitle");
			{
				var iconWidth = Mathf.Min(EditorGUIUtility.currentViewWidth/3f - 20f, 128f);
				var content   = new GUIContent(Target.Title.Replace(' ', '\n'));
				var height    = Mathf.Max(titleStyle.CalcHeight(content, EditorGUIUtility.currentViewWidth - iconWidth), iconWidth);

				if (Target.Icon != null)
				{
					GUILayout.Label(Target.Icon, EditorStyles.centeredGreyMiniLabel, GUILayout.Width(iconWidth), GUILayout.Height(iconWidth));
				}

				GUILayout.Label(content, titleStyle, GUILayout.Height(height));
			}
			GUILayout.EndHorizontal();
		}
	}
}
#endif

namespace Lean.Common.Examples
{
	/// <summary>This class defines documentation data that can be viewed in the inspector.</summary>
	public class LeanDocumentation : ScriptableObject
	{
		public string Title;

		public string YouTube;

		public string Forum;

		public string Link;

		public string IconData;

		public string HTML;

		[System.NonSerialized]
		private Texture2D icon;

		public Texture2D Icon
		{
			get
			{
				if (icon == null)
				{
					icon = new Texture2D(1, 1);

					icon.LoadImage(System.Convert.FromBase64String(IconData));
				}

				return icon;
			}
		}
	}
}