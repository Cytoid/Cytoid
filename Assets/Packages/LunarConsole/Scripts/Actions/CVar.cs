//
//  CVar.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using LunarConsolePluginInternal;

namespace LunarConsolePlugin
{
    public delegate void CVarChangedDelegate(CVar cvar);

    public enum CVarType
    {
        Boolean,
        Integer,
        Float,
        String,
        Enum
    }

    struct CValue
    {
        public string stringValue;
        public int intValue;
        public float floatValue;

        public bool Equals(ref CValue other)
        {
            return other.intValue == intValue &&
            other.floatValue == floatValue &&
            other.stringValue == stringValue;
        }
    }

    public struct CVarValueRange
    {
        public static readonly CVarValueRange Undefined = new CVarValueRange(float.NaN, float.NaN);

        public readonly float min;
        public readonly float max;

        public CVarValueRange(float min, float max)
        {
            this.min = min;
            this.max = max;
        }

        public bool IsValid
        {
            get { return !float.IsNaN(min) && !float.IsNaN(max); }
        }
    }

    [Flags]
    public enum CFlags
    {   
        /// <summary>
        /// No flags (default value)
        /// </summary>
        None      = 0,

        /// <summary>
        /// Won't be listed in UI
        /// </summary>
        Hidden    = 1 << 1,

        /// <summary>
        /// Don't save between sessions
        /// </summary>
        NoArchive = 1 << 2
    }

    public class CVar : IEquatable<CVar>, IComparable<CVar>
    {
        private static int s_nextId;

        private readonly int m_id;
        private readonly string m_name;
        private readonly CVarType m_type;
        private readonly CFlags m_flags;

        private CValue m_value;
        private CValue m_defaultValue;
        private CVarValueRange m_range = CVarValueRange.Undefined;

        private CVarChangedDelegateList m_delegateList;

        public CVar(string name, bool defaultValue, CFlags flags = CFlags.None)
            : this(name, CVarType.Boolean, flags)
        {
            this.IntValue = defaultValue ? 1 : 0;
            m_defaultValue = m_value;
        }

        public CVar(string name, int defaultValue, CFlags flags = CFlags.None)
            : this(name, CVarType.Integer, flags)
        {
            this.IntValue = defaultValue;
            m_defaultValue = m_value;
        }

        public CVar(string name, float defaultValue, CFlags flags = CFlags.None)
            : this(name, CVarType.Float, flags)
        {
            this.FloatValue = defaultValue;
            m_defaultValue = m_value;
        }

        public CVar(string name, string defaultValue, CFlags flags = CFlags.None)
            : this(name, CVarType.String, flags)
        {
            this.Value = defaultValue;
            m_defaultValue = m_value;
        }
        
        protected CVar(string name, CVarType type, CFlags flags)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            m_id = ++s_nextId;

            m_name = name;
            m_type = type;
            m_flags = flags;
        }

        #region Delegates

        public void AddDelegate(CVarChangedDelegate del)
        {
            if (del == null)
            {
                throw new ArgumentNullException("del");
            }

            if (m_delegateList == null)
            {
                m_delegateList = new CVarChangedDelegateList(1);
                m_delegateList.Add(del);
            }
            else if (!m_delegateList.Contains(del))
            {
                m_delegateList.Add(del);
            }
        }

        public void RemoveDelegate(CVarChangedDelegate del)
        {
            if (del != null && m_delegateList != null)
            {
                m_delegateList.Remove(del);

                if (m_delegateList.Count == 0)
                {
                    m_delegateList = null;
                }
            }
        }

        public void RemoveDelegates(object target)
        {
            if (target != null && m_delegateList != null)
            {
                for (int i = m_delegateList.Count - 1; i >= 0; --i)
                {
                    if (m_delegateList.Get(i).Target == target)
                    {
                        m_delegateList.RemoveAt(i);
                    }
                }

                if (m_delegateList.Count == 0)
                {
                    m_delegateList = null;
                }
            }
        }

        private void NotifyValueChanged()
        {
            if (m_delegateList != null && m_delegateList.Count > 0)
            {
                m_delegateList.NotifyValueChanged(this);
            }
        }

        #endregion

        #region IEquatable

        public bool Equals(CVar other)
        {
            return other != null &&
            other.m_name == m_name &&
            other.m_value.Equals(ref m_value) &&
            other.m_defaultValue.Equals(ref m_defaultValue) &&
            other.m_type == m_type;
        }

        #endregion

        #region IComparable

        public int CompareTo(CVar other)
        {
            return Name.CompareTo(other.Name);
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

        public CVarType Type
        {
            get { return m_type; }
        }

        public string DefaultValue
        {
            get { return m_defaultValue.stringValue; }
            protected set { m_defaultValue.stringValue = value; }
        }

        public bool IsString
        {
            get { return m_type == CVarType.String; }
        }

        public string Value
        {
            get { return m_value.stringValue; }
            set
            {
                bool changed = m_value.stringValue != value;

                m_value.stringValue = value;
                m_value.floatValue = IsInt || IsFloat ? StringUtils.ParseFloat(value, 0.0f) : 0.0f;
                m_value.intValue = IsInt || IsFloat ? (int)FloatValue : 0;

                if (changed)
                {
                    NotifyValueChanged();
                }
            }
        }

        public CVarValueRange Range
        {
            get { return m_range; }
            set { m_range = value; }
        }

        public bool HasRange
        {
            get { return m_range.IsValid; }
        }

        public bool IsInt
        {
            get { return m_type == CVarType.Integer || m_type == CVarType.Boolean; }
        }

        public int IntValue
        {
            get { return m_value.intValue; }
            set
            {
                bool changed = m_value.intValue != value;

                m_value.stringValue = StringUtils.ToString(value);
                m_value.intValue = value;
                m_value.floatValue = (float)value;

                if (changed)
                {
                    NotifyValueChanged();
                }
            }
        }

        public bool IsFloat
        {
            get { return m_type == CVarType.Float; }
        }

        public float FloatValue
        {
            get { return m_value.floatValue; }
            set
            {
                float oldValue = m_value.floatValue;

                m_value.stringValue = StringUtils.ToString(value);
                m_value.intValue = (int)value;
                m_value.floatValue = value;

                if (oldValue != value)
                {
                    NotifyValueChanged();
                }
            }
        }

        public bool IsBool
        {
            get { return m_type == CVarType.Boolean; }
        }

        public bool BoolValue
        {
            get { return m_value.intValue != 0; }
            set { this.IntValue = value ? 1 : 0; }
        }

        public virtual string[] AvailableValues
        {
            get { return null; }
        }

        public bool IsDefault
        {
            get { return m_value.Equals(m_defaultValue); }
            set
            {
                bool changed = this.IsDefault ^ value;
                m_value = m_defaultValue;

                if (changed)
                {
                    NotifyValueChanged();
                }
            }
        }

        public bool HasFlag(CFlags flag)
        {
            return (m_flags & flag) != 0;
        }

        public CFlags Flags
        {
            get { return m_flags; }
        }

        public bool IsHidden
        {
            get { return (m_flags & CFlags.Hidden) != 0; }
        }

        #endregion

        #region Operators

        public static implicit operator string(CVar cvar)
        {
            return cvar.m_value.stringValue;
        }

        public static implicit operator int(CVar cvar)
        {
            return cvar.m_value.intValue;
        }

        public static implicit operator float(CVar cvar)
        {
            return cvar.m_value.floatValue;
        }

        public static implicit operator bool(CVar cvar)
        {
            return cvar.m_value.intValue != 0;
        }

        #endregion
    }

    public class CEnumVar<T> : CVar  where T : struct, IConvertible
    {
        private readonly IDictionary<string, T> m_valueLookup;
        private readonly string[] m_names;

        public CEnumVar(string name, T defaultValue, CFlags flags = CFlags.None) : base(name, CVarType.Enum, flags)
        {
            if (!typeof(T).IsEnum) 
            {
                throw new ArgumentException("T must be an enumerated type");
            }
            
            var value = defaultValue.ToString();
            
            Value = value;
            DefaultValue = value;
            
            var values = Enum.GetValues(typeof(T));
            m_names = Enum.GetNames(typeof(T));
            
            m_valueLookup = new Dictionary<string, T>();
            for (int i = 0; i < values.Length; i++)
            {
                m_valueLookup[m_names[i]] = (T) values.GetValue(i);
            }
        }

        public override string[] AvailableValues
        {
            get { return m_names; }
        }

        public T EnumValue
        {
            get { return m_valueLookup[Value]; }
        }

        #if UNITY_2017_1_OR_NEWER
        
        public static implicit operator T(CEnumVar<T> cvar)
        {
            return cvar.EnumValue;
        }
        
        #endif
    }
    
    public class CVarList : IEnumerable<CVar>
    {
        private readonly List<CVar> m_variables;
        private readonly Dictionary<int, CVar> m_lookupById;

        public CVarList()
        {
            m_variables = new List<CVar>();
            m_lookupById = new Dictionary<int, CVar>();
        }

        public void Add(CVar variable)
        {
            m_variables.Add(variable);
            m_lookupById.Add(variable.Id, variable);
        }

        public bool Remove(int id)
        {
            CVar variable;
            if (m_lookupById.TryGetValue(id, out variable))
            {
                m_lookupById.Remove(id);
                m_variables.Remove(variable);

                return true;
            }

            return false;
        }

        public CVar Find(int id)
        {
            CVar variable;
            return m_lookupById.TryGetValue(id, out variable) ? variable : null;
        }

        public CVar Find(string name)
        {
            foreach (var cvar in m_variables)
            {
                if (cvar.Name == name)
                {
                    return cvar;
                }
            }
            return null;
        }

        public void Clear()
        {
            m_variables.Clear();
            m_lookupById.Clear();
        }

        #region IEnumerable implementation

        public IEnumerator<CVar> GetEnumerator()
        {
            return m_variables.GetEnumerator();
        }

        #endregion

        #region IEnumerable implementation

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return m_variables.GetEnumerator();
        }

        #endregion

        public int Count
        {
            get { return m_variables.Count; }
        }
    }

    [AttributeUsage (AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class CVarRangeAttribute : Attribute
    {
        public readonly float min;

        public readonly float max;

        public CVarRangeAttribute(float min, float max)
        {
            this.min = min;
            this.max = max;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class CVarContainerAttribute : Attribute
    {
    }

    class CVarChangedDelegateList : BaseList<CVarChangedDelegate>
    {
        public CVarChangedDelegateList(int capacity)
            : base(NullCVarChangedDelegate, capacity)
        {
        }

        public void NotifyValueChanged(CVar cvar)
        {
            try
            {
                Lock();

                int elementsCount = list.Count;
                for (int i = 0; i < elementsCount; ++i) // do not update added items on that tick
                {
                    try
                    {
                        list[i](cvar);
                    }
                    catch (Exception e)
                    {
                        Log.e(e, "Exception while calling value changed delegate for '{0}'", cvar.Name);
                    }
                }
            }
            finally
            {
                Unlock();
            }
        }

        static void NullCVarChangedDelegate(CVar cvar)
        {
        }
    }
}