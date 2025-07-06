//
//  LUConsoleLogEntryTableViewCell.h
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

@class LULogMessage;

@interface LUConsoleLogEntryTableViewCell : UITableViewCell

@property (nonatomic, strong, nullable) UIImage      * icon;
@property (nonatomic, strong, nullable) UIColor      * messageColor;
@property (nonatomic, strong, nullable) UIColor      * cellColor;

+ (nonnull instancetype)cellWithFrame:(CGRect)frame cellIdentifier:(nullable NSString *)cellIdentifier;
- (nonnull instancetype)initWithFrame:(CGRect)frame cellIdentifier:(nullable NSString *)cellIdentifier;

- (void)setMessage:(LULogMessage *)message;
- (void)setSize:(CGSize)size;

+ (CGFloat)heightForCellWithText:(nullable NSString *)text width:(CGFloat)width;

@end

@interface LUConsoleCollapsedLogEntryTableViewCell : LUConsoleLogEntryTableViewCell

@property (nonatomic, assign) NSInteger collapsedCount;

@end

@interface LUConsoleOverlayLogEntryTableViewCell : LUConsoleLogEntryTableViewCell

+ (CGFloat)heightForCellWithText:(nullable NSString *)text width:(CGFloat)width;

- (void)setMessage:(LULogMessage *)message attributes:(NSDictionary<NSAttributedStringKey, id> *)attributes;

@end

NS_ASSUME_NONNULL_END
