//
//  LUConsolePopupController.h
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

@class LUConsolePopupButton;
@class LUConsolePopupController;

extern NSString * const LUConsolePopupControllerWillAppearNotification;
extern NSString * const LUConsolePopupControllerWillDisappearNotification;

@protocol LUConsolePopupControllerDelegate <NSObject>

- (void)popupControllerDidDismiss:(LUConsolePopupController *)controller;

@end

typedef void(^LUConsolePopupButtonCallback)(LUConsolePopupButton *button);

@interface LUConsolePopupButton : NSObject

+ (instancetype)buttonWithIcon:(UIImage *)icon target:(id)target action:(SEL)action;
- (instancetype)initWithIcon:(UIImage *)icon target:(id)target action:(SEL)action;

@end

@interface LUConsolePopupController : LUViewController

@property (nonatomic, readonly) LUViewController *contentController;
@property (nonatomic, weak) id<LUConsolePopupControllerDelegate> popupDelegate;

- (instancetype)initWithContentController:(LUViewController *)contentController;

- (void)presentFromController:(UIViewController *)controller animated:(BOOL)animated;
- (void)dismissAnimated:(BOOL)animated;

- (void)setLearnMoreTitle:(NSString *)title target:(id)target action:(SEL)action;

@end
