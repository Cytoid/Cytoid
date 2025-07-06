//
//  LUConsoleLogMenuController.h
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


#import <UIKit/UIKit.h>

#import "LUViewController.h"

@class LUConsoleLogMenuController;

@interface LUConsoleLogMenuControllerButton : NSObject

@property (nonatomic, strong) UIColor *textColor;
@property (nonatomic, strong) UIColor *textHighlightedColor;

+ (instancetype)buttonWithTitle:(NSString *)title target:(id)target action:(SEL)action;
- (instancetype)initWithTitle:(NSString *)title target:(id)target action:(SEL)action;

@end

@protocol LUConsoleLogMenuControllerDelegate <NSObject>

- (void)menuControllerDidRequestClose:(LUConsoleLogMenuController *)controller;

@end


@interface LUConsoleLogMenuController : LUViewController

@property (nonatomic, weak) id<LUConsoleLogMenuControllerDelegate> delegate;

- (LUConsoleLogMenuControllerButton *)addButtonTitle:(NSString *)title target:(id)target action:(SEL)action;

@end
