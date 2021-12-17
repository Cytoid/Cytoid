//
//  CAction.cs
//
//  Lunar Unity Mobile Console
//  https://github.com/SpaceMadness/lunar-unity-console
//
//  Copyright 2015-2021 Alex Lementuev, SpaceMadness.
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//


﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using UnityEngine;

namespace LunarConsolePluginInternal
{
    public class CAction : IComparable<CAction>
    {
        static readonly string[] kEmptyArgs = new String[0];
        static int s_nextActionId;

        readonly int m_id;
        readonly string m_name;
        Delegate m_actionDelegate;

        public CAction(string name, Delegate actionDelegate)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw new ArgumentException("Action name is empty");
            }

            if (actionDelegate == null)
            {
                throw new ArgumentNullException("actionDelegate");
            }

            m_id = s_nextActionId++;
            m_name = name;
            m_actionDelegate = actionDelegate;
        }

        public bool Execute()
        {
            try
            {
                return ReflectionUtils.Invoke(ActionDelegate, kEmptyArgs); // TODO: remove it
            }
            catch (TargetInvocationException e)
            {
                Log.e(e.InnerException, "Exception while invoking action '{0}'", m_name);
            }
            catch (Exception e)
            {
                Log.e(e, "Exception while invoking action '{0}'", m_name);
            }

            return false;
        }

        #region Helpers

        internal bool StartsWith(string prefix)
        {
            return StringUtils.StartsWithIgnoreCase(Name, prefix);
        }

        #endregion

        #region IComparable

        public int CompareTo(CAction other)
        {
            return Name.CompareTo(other.Name);
        }

        #endregion

        #region String representation

        public override string ToString()
        {
            return string.Format("{0} ({1})", Name, ActionDelegate);
        }

        #endregion

        #region Properties

        public int Id
        {
            get { return m_id; }
        }

        public string Name
        {
            get { return m_name; }
        }

        public Delegate ActionDelegate
        {
            get { return m_actionDelegate; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("actionDelegate");
                }
                m_actionDelegate = value;
            }
        }

        #endregion
    }

    public class CActionList : IEnumerable<CAction>
    {
        private readonly List<CAction> m_actions;
        private readonly Dictionary<int, CAction> m_actionLookupById;
        private readonly Dictionary<string, CAction> m_actionLookupByName;

        public CActionList()
        {
            m_actions = new List<CAction>();
            m_actionLookupById = new Dictionary<int, CAction>();
            m_actionLookupByName = new Dictionary<string, CAction>();
        }

        public void Add(CAction action)
        {
            m_actions.Add(action);
            m_actionLookupById.Add(action.Id, action);
            m_actionLookupByName.Add(action.Name, action);
        }

        public bool Remove(int id)
        {
            CAction action;
            if (m_actionLookupById.TryGetValue(id, out action))
            {
                m_actionLookupById.Remove(id);
                m_actionLookupByName.Remove(action.Name);
                m_actions.Remove(action);

                return true;
            }

            return false;
        }

        public CAction Find(string name)
        {
            CAction action;
            return m_actionLookupByName.TryGetValue(name, out action) ? action : null;
        }

        public CAction Find(int id)
        {
            CAction action;
            return m_actionLookupById.TryGetValue(id, out action) ? action : null;
        }

        public void Clear()
        {
            m_actions.Clear();
            m_actionLookupById.Clear();
            m_actionLookupByName.Clear();
        }

        #region IEnumerable implementation

        public IEnumerator<CAction> GetEnumerator()
        {
            return m_actions.GetEnumerator();
        }

        #endregion

        #region IEnumerable implementation

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return m_actions.GetEnumerator();
        }

        #endregion
    }
}
