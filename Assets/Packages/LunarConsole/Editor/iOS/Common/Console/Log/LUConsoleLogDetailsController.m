//
//  LUConsoleLogDetailsController.m
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


#import "LUConsoleLogDetailsController.h"

#import "Lunar.h"

#define NO_STACK_TRACE_WARNING @"No stack trace found.\n\nMake sure \"Development Build\" checkbox is checked under \"Build Settings\" (File ▶ Build Settings...)"

@interface LUConsoleLogDetailsController ()
{
    LUConsoleLogEntry * _entry;
}

@property (nonatomic, weak) IBOutlet UITextView * stackTraceView;

@end

@implementation LUConsoleLogDetailsController

- (instancetype)initWithEntry:(LUConsoleLogEntry *)entry
{
    self = [super initWithNibName:NSStringFromClass([self class]) bundle:nil];
    if (self)
    {
        _entry = entry;
        
        self.popupIcon = _entry.icon;
        self.popupTitle = @"Message Details";
        self.popupButtons = @[
            [LUConsolePopupButton buttonWithIcon:LUGetImage(@"lunar_console_icon_button_clipboard") target:self action:@selector(onCopyToClipboard:)]
        ];
    }
    return self;
}

- (void)viewDidLoad
{
    [super viewDidLoad];
    
    LUTheme *theme = [LUTheme mainTheme];
    
    NSString *stackTrace = [_entry hasStackTrace] ?
        [LUStacktrace optimizeStacktrace:_entry.stackTrace] : NO_STACK_TRACE_WARNING;
    
    NSDictionary *attributes = @{
        NSFontAttributeName : theme.fontSmall,
        NSForegroundColorAttributeName : theme.logMessageStacktraceColor
    };
    
    NSString *text = [NSString stringWithFormat:@"%@\n\n%@", _entry.message, stackTrace];
    NSMutableAttributedString *attributedText = [[NSMutableAttributedString alloc] initWithString:text attributes:attributes];
    [attributedText addAttribute:NSFontAttributeName
                           value:theme.logMessageDetailFont
                           range:NSMakeRange(0, _entry.message.length)];
    [attributedText addAttribute:NSForegroundColorAttributeName
                           value:theme.cellLog.textColor
                           range:NSMakeRange(0, _entry.message.length)];
    _stackTraceView.backgroundColor = theme.tableColor;
    _stackTraceView.attributedText = attributedText;
    self.view.backgroundColor = theme.tableColor;
    
    // update layout
    [self.view layoutIfNeeded];
}

#pragma mark -
#pragma mark Copy to clipboard

- (void)onCopyToClipboard:(id)sender
{
    UIPasteboard *pastboard = [UIPasteboard generalPasteboard];
    
    NSString *text = _entry.message.text;
    if ([_entry hasStackTrace])
    {
        text = [text stringByAppendingFormat:@"\n\n%@", _entry.stackTrace];
    }
    [pastboard setString:text];
}

@end
