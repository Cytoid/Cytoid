//
//  LUActionButton.m
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


#import "LUActionButton.h"

#import "Lunar.h"

@interface LUActionButton ()
{
    UIGestureRecognizer * _longPressGestureRecognizer;
    UIGestureRecognizer * _panViewGestureRecognizer;
}
@end

@implementation LUActionButton

- (instancetype)initWithCoder:(NSCoder *)aDecoder
{
    self = [super initWithCoder:aDecoder];
    if (self)
    {
        LUButtonSkin *skin = [LUTheme mainTheme].actionButtonLargeSkin;
        
        [self setBackgroundImage:skin.normalImage forState:UIControlStateNormal];
        [self setBackgroundImage:skin.selectedImage forState:UIControlStateHighlighted];
        
        _longPressGestureRecognizer = [[UILongPressGestureRecognizer alloc] initWithTarget:self
                                                                                    action:@selector(handleLongPressGesture:)];
        [self addGestureRecognizer:_longPressGestureRecognizer];
    }
    return self;
}

- (void)handleLongPressGesture:(UILongPressGestureRecognizer *)gestureRecognizer
{
    [self removeGestureRecognizer:gestureRecognizer];
    
    _panViewGestureRecognizer = [LUPanViewGestureRecognizer new];
    [self addGestureRecognizer:_panViewGestureRecognizer];
}

@end
