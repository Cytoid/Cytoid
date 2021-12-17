//
//  CRegistry.cs
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


using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

using LunarConsolePlugin;

namespace LunarConsolePluginInternal
{
    delegate bool CActionFilter(CAction action);

    public interface ICRegistryDelegate
    {
        void OnActionRegistered(CRegistry registry, CAction action);
        void OnActionUnregistered(CRegistry registry, CAction action);
        void OnVariableRegistered(CRegistry registry, CVar cvar);
        void OnVariableUpdated(CRegistry registry, CVar cvar);
    }

    public class CRegistry
    {
        private readonly CActionList m_actions = new CActionList();
        private readonly CVarList m_vars = new CVarList();

        private ICRegistryDelegate m_delegate;

        #region Commands registry

        public CAction RegisterAction(string name, Delegate actionDelegate)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw new ArgumentException("Action's name is empty");
            }
            
            if (actionDelegate == null)
            {
                throw new ArgumentNullException("actionDelegate");
            }

            CAction action = m_actions.Find(name);
            if (action != null)
            {
                // Log.w("Overriding action: {0}", name);
                action.ActionDelegate = actionDelegate;
            }
            else
            {
                action = new CAction(name, actionDelegate);
                m_actions.Add(action);

                if (m_delegate != null)
                {
                    m_delegate.OnActionRegistered(this, action);
                }
            }

            return action;
        }

        public bool Unregister(string name)
        {
            return Unregister(delegate(CAction action)
            {
                return action.Name == name;
            });
        }

        public bool Unregister(int id)
        {
            return Unregister(delegate(CAction action)
            {
                return action.Id == id;
            });
        }

        public bool Unregister(Delegate del)
        {
            return Unregister(delegate(CAction action)
            {
                return action.ActionDelegate == del;
            });
        }

        public bool UnregisterAll(object target)
        {
            return target != null && Unregister(delegate(CAction action)
            {
                return action.ActionDelegate.Target == target;
            });
        }

        bool Unregister(CActionFilter filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException("filter");
            }

            IList<CAction> actionsToRemove = new List<CAction>();
            foreach (var action in m_actions)
            {
                if (filter(action))
                {
                    actionsToRemove.Add(action);
                }
            }

            foreach (var action in actionsToRemove)
            {
                RemoveAction(action);
            }

            return actionsToRemove.Count > 0;
        }

        bool RemoveAction(CAction action)
        {
            if (m_actions.Remove(action.Id))
            {
                if (m_delegate != null)
                {
                    m_delegate.OnActionUnregistered(this, action);
                }

                return true;
            }

            return false;
        }

        public CAction FindAction(int id)
        {
            return m_actions.Find(id);
        }

        #endregion

        #region Variables

        public void Register(CVar cvar)
        {
            m_vars.Add(cvar);

            if (m_delegate != null)
            {
                m_delegate.OnVariableRegistered(this, cvar);
            }
        }

        public CVar FindVariable(int variableId)
        {
            return m_vars.Find(variableId);
        }

        public CVar FindVariable(string variableName)
        {
            return m_vars.Find(variableName);
        }

        #endregion

        #region Destroyable

        public void Destroy()
        {
            m_actions.Clear();
            m_vars.Clear();
            m_delegate = null;
        }

        #endregion

        #region Properties

        public ICRegistryDelegate registryDelegate
        {
            get { return m_delegate; }
            set { m_delegate = value; }
        }

        public CActionList actions
        {
            get { return m_actions; }
        }

        public CVarList cvars
        {
            get { return m_vars; }
        }

        #endregion
    }
}
