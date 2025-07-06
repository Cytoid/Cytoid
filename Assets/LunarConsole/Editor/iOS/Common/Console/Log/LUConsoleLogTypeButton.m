//
//  LUConsoleLogTypeButton.m
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


#import "LUConsoleLogTypeButton.h"

#import "Lunar.h"

static const NSUInteger kCountMax = 999;

@interface LUConsoleLogTypeButton ()
{
    NSString * _initialText;
}

@end

@implementation LUConsoleLogTypeButton


- (void)awakeFromNib
{
    [super awakeFromNib];
    
    _initialText = self.titleLabel.text;
    _count = INT32_MAX;

    // text color
    [self setTitleColor:[LUTheme mainTheme].logButtonTitleColor forState:UIControlStateNormal];
    [self setTitleColor:[LUTheme mainTheme].logButtonTitleSelectedColor forState:UIControlStateSelected];
    
    // images
    UIImage *normalImage = [self imageForState:UIControlStateNormal];
    UIImage *selectedImage = [self image:normalImage changeAlpha:0.1];
    [self setImage:selectedImage forState:UIControlStateSelected];
    
    self.count = 0;
    self.on = NO;
}

- (void)setCount:(NSUInteger)count
{
    if (_count != count)
    {
        if (count < kCountMax)
        {
            NSString *countText = [[NSString alloc] initWithFormat:@"%ld", (unsigned long)count];
            [self setCountText:countText];
        }
        else if (_count < kCountMax)
        {
            [self setCountText:_initialText];
        }
        
        _count = count;
    }
}

#pragma mark -
#pragma mark Image Helpers

- (UIImage *)image:(UIImage *)image changeAlpha:(CGFloat)alpha
{
    UIGraphicsBeginImageContextWithOptions(image.size, NO, image.scale);
    
    CGContextRef ctx = UIGraphicsGetCurrentContext();
    CGRect area = CGRectMake(0, 0, image.size.width, image.size.height);
    
    CGContextScaleCTM(ctx, 1, -1);
    CGContextTranslateCTM(ctx, 0, -area.size.height);
    
    CGContextSetBlendMode(ctx, kCGBlendModeMultiply);
    
    CGContextSetAlpha(ctx, alpha);
    
    CGContextDrawImage(ctx, area, image.CGImage);
    
    UIImage *result = UIGraphicsGetImageFromCurrentImageContext();
    
    UIGraphicsEndImageContext();
    
    return result;
}

#pragma mark -
#pragma mark Properties

- (void)setCountText:(NSString *)text
{
    [self setTitle:text forState:UIControlStateNormal];
    [self setTitle:text forState:UIControlStateSelected];
}

@end
