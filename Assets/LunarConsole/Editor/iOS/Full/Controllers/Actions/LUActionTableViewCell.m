//
//  LUActionTableViewCell.m
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


#import "Lunar-Full.h"

#import "LUActionTableViewCell.h"

@implementation LUActionTableViewCell

+ (instancetype)cellWithReuseIdentifier:(nullable NSString *)reuseIdentifier
{
    return [[[self class] alloc] initWithReuseIdentifier:reuseIdentifier];
}

- (instancetype)initWithReuseIdentifier:(nullable NSString *)reuseIdentifier
{
    self = [super initWithStyle:UITableViewCellStyleDefault reuseIdentifier:reuseIdentifier];
    if (self)
    {
        LUTheme *theme = [LUTheme mainTheme];
        
        self.textLabel.font = theme.actionsFont;
        self.textLabel.textColor = theme.actionsTextColor;
        LU_SET_ACCESSIBILITY_IDENTIFIER(self.textLabel, @"Action Title");
    }
    return self;
}

#pragma mark -
#pragma mark Properties

- (NSString *)title
{
    return self.textLabel.text;
}

- (void)setTitle:(NSString *)title
{
    self.textLabel.text = title;
}

- (UIColor *)cellColor
{
    return self.contentView.backgroundColor;
}

- (void)setCellColor:(UIColor * __nullable)cellColor
{
    self.contentView.superview.backgroundColor = cellColor;
}

@end
