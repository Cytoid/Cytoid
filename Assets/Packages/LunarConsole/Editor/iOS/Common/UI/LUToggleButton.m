//
//  LUToggleButton.m
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


#import "LUToggleButton.h"

@implementation LUToggleButton

- (instancetype)initWithCoder:(NSCoder *)aDecoder
{
    self = [super initWithCoder:aDecoder];
    if (self)
    {
        [self addTarget:self action:@selector(onClick:) forControlEvents:UIControlEventTouchUpInside];
    }
    return self;
}

#pragma mark -
#pragma mark Actions

- (void)onClick:(id)sender
{
    self.on = !self.isOn;
}

#pragma mark -
#pragma mark Properties

- (BOOL)isOn
{
    return self.selected;
}

- (void)setOn:(BOOL)on
{
    if (self.selected != on)
    {
        self.selected = on;
        if ([_delegate respondsToSelector:@selector(toggleButtonStateChanged:)])
        {
            [_delegate toggleButtonStateChanged:self];
        }
    }
}

@end
