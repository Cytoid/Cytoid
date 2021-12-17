//
//  LunarConsoleAction.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using UnityEngine.Events;

using Object = UnityEngine.Object;

using LunarConsolePlugin;

namespace LunarConsolePluginInternal
{
    public enum LunarPersistentListenerMode
    {
        Void,
        Bool,
        Int,
        Float,
        String,
        Object
    }

    [Serializable]
    internal class LunarArgumentCache : ISerializationCallbackReceiver
    {
        [SerializeField]
        private UnityEngine.Object m_objectArgument;

        [SerializeField]
        private string m_objectArgumentAssemblyTypeName;

        [SerializeField]
        private int m_intArgument;

        [SerializeField]
        private float m_floatArgument;

        [SerializeField]
        private string m_stringArgument;

        [SerializeField]
        private bool m_boolArgument;

        public UnityEngine.Object unityObjectArgument
        {
            get
            {
                return this.m_objectArgument;
            }
            set
            {
                this.m_objectArgument = value;
                this.m_objectArgumentAssemblyTypeName = ((!(value != null)) ? string.Empty : value.GetType().AssemblyQualifiedName);
            }
        }

        public string unityObjectArgumentAssemblyTypeName
        {
            get
            {
                return this.m_objectArgumentAssemblyTypeName;
            }
        }

        public int intArgument
        {
            get
            {
                return this.m_intArgument;
            }
            set
            {
                this.m_intArgument = value;
            }
        }

        public float floatArgument
        {
            get
            {
                return this.m_floatArgument;
            }
            set
            {
                this.m_floatArgument = value;
            }
        }

        public string stringArgument
        {
            get
            {
                return this.m_stringArgument;
            }
            set
            {
                this.m_stringArgument = value;
            }
        }

        public bool boolArgument
        {
            get
            {
                return this.m_boolArgument;
            }
            set
            {
                this.m_boolArgument = value;
            }
        }

        private void TidyAssemblyTypeName()
        {
            if (!string.IsNullOrEmpty(this.m_objectArgumentAssemblyTypeName))
            {
                int num = 2147483647;
                int num2 = this.m_objectArgumentAssemblyTypeName.IndexOf(", Version=");
                if (num2 != -1)
                {
                    num = Math.Min(num2, num);
                }
                num2 = this.m_objectArgumentAssemblyTypeName.IndexOf(", Culture=");
                if (num2 != -1)
                {
                    num = Math.Min(num2, num);
                }
                num2 = this.m_objectArgumentAssemblyTypeName.IndexOf(", PublicKeyToken=");
                if (num2 != -1)
                {
                    num = Math.Min(num2, num);
                }
                if (num != 2147483647)
                {
                    this.m_objectArgumentAssemblyTypeName = this.m_objectArgumentAssemblyTypeName.Substring(0, num);
                }
            }
        }

        public void OnBeforeSerialize()
        {
            this.TidyAssemblyTypeName();
        }

        public void OnAfterDeserialize()
        {
            this.TidyAssemblyTypeName();
        }
    }

    [Serializable]
    public class LunarConsoleActionCall
    {
        private static readonly Type[] kParamTypes = {
            typeof(int),
            typeof(float),
            typeof(string),
            typeof(bool)
        };

        #pragma warning disable 0649

        [SerializeField]
        Object m_target;

        [SerializeField]
        string m_methodName;

        [SerializeField]
        LunarPersistentListenerMode m_mode;

        [SerializeField]
        LunarArgumentCache m_arguments;

        #pragma warning restore 0649

        public void Invoke()
        {
            MethodInfo method = null;
            object[] invokeParams = null;
            switch (m_mode)
            {
                case LunarPersistentListenerMode.Void:
                    method = ResolveMethod(m_target, m_methodName, typeof(void));
                    invokeParams = new object[0];
                    break;

                case LunarPersistentListenerMode.Bool:
                    method = ResolveMethod(m_target, m_methodName, typeof(bool));
                    invokeParams = new object[] { m_arguments.boolArgument };
                    break;

                case LunarPersistentListenerMode.Float:
                    method = ResolveMethod(m_target, m_methodName, typeof(float));
                    invokeParams = new object[] { m_arguments.floatArgument };
                    break;

                case LunarPersistentListenerMode.Int:
                    method = ResolveMethod(m_target, m_methodName, typeof(int));
                    invokeParams = new object[] { m_arguments.intArgument };
                    break;

                case LunarPersistentListenerMode.String:
                    method = ResolveMethod(m_target, m_methodName, typeof(string));
                    invokeParams = new object[] { m_arguments.stringArgument };
                    break;

                case LunarPersistentListenerMode.Object:
                    method = ResolveMethod(m_target, m_methodName, typeof(UnityEngine.Object));
                    invokeParams = new object[] { m_arguments.unityObjectArgument };
                    break;

                default:
                    Log.e("Unable to invoke action: unexpected invoke mode '{0}'", m_mode);
                    return;
            }

            if (method != null)
            {
                method.Invoke(m_target, invokeParams);
            }
            else
            {
                Log.e("Unable to invoke action: can't resolve method '{0}'", m_methodName);
            }
        }

        static MethodInfo ResolveMethod(object target, string methodName, Type paramType)
        {
            var methods = ClassUtils.ListInstanceMethods(target.GetType(), delegate(MethodInfo method)
            {
                if (method.Name != methodName)
                {
                    return false;
                }

                var parameters = method.GetParameters();
                if (paramType == typeof(void))
                {
                    return parameters.Length == 0;
                }

                return parameters.Length == 1 && (parameters[0].ParameterType == paramType || parameters[0].ParameterType.IsSubclassOf(paramType));
            });
            return methods.Count == 1 ? methods[0] : null;
        }

        public static bool IsPersistantListenerValid(UnityEngine.Object target, string methodName, LunarPersistentListenerMode mode)
        {
            if (target == null)
            {
                return false;
            }

            List<MethodInfo> methods = ListActionMethods(target);
            foreach (var method in methods)
            {
                if (method.Name == methodName)
                {
                    var parameters = method.GetParameters();
                    if (mode == LunarPersistentListenerMode.Void)
                    {
                        if (parameters.Length == 0)
                        {
                            return true;
                        }
                    }
                    else if (parameters.Length == 1)
                    {
                        var paramType = parameters[0].ParameterType;
                        if (mode == LunarPersistentListenerMode.Bool && paramType == typeof(bool))
                        {
                            return true;
                        }
                        if (mode == LunarPersistentListenerMode.Float && paramType == typeof(float))
                        {
                            return true;
                        }
                        if (mode == LunarPersistentListenerMode.Int && paramType == typeof(int))
                        {
                            return true;
                        }
                        if (mode == LunarPersistentListenerMode.String && paramType == typeof(string))
                        {
                            return true;
                        }
                        if (mode == LunarPersistentListenerMode.Object && paramType.IsSubclassOf(typeof(UnityEngine.Object)))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static List<MethodInfo> ListActionMethods(object target)
        {
            List<MethodInfo> methods = new List<MethodInfo>();
            ClassUtils.ListMethods(methods, target.GetType(), IsValidActionMethod, BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            return methods;
        }

        /// <summary>
        /// Determines if method is a valid action delegate
        /// </summary>
        private static bool IsValidActionMethod(MethodInfo method)
        {
            if (!method.IsPublic)
            {
                return false; // only list public methods
            }

            if (method.ReturnType != typeof(void))
            {
                return false; // non-void return type are not allowed
            }

            if (method.IsAbstract)
            {
                return false; // don't list abstract methods
            }

            var methodParams = method.GetParameters();
            if (methodParams.Length > 1)
            {
                return false; // no more then a single param
            }

            var attributes = method.GetCustomAttributes(typeof(ObsoleteAttribute), false);
            if (attributes != null && attributes.Length > 0)
            {
                return false; // no obsolete methods
            }

            if (methodParams.Length == 1)
            {
                var paramType = methodParams[0].ParameterType;
                if (!paramType.IsSubclassOf(typeof(UnityEngine.Object)) && Array.IndexOf(kParamTypes, paramType) == -1)
                {
                    return false;
                }
            }

            return true;
        }

        public Object target
        {
            get { return m_target; }
        }

        public string methodName
        {
            get { return m_methodName; }
        }

        public LunarPersistentListenerMode mode
        {
            get { return m_mode; }
        }
    }

    public class LunarConsoleAction : MonoBehaviour
    {
        #pragma warning disable 0649

        [SerializeField]
        string m_title = "Untitled Action";

        [SerializeField]
        [HideInInspector]
        List<LunarConsoleActionCall> m_calls;

        #pragma warning restore 0649

        void Awake()
        {
            if (!actionsEnabled)
            {
                Destroy(this);
            }
        }

        void Start()
        {
            if (actionsEnabled)
            {
                RegisterAction();
            }
            else
            {
                Destroy(this);
            }
        }

        void OnValidate()
        {
            if (m_calls != null && m_calls.Count > 0)
            {
                foreach (var call in m_calls)
                {
                    Validate(call);
                }
            }
        }

        void Validate(LunarConsoleActionCall call)
        {
            if (call.target == null)
            {
                Debug.LogWarning(string.Format("Action '{0}' ({1}) is missing a target object", m_title, gameObject.name), gameObject); 
            }
            else if (!LunarConsoleActionCall.IsPersistantListenerValid(call.target, call.methodName, call.mode))
            {
                Debug.LogWarning(string.Format("Action '{0}' ({1}) is missing a handler <{2}.{3} ({4})>", m_title, gameObject.name, call.target.GetType(), call.methodName, ModeParamTypeName(call.mode)), gameObject); 
            }
        }
        
        void OnDestroy()
        {
            if (actionsEnabled)
            {
                UnregisterAction();
            }
        }
        
        public List<LunarConsoleActionCall> calls
        {
            get { return m_calls; }
        }

        private void RegisterAction()
        {
            LunarConsole.RegisterAction(m_title, InvokeAction);
        }

        private void UnregisterAction()
        {
            LunarConsole.UnregisterAction(InvokeAction);
        }

        private void InvokeAction()
        {
            if (m_calls != null && m_calls.Count > 0)
            {
                foreach (LunarConsoleActionCall call in m_calls)
                {
                    call.Invoke();
                }
            }
            else
            {
                Debug.LogWarningFormat("Action '{0}' has 0 calls", m_title);
            }
        }

        static string ModeParamTypeName(LunarPersistentListenerMode mode)
        {
            switch (mode)
            {
                case LunarPersistentListenerMode.Void:
                    return "";

                case LunarPersistentListenerMode.Bool:
                    return "bool";

                case LunarPersistentListenerMode.Float:
                    return "float";

                case LunarPersistentListenerMode.Int:
                    return "int";

                case LunarPersistentListenerMode.String:
                    return "string";

                case LunarPersistentListenerMode.Object:
                    return "UnityEngine.Object";
            }

            return "???";
        }

        bool actionsEnabled
        {
            get { return LunarConsoleConfig.actionsEnabled; }
        }
    }
}