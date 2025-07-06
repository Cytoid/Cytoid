//
//  ColorUtils.cs
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


﻿using UnityEngine;
using System.Collections;

namespace LunarConsolePluginInternal
{
    static class ColorUtils
    {
        private const float kMultiplier = 1.0f / 255.0f;

        public static Color FromRGBA(uint value)
        {
            float r = ((value >> 24) & 0xff) * kMultiplier;
            float g = ((value >> 16) & 0xff) * kMultiplier;
            float b = ((value >> 8) & 0xff) * kMultiplier;
            float a = (value & 0xff) * kMultiplier;

            return new Color(r, g, b, a);
        }

        public static Color FromRGB(uint value)
        {
            float r = ((value >> 16) & 0xff) * kMultiplier;
            float g = ((value >> 8) & 0xff) * kMultiplier;
            float b = (value & 0xff) * kMultiplier;
            float a = 1.0f;

            return new Color(r, g, b, a);
        }

        public static uint ToRGBA(ref Color value)
        {
            uint r = (uint)(value.r * 255.0f) & 0xff;
            uint g = (uint)(value.g * 255.0f) & 0xff;
            uint b = (uint)(value.b * 255.0f) & 0xff;
            uint a = (uint)(value.a * 255.0f) & 0xff;

            return (r << 24) | (g << 16) | (b << 8) | a;
        }
    }
}

