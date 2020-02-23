using System;
using System.Collections.Generic;
using UnityEngine;

namespace Polyglot
{
	[Serializable]
	public class LocalizedString
	{
		[SerializeField]
		[LocalizedString]
		private string key;
		public string Key
		{
			get
			{
				return key;
			}
			set
			{
				key = value;
			}
		}

		public string Text
		{
			get
			{
				if (parameters != null && parameters.Count > 0)
				{
					return Localization.GetFormat(key, parameters.ToArray());
				}
				else
				{
					return Localization.Get(key);
				}
			}
		}


		public List<object> Parameters { get { return parameters; } }


		private List<object> parameters = new List<object>();

		public void ClearParameters()
		{
			parameters.Clear();
		}

		public void AddParameter(object parameter)
		{
			parameters.Add(parameter);
		}
		public void AddParameter(int parameter)
		{
			AddParameter((object)parameter);
		}
		public void AddParameter(float parameter)
		{
			AddParameter((object)parameter);
		}
		public void AddParameter(string parameter)
		{
			AddParameter((object)parameter);
		}

		public static implicit operator string(LocalizedString localizedString)
		{
			return localizedString.Text;
		}

		public override string ToString()
		{
			return Text;
		}
	}
}
