//
//  lunar_unity_native_interface.h
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


#ifndef __LunarConsole__unity_native_interface__
#define __LunarConsole__unity_native_interface__

// life cycle
OBJC_EXTERN void __lunar_console_initialize(const char *targetName, const char *methodName, const char *version, const char *settingsJson);
OBJC_EXTERN void __lunar_console_destroy(void);

// show/hide
OBJC_EXTERN void __lunar_console_show(void);
OBJC_EXTERN void __lunar_console_hide(void);

// clear
OBJC_EXTERN void __lunar_console_clear(void);

// messages
OBJC_EXTERN void __lunar_console_log_message(const char *message, const char *stacktrace, int type);

// actions
OBJC_EXTERN void __lunar_console_action_register(int actionId, const char *actionName);
OBJC_EXTERN void __lunar_console_action_unregister(int actionId);

// variables
OBJC_EXTERN void __lunar_console_cvar_register(int entryId, const char *name, const char *type, const char *value, const char *defaultValue, int flags, BOOL hasRange, float min, float max, const char *values);
OBJC_EXTERN void __lunar_console_cvar_update(int entryId, const char *value);

#endif /* defined(__LunarConsole__unity_native_interface__) */
