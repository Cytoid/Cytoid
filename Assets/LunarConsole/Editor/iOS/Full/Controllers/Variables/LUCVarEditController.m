//
//  LUCVarEditController.m
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


#import "LUCVarEditController.h"

#import "Lunar-Full.h"

@implementation LUCVarEditController

- (instancetype)initWithVariable:(LUCVar *)variable
{
    self = [super initWithNibName:NSStringFromClass([self class]) bundle:nil];
    if (self) {
        _variable = variable;
        self.popupTitle = variable.name;
        self.popupIcon = LUGetImage(@"lunar_console_icon_settings");
        self.popupButtons = @[
            [LUConsolePopupButton buttonWithIcon:LUGetImage(@"lunar_console_icon_button_variable_reset")
                        target:self
                        action:@selector(onResetButton:)]
        ];
    }
    return self;
}

- (void)viewDidLoad
{
    [super viewDidLoad];
    
    self.view.backgroundColor = [LUTheme mainTheme].backgroundColorDark;
}

#pragma mark -
#pragma mark Actions

- (void)onResetButton:(id)sender
{
    [self notifyValueUpdate:_variable.defaultValue];
}

- (void)notifyValueUpdate:(NSString *)value
{
    if (![_variable.value isEqualToString:value])
    {
        if ([_delegate respondsToSelector:@selector(editController:didChangeValue:)])
        {
            [_delegate editController:self didChangeValue:value];
        }
    }
}

#pragma mark -
#pragma mark Popup Controller

- (CGSize)preferredPopupSize
{
    return CGSizeMake(0, 70);
}

@end

@interface LUCVarValueController () <UITextFieldDelegate>

@property (nonatomic, weak) IBOutlet UISlider * slider;
@property (nonatomic, weak) IBOutlet UITextField * textField;
@property (nonatomic, weak) IBOutlet UILabel *errorLabel;
@property (nonatomic, weak) IBOutlet NSLayoutConstraint *errorLabelHeightConstraint;

@end

@implementation LUCVarValueController

- (instancetype)initWithVariable:(LUCVar *)variable
{
    self = [super initWithVariable:variable];
    if (self) {
    }
    return self;
}

- (void)viewDidLoad
{
    [super viewDidLoad];
    
    if (self.variable.type == LUCVarTypeFloat && self.variable.hasRange)
    {
        CGFloat min = self.variable.range.min;
        CGFloat max = self.variable.range.max;
        if (max - min > 0.000001)
        {
            _slider.minimumValue = min;
            _slider.maximumValue = max;
            _slider.value = [self.variable.value floatValue];
            _errorLabelHeightConstraint.constant = 0.0f;
        }
        else
        {
            _slider.enabled = NO;
            _errorLabel.text = [NSString stringWithFormat:@"Invalid range [%g, %g]", min, max];
        }
    }
    else
    {
        _slider.hidden = YES;
        _errorLabelHeightConstraint.constant = 0.0f;
    }
    
    _textField.text = self.variable.value;
	LU_SET_ACCESSIBILITY_IDENTIFIER(_textField, @"Editor Input Field")
}

#pragma mark -
#pragma mark Actions

- (IBAction)sliderValueChanged:(id)sender
{
    UISlider *slider = sender;
    _textField.text = [[NSString alloc] initWithFormat:@"%g", slider.value];
}

- (IBAction)sliderEditingFinished:(id)sender
{
    UISlider *slider = sender;
    NSString *value = [[NSString alloc] initWithFormat:@"%g", slider.value];
    
    _textField.text = value;
    [self notifyValueUpdate:value];
}

- (void)onResetButton:(id)sender
{
    _textField.text = self.variable.defaultValue;
    if (self.variable.type == LUCVarTypeFloat && self.variable.hasRange)
    {
        _slider.value = [self.variable.defaultValue floatValue];
    }
    [super onResetButton:sender];
}

#pragma mark -
#pragma mark UITextFieldDelegate

- (void)textFieldDidEndEditing:(UITextField *)textField
{
    NSString *valueText = textField.text;
    if ([self isValidValue:valueText])
    {
        if (self.variable.type == LUCVarTypeFloat)
        {
            float value;
            LUStringTryParseFloat(valueText, &value);
            if (self.variable.hasRange)
            {
                if (value < self.variable.range.min)
                {
                    value = self.variable.range.min;
                }
                else if (value > self.variable.range.max)
                {
                    value = self.variable.range.max;
                }
                _slider.value = value;
            }
            [self notifyValueUpdate:[[NSString alloc] initWithFormat:@"%g", value]];
        }
        else
        {
            [self notifyValueUpdate:valueText];
        }
    }
    else
    {
        LUDisplayAlertView(@"Input Error", [NSString stringWithFormat:@"Invalid value: '%@'", valueText]);
    }
}

- (BOOL)textFieldShouldReturn:(UITextField *)textField
{
    [textField resignFirstResponder];
    return NO;
}

#pragma mark -
#pragma mark Value

- (BOOL)isValidValue:(NSString *)value
{
    switch (self.variable.type)
    {
        case LUCVarTypeFloat:
            return LUStringTryParseFloat(value, NULL);
        case LUCVarTypeInteger:
            return LUStringTryParseInteger(value, NULL);
        default:
            return YES;
    }
}

#pragma mark -
#pragma mark Popup Controller

- (CGSize)preferredPopupSize
{
    return CGSizeMake(0, 70);
}

@end

@interface LUCVarEnumController () <UIPickerViewDelegate, UIPickerViewDataSource>

@property (nonatomic, weak) IBOutlet UIPickerView * pickerView;

@end

@implementation LUCVarEnumController

- (void)viewDidLoad
{
    [super viewDidLoad];
    
    NSUInteger row = [self.variable.values indexOfObject:self.variable.value];
    [self.pickerView selectRow:row inComponent:0 animated:NO];
}

- (void)onResetButton:(id)sender
{
    [super onResetButton:sender];
    
    NSUInteger row = [self.variable.values indexOfObject:self.variable.value];
    [self.pickerView selectRow:row inComponent:0 animated:YES];
}

#pragma mark -
#pragma mark UIPickerViewDelegate

- (NSAttributedString *)pickerView:(UIPickerView *)pickerView attributedTitleForRow:(NSInteger)row forComponent:(NSInteger)component
{
    NSString *value = self.variable.values[row];
    return [[NSAttributedString alloc] initWithString:value attributes:@{ NSForegroundColorAttributeName : LUTheme.mainTheme.variableTextColor }];
}

- (void)pickerView:(UIPickerView *)pickerView didSelectRow:(NSInteger)row inComponent:(NSInteger)component
{
    NSString *value = self.variable.values[row];
    [self notifyValueUpdate:value];
}

#pragma mark -
#pragma mark UIPickerViewDataSource

// returns the number of 'columns' to display.
- (NSInteger)numberOfComponentsInPickerView:(UIPickerView *)pickerView
{
    return 1;
}

// returns the # of rows in each component..
- (NSInteger)pickerView:(UIPickerView *)pickerView numberOfRowsInComponent:(NSInteger)component
{
    return self.variable.values.count;
}

#pragma mark -
#pragma mark Popup Controller

- (CGSize)preferredPopupSize
{
    return CGSizeMake(0, 216);
}


@end
