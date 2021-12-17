//
//  LUTextField.m
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


#import "LUTextField.h"

#import "Lunar.h"

@interface LUTextField () <UITextFieldDelegate>

@property (nonatomic, strong) NSString *lastText;

@end

@implementation LUTextField

- (instancetype)initWithCoder:(NSCoder *)aDecoder
{
    self = [super initWithCoder:aDecoder];
    if (self) {
        self.backgroundColor = [LUTheme mainTheme].variableEditBackground;
        self.textColor = [LUTheme mainTheme].variableEditTextColor;
        self.delegate = self;
    }
    return self;
}

#pragma mark -
#pragma mark UITextFieldDelegate

- (BOOL)textFieldShouldReturn:(UITextField *)textField
{
    [textField resignFirstResponder];
    return NO;
}

- (void)textFieldDidBeginEditing:(UITextField *)textField
{
    self.lastText = textField.text;
}

- (void)textFieldDidEndEditing:(UITextField *)textField
{
    NSString *valueText = textField.text;
    if (![self isTextValid:valueText]) {
        if ([self.textInputDelegate respondsToSelector:@selector(textFieldInputDidBecomeInvalid:)]) {
            [self.textInputDelegate textFieldInputDidBecomeInvalid:self];
        }
        self.text = self.lastText;
    } else {
        if ([self.textInputDelegate respondsToSelector:@selector(textFieldDidEndEditing:)]) {
            [self.textInputDelegate textFieldDidEndEditing:self];
        }
    }
}

#pragma mark -
#pragma mark Text Validation

- (BOOL)isTextValid:(NSString *)text
{
    return _textValidator == nil || [_textValidator isTextValid:text];
}

@end

@implementation LUTextFieldIntegerInputValidator

- (BOOL)isTextValid:(NSString *)text
{
    return LUStringTryParseInteger(text, NULL);
}

@end

@implementation LUTextFieldFloatInputValidator

- (BOOL)isTextValid:(NSString *)text
{
    return LUStringTryParseFloat(text, NULL);
}

@end
