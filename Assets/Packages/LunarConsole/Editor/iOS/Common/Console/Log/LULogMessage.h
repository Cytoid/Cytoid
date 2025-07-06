//
//  LULogMessage.h
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

@class LUAttributedTextSkin;

@interface LURichTextTag : NSObject

@property (nonatomic, readonly) NSRange range;

- (instancetype)initWithRange:(NSRange)range;

@end

typedef enum : NSUInteger {
    LURichTextStyleBold,
    LURichTextStyleItalic,
    LURichTextStyleBoldItalic
} LURichTextStyle;

@interface LURichTextStyleTag : LURichTextTag

@property (nonatomic, readonly) LURichTextStyle style;

- (instancetype)initWithStyle:(LURichTextStyle)style range:(NSRange)range;

@end

@interface LURichTextColorTag : LURichTextTag

@property (nonatomic, readonly) UIColor * color;

- (instancetype)initWithColor:(UIColor *)color range:(NSRange)range;

@end

@interface LULogMessage : NSObject

@property (nonatomic, readonly, nullable) NSString *text;
@property (nonatomic, readonly, nullable) NSArray<LURichTextTag *> *tags;
@property (nonatomic, readonly) NSUInteger length;

- (instancetype)initWithText:(nullable NSString *)text tags:(NSArray<LURichTextTag *> * _Nullable)tags;

+ (instancetype)fromRichText:(nullable NSString *)text;

@end

@interface LULogMessage (AttributedString)

- (NSAttributedString *)createAttributedTextWithSkin:(LUAttributedTextSkin *)skin;
- (NSAttributedString *)createAttributedTextWithSkin:(LUAttributedTextSkin *)skin attributes:(nullable NSDictionary<NSAttributedStringKey, id> *)attrs;


@end

NS_ASSUME_NONNULL_END
