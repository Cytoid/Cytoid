//
//  ReflectionUtils.cs
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
    public delegate bool ReflectionTypeFilter(Type type);

    static class ReflectionUtils
    {
        #region Invocation

        private static readonly object[] EMPTY_INVOKE_ARGS = new object[0];

        public static bool Invoke(Delegate del, string[] invokeArgs)
        {
            if (del == null)
            {
                throw new ArgumentNullException("del");
            }

            return Invoke(del.Target, del.Method, invokeArgs);
        }

        public static bool Invoke(object target, MethodInfo method, string[] invokeArgs)
        {
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length == 0)
            {
                return Invoke(target, method, EMPTY_INVOKE_ARGS);
            }

            List<object> invokeList = new List<object>(invokeArgs.Length);

            Iterator<string> iter = new Iterator<string>(invokeArgs);
            foreach (ParameterInfo param in parameters)
            {
                invokeList.Add(ResolveInvokeParameter(param, iter));
            }

            return Invoke(target, method, invokeList.ToArray());
        }

        private static bool Invoke(object target, MethodInfo method, object[] args)
        {
            if (method.ReturnType == typeof(bool))
            {
                return (bool)method.Invoke(target, args);
            }

            method.Invoke(target, args);
            return true;
        }

        private static object ResolveInvokeParameter(ParameterInfo param, Iterator<string> iter)
        {
            if (param.IsOptional && !iter.HasNext())
            {
                return param.DefaultValue;
            }

            Type type = param.ParameterType;

            if (type == typeof(string[]))
            {
                List<string> values = new List<string>();
                while (iter.HasNext())
                {
                    values.Add(NextArg(iter));
                }
                return values.ToArray();
            }

            if (type == typeof(string))
            {
                return NextArg(iter);
            }

            if (type == typeof(float))
            {
                return NextFloatArg(iter);
            }

            if (type == typeof(int))
            {
                return NextIntArg(iter);
            }

            if (type == typeof(bool))
            {
                return NextBoolArg(iter);
            }

            if (type == typeof(Vector2))
            {
                float x = NextFloatArg(iter);
                float y = NextFloatArg(iter);

                return new Vector2(x, y);
            }

            if (type == typeof(Vector3))
            {
                float x = NextFloatArg(iter);
                float y = NextFloatArg(iter);
                float z = NextFloatArg(iter);

                return new Vector3(x, y, z);
            }

            if (type == typeof(Vector4))
            {
                float x = NextFloatArg(iter);
                float y = NextFloatArg(iter);
                float z = NextFloatArg(iter);
                float w = NextFloatArg(iter);

                return new Vector4(x, y, z, w);
            }

            if (type == typeof(int[]))
            {
                List<int> values = new List<int>();
                while (iter.HasNext())
                {
                    values.Add(NextIntArg(iter));
                }
                return values.ToArray();
            }

            if (type == typeof(float[]))
            {
                List<float> values = new List<float>();
                while (iter.HasNext())
                {
                    values.Add(NextFloatArg(iter));
                }
                return values.ToArray();
            }

            if (type == typeof(bool[]))
            {
                List<bool> values = new List<bool>();
                while (iter.HasNext())
                {
                    values.Add(NextBoolArg(iter));
                }
                return values.ToArray();
            }

            throw new ReflectionException("Unsupported value type: " + type);
        }

        public static int NextIntArg(Iterator<string> iter)
        {
            string arg = NextArg(iter);
            int value;

            if (int.TryParse(arg, out value))
            {
                return value;
            }

            throw new ReflectionException("Can't parse int arg: '" + arg + "'"); 
        }

        public static float NextFloatArg(Iterator<string> iter)
        {
            string arg = NextArg(iter);
            float value;

            if (float.TryParse(arg, out value))
            {
                return value;
            }

            throw new ReflectionException("Can't parse float arg: '" + arg + "'"); 
        }

        public static bool NextBoolArg(Iterator<string> iter)
        {
            string arg = NextArg(iter).ToLower();
            if (arg == "1" || arg == "yes" || arg == "true") return true;
            if (arg == "0" || arg == "no"  || arg == "false") return false;

            throw new ReflectionException("Can't parse bool arg: '" + arg + "'"); 
        }

        public static string NextArg(Iterator<string> iter)
        {
            if (iter.HasNext())
            {
                string arg = StringUtils.UnArg(iter.Next());
                if (!IsValidArg(arg)) 
                {
                    throw new ReflectionException("Invalid arg: " + arg);
                }

                return arg;
            }

            throw new ReflectionException("Unexpected end of args");
        }

        public static bool IsValidArg(string arg)
        {
            // TODO: think about a better way of checking args
            // check is omitted since it messes up with operation commands (e.g. "bind x -variable")
            // return !arg.StartsWith("-") || StringUtils.IsNumeric(arg);
            return true;
        }

        #endregion

        #region Assembly Helper

        public static List<Assembly> ListAssemblies(Func<Assembly, bool> filter)
        {
            List<Assembly> result = new List<Assembly>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (filter(assembly))
                {
                    result.Add(assembly);
                }
            }
            return result;
        }

        public static List<Type> FindAttributeTypes<T>(Assembly assembly) where T : Attribute
        {
            return FindAttributeTypes(assembly, typeof(T));
        }

        public static List<Type> FindAttributeTypes(Assembly assembly, Type attributeType)
        {
            return FindTypes(assembly, delegate(Type type)
            {
                var attributes = type.GetCustomAttributes(attributeType, false);
                return attributes != null && attributes.Length > 0;
            });
        }

        public static List<Type> FindTypes(Assembly assembly, ReflectionTypeFilter filter)
        {
            var list = new List<Type>();

            try
            {
                foreach (var type in GetAssemblyTypes(assembly))
                {
                    if (filter(type))
                    {
                        list.Add(type);
                    }
                }
            }
            catch (Exception e)
            {
                Log.e(e, "Unable to list types for assembly: {0}", assembly);
            }

            return list;
        }

        private static IEnumerable<Type> GetAssemblyTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                List<Type> result = new List<Type>();
                Type[] types = e.Types;
                for (int i = 0; i < types.Length; ++i)
                {
                    if (types[i] != null)
                    {
                        result.Add(types[i]);
                    }
                }
                return result;
            }
        }

        #endregion
    }

    class ReflectionException : Exception
    {
        public ReflectionException(string message)
            : base(message)
        {
        }

        public ReflectionException(string format, params object[] args)
            : this(StringUtils.TryFormat(format, args))
        {
        }
    }
}