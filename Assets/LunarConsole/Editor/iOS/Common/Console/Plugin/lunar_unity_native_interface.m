//
//  lunar_unity_native_interface.m
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


#include <Foundation/Foundation.h>

#include "lunar_unity_native_interface.h"

#import "Lunar.h"
#import "LUConsolePlugin.h"

static LUConsolePlugin *_lunarConsolePlugin;

void __lunar_console_initialize(const char *targetNameStr, const char *methodNameStr, const char *versionStr, const char *settingsJsonStr)
{
    lunar_dispatch_main(^{
        if (_lunarConsolePlugin == nil) {
            NSString *targetName = [[NSString alloc] initWithUTF8String:targetNameStr];
            NSString *methodName = [[NSString alloc] initWithUTF8String:methodNameStr];
            NSString *version = [[NSString alloc] initWithUTF8String:versionStr];
            NSString *settingsJson = [[NSString alloc] initWithUTF8String:settingsJsonStr];

            _lunarConsolePlugin = [[LUConsolePlugin alloc] initWithTargetName:targetName
                                                                   methodName:methodName
                                                                      version:version
                                                                 settingsJson:settingsJson];
            [_lunarConsolePlugin start];
        }
    });
}

void __lunar_console_destroy()
{
    lunar_dispatch_main(^{
        [_lunarConsolePlugin stop];
        _lunarConsolePlugin = nil;
    });
}

void __lunar_console_show()
{
    lunar_dispatch_main(^{
        LUAssert(_lunarConsolePlugin);
        [_lunarConsolePlugin showConsole];
    });
}

void __lunar_console_hide()
{
    lunar_dispatch_main(^{
        LUAssert(_lunarConsolePlugin);
        [_lunarConsolePlugin hideConsole];
    });
}

void __lunar_console_clear()
{
    lunar_dispatch_main(^{
        LUAssert(_lunarConsolePlugin);
        [_lunarConsolePlugin clearConsole];
    });
}

void __lunar_console_log_message(const char *messageStr, const char *stackTraceStr, int type)
{
    NSString *message = [[NSString alloc] initWithUTF8String:messageStr];
    NSString *stackTrace = [[NSString alloc] initWithUTF8String:stackTraceStr];

    if ([NSThread isMainThread]) {
        [_lunarConsolePlugin logMessage:message stackTrace:stackTrace type:type];
    } else {
        dispatch_async(dispatch_get_main_queue(), ^{
            [_lunarConsolePlugin logMessage:message stackTrace:stackTrace type:type];
        });
    }
}

void __lunar_console_action_register(int actionId, const char *actionNameStr)
{
    NSString *actionName = [[NSString alloc] initWithUTF8String:actionNameStr];

    if ([NSThread isMainThread]) {
        [_lunarConsolePlugin registerActionWithId:actionId name:actionName];
    } else {
        dispatch_async(dispatch_get_main_queue(), ^{
            [_lunarConsolePlugin registerActionWithId:actionId name:actionName];
        });
    }
}

void __lunar_console_action_unregister(int actionId)
{
    if ([NSThread isMainThread]) {
        [_lunarConsolePlugin unregisterActionWithId:actionId];
    } else {
        dispatch_async(dispatch_get_main_queue(), ^{
            [_lunarConsolePlugin unregisterActionWithId:actionId];
        });
    }
}

void __lunar_console_cvar_register(int entryId, const char *nameStr, const char *typeStr, const char *valueStr, const char *defaultValueStr, int flags, BOOL hasRange, float min, float max, const char *valuesStr)
{
    lunar_dispatch_main(^{
        NSString *name = [[NSString alloc] initWithUTF8String:nameStr];
        NSString *type = [[NSString alloc] initWithUTF8String:typeStr];
        NSString *value = [[NSString alloc] initWithUTF8String:valueStr];
        NSString *defaultValue = [[NSString alloc] initWithUTF8String:defaultValueStr];
        NSString *values = valuesStr ? [[NSString alloc] initWithUTF8String:valuesStr] : nil;

        LUCVar *cvar = [_lunarConsolePlugin registerVariableWithId:entryId name:name type:type value:value defaultValue:defaultValue];
        cvar.values = [values componentsSeparatedByString:@","];
        cvar.flags = flags;
        if (!isnan(min) && !isnan(max)) {
            cvar.range = LUMakeCVarRange(min, max);
        }
    });
}

void __lunar_console_cvar_update(int entryId, const char *valueStr)
{
    lunar_dispatch_main(^{
        NSString *value = [[NSString alloc] initWithUTF8String:valueStr];
        [_lunarConsolePlugin setValue:value forVariableWithId:entryId];
    });
}
