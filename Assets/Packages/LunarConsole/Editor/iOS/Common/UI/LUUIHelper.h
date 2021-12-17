//
//  LUUIHelper.h
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

NS_ASSUME_NONNULL_BEGIN

@interface LUAlertAction : NSObject

@property (nonatomic, readonly) NSString *title;
@property (nullable, nonatomic, copy, readonly) void (^handler)(LUAlertAction *action);

- (instancetype)initWithTitle:(NSString *)title handler:(void (^ __nullable)(LUAlertAction *action))handler;

@end

@interface LUUIHelper : NSObject

+ (void)showAlertViewWithTitle:(NSString *)title message:(NSString *)message;
+ (void)showAlertViewWithTitle:(NSString *)title message:(NSString *)message actions:(NSArray<LUAlertAction *> *)actions;
+ (void)view:(UIView *)view centerInParent:(UIView *)parent;
+ (CGRect)safeAreaRect;

@end

NS_ASSUME_NONNULL_END
