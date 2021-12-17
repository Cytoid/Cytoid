//
//  LUConsolePlugin.h
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


#import <Foundation/Foundation.h>

#import "LUObject.h"
#import "LUConsoleLogEntry.h"

@class LUActionRegistry;
@class LUConsole;
@class LUConsolePlugin;
@class LUCVar;
@class LUPluginSettings;
@class LUWindow;

@protocol LUConsolePluginDelegate <NSObject>

@optional
- (void)consolePluginDidOpenController:(LUConsolePlugin *)plugin;
- (void)consolePluginDidCloseController:(LUConsolePlugin *)plugin;

@end

extern BOOL LUConsoleIsFreeVersion;
extern BOOL LUConsoleIsFullVersion;

extern NSString * const LUConsoleCheckFullVersionNotification;
extern NSString * const LUConsoleCheckFullVersionNotificationSource;

@interface LUConsolePlugin : LUObject

@property (nonatomic, readonly) LUWindow         * consoleWindow;
@property (nonatomic, readonly) LUWindow         * actionOverlayWindow;
@property (nonatomic, readonly) LUWindow         * warningWindow;
@property (nonatomic, readonly) LUConsole        * console;
@property (nonatomic, readonly) LUActionRegistry * actionRegistry;
@property (nonatomic, readonly) NSString         * version;

@property (nonatomic, assign) NSInteger capacity;
@property (nonatomic, assign) NSInteger trim;

@property (nonatomic, readonly) LUPluginSettings *settings;
@property (nonatomic, weak) id<LUConsolePluginDelegate> delegate;

- (instancetype)initWithTargetName:(NSString *)targetName
                        methodName:(NSString *)methodName
                           version:(NSString *)version
                      settingsJson:(NSString *)settingsJson;

- (void)start;
- (void)stop;

- (void)showConsole;
- (void)hideConsole;

- (void)showOverlay;
- (void)hideOverlay;

- (void)logMessage:(NSString *)message stackTrace:(NSString *)stackTrace type:(LUConsoleLogType)type;
- (void)clearConsole;
- (void)clearState;

- (void)registerActionWithId:(int)actionId name:(NSString *)name;
- (void)unregisterActionWithId:(int)actionId;

- (LUCVar *)registerVariableWithId:(int)entryId name:(NSString *)name type:(NSString *)type value:(NSString *)value;
- (LUCVar *)registerVariableWithId:(int)entryId name:(NSString *)name type:(NSString *)type value:(NSString *)value defaultValue:(NSString *)defaultValue;
- (LUCVar *)registerVariableWithId:(int)entryId name:(NSString *)name type:(NSString *)type value:(NSString *)value defaultValue:(NSString *)defaultValue values:(NSArray<NSString *> *)values;
- (void)setValue:(NSString *)value forVariableWithId:(int)variableId;

- (void)enableGestureRecognition;
- (void)disableGestureRecognition;

- (void)sendScriptMessageName:(NSString *)name;
- (void)sendScriptMessageName:(NSString *)name params:(NSDictionary *)params;

@end
