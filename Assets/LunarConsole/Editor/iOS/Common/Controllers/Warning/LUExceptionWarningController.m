//
//  LUExceptionWarningController.m
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


#import "LUExceptionWarningController.h"

#import "Lunar.h"

@interface LUExceptionWarningController () {
    LULogMessage *_message;
}

@property (nonatomic, weak) IBOutlet UILabel *errorLabel;

@end

@implementation LUExceptionWarningController

- (instancetype)initWithMessage:(LULogMessage *)message
{
    self = [super initWithNibName:NSStringFromClass([self class]) bundle:nil];
    if (self) {
        _message = message;
    }
    return self;
}


- (void)viewDidLoad
{
    [super viewDidLoad];

    if (_message.tags.count > 0)
    {
        _errorLabel.attributedText = [_message createAttributedTextWithSkin:[LUTheme mainTheme].attributedTextSkin];
    }
    else
    {
        _errorLabel.text = _message.text;
    }
}

#pragma mark -
#pragma mark Actions

- (IBAction)onShowButton:(id)sender
{
    if ([_delegate respondsToSelector:@selector(exceptionWarningControllerDidShow:)]) {
        [_delegate exceptionWarningControllerDidShow:self];
    }
}

- (IBAction)onDismissButton:(id)sender
{
    if ([_delegate respondsToSelector:@selector(exceptionWarningControllerDidDismiss:)]) {
        [_delegate exceptionWarningControllerDidDismiss:self];
    }
}

@end
