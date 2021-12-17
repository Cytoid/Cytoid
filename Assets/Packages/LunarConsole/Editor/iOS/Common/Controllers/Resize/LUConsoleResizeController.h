//
//  LUConsoleResizeController.h
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

@class LUConsoleResizeController;

@protocol LUConsoleResizeControllerDelegate <NSObject>

- (void)consoleResizeControllerDidClose:(LUConsoleResizeController *)controller;

@end

@interface LUConsoleResizeController : UIViewController

@property (nonatomic, weak) id<LUConsoleResizeControllerDelegate> delegate;

- (instancetype)initWithMaxSize:(CGSize)maxSize topConstraint:(NSLayoutConstraint *)topConstraint leadingConstraint:(NSLayoutConstraint *)leadingConstraint bottomConstraint:(NSLayoutConstraint *)bottomConstraint trailingConstraint:(NSLayoutConstraint *)trailingConstraint;

@end
