//
//  LUConsoleController.h
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


#import "LUViewController.h"
#import "LUSerializableObject.h"

@class LUConsolePlugin;
@class LUConsoleController;

extern NSString * const LUConsoleControllerDidResizeNotification;

@protocol LUConsoleControllerDelegate <NSObject>

@optional
- (void)consoleControllerDidOpen:(LUConsoleController *)controller;
- (void)consoleControllerDidClose:(LUConsoleController *)controller;

@end

@interface LUConsoleControllerState : LUSerializableObject

@property (nonatomic, assign) UIEdgeInsets controllerInsets;

@end

@interface LUConsoleController : LUViewController

@property (nonatomic, assign) BOOL scrollEnabled;
@property (nonatomic, strong) NSArray<NSString *> *emails;
@property (nonatomic, weak) id<LUConsoleControllerDelegate> delegate;

+ (instancetype)controllerWithPlugin:(LUConsolePlugin *)plugin;
- (instancetype)initWithPlugin:(LUConsolePlugin *)plugin;

@end
