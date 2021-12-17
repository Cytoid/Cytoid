//
//  LUTheme.h
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

@interface LUCellSkin : NSObject

+ (instancetype)cellSkin;

@property (nonatomic, readonly) UIImage *icon;
@property (nonatomic, readonly) UIColor *textColor;
@property (nonatomic, readonly) UIColor *backgroundColorLight;
@property (nonatomic, readonly) UIColor *backgroundColorDark;
@property (nonatomic, readonly) UIColor *overlayTextColor;

@end

@interface LUButtonSkin : NSObject

+ (instancetype)buttonSkin;

@property (nonatomic, readonly) UIImage *normalImage;
@property (nonatomic, readonly) UIImage *selectedImage;
@property (nonatomic, readonly) UIFont  *font;

@end

@interface LUAttributedTextSkin : NSObject

@property (nonatomic, readonly) UIFont *regularFont;
@property (nonatomic, readonly) UIFont *boldFont;
@property (nonatomic, readonly) UIFont *italicFont;
@property (nonatomic, readonly) UIFont *boldItalicFont;

@end

@interface LUTheme : NSObject

@property (nonatomic, readonly) LUAttributedTextSkin *attributedTextSkin;

@property (nonatomic, readonly) UIColor *statusBarColor;
@property (nonatomic, readonly) UIColor *statusBarTextColor;

@property (nonatomic, readonly) UIColor *tableColor;
@property (nonatomic, readonly) UIColor *logButtonTitleColor;
@property (nonatomic, readonly) UIColor *logButtonTitleSelectedColor;

@property (nonatomic, readonly) LUCellSkin *cellLog;
@property (nonatomic, readonly) LUCellSkin *cellError;
@property (nonatomic, readonly) LUCellSkin *cellWarning;

@property (nonatomic, readonly) UIColor *backgroundColorLight;
@property (nonatomic, readonly) UIColor *backgroundColorDark;

@property (nonatomic, readonly) UIFont *font;
@property (nonatomic, readonly) UIFont *fontOverlay;
@property (nonatomic, readonly) UIFont *fontSmall;
@property (nonatomic, readonly) NSLineBreakMode lineBreakMode;

@property (nonatomic, readonly) CGFloat cellHeight;
@property (nonatomic, readonly) CGFloat indentHor;
@property (nonatomic, readonly) CGFloat indentVer;
@property (nonatomic, readonly) CGFloat cellHeightTiny;
@property (nonatomic, readonly) CGFloat indentHorTiny;
@property (nonatomic, readonly) CGFloat indentVerTiny;
@property (nonatomic, readonly) CGFloat buttonWidth;
@property (nonatomic, readonly) CGFloat buttonHeight;

@property (nonatomic, readonly) LUButtonSkin *actionButtonLargeSkin;

@property (nonatomic, readonly) UIImage *collapseBackgroundImage;
@property (nonatomic, readonly) UIColor *collapseBackgroundColor;
@property (nonatomic, readonly) UIColor *collapseTextColor;

@property (nonatomic, readonly) UIFont  *actionsWarningFont;
@property (nonatomic, readonly) UIColor *actionsWarningTextColor;
@property (nonatomic, readonly) UIFont  *actionsFont;
@property (nonatomic, readonly) UIColor *actionsTextColor;
@property (nonatomic, readonly) UIColor *actionsBackgroundColorLight;
@property (nonatomic, readonly) UIColor *actionsBackgroundColorDark;
@property (nonatomic, readonly) UIFont  *actionsGroupFont;
@property (nonatomic, readonly) UIColor *actionsGroupTextColor;
@property (nonatomic, readonly) UIColor *actionsGroupBackgroundColor;

@property (nonatomic, readonly) UIFont  *contextMenuFont;
@property (nonatomic, readonly) UIColor *contextMenuBackgroundColor;
@property (nonatomic, readonly) UIColor *contextMenuTextColor;
@property (nonatomic, readonly) UIColor *contextMenuTextHighlightColor;
@property (nonatomic, readonly) UIColor *contextMenuTextProColor;
@property (nonatomic, readonly) UIColor *contextMenuTextProHighlightColor;

@property (nonatomic, readonly) UIFont  *variableEditFont;
@property (nonatomic, readonly) UIColor *variableEditTextColor;
@property (nonatomic, readonly) UIColor *variableEditBackground;
@property (nonatomic, readonly) UIColor *variableTextColor;
@property (nonatomic, readonly) UIColor *variableVolatileTextColor;

@property (nonatomic, readonly) UIFont  *enumButtonFont;
@property (nonatomic, readonly) UIColor *enumButtonTitleColor;

@property (nonatomic, readonly) UIColor *switchTintColor;

@property (nonatomic, readonly) UIImage *settingsIconImage;
@property (nonatomic, readonly) UIColor *settingsTextColorUnavailable;

@property (nonatomic, readonly) UIFont  *logMessageDetailFont;
@property (nonatomic, readonly) UIColor *logMessageStacktraceColor;

+ (LUTheme *)mainTheme;

@end

