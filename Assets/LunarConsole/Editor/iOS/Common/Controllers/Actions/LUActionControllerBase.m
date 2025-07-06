//
//  LUActionControllerBase.m
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


#import "Lunar.h"

#import "LUActionControllerBase_Inheritance.h"

NSString * const LUActionControllerDidChangeVariable = @"LUActionControllerDidChangeVariable";
NSString * const LUActionControllerDidChangeVariableKeyVariable = @"variable";

NSString * const LUActionControllerDidSelectAction = @"LUActionControllerDidSelectAction";
NSString * const LUActionControllerDidSelectActionKeyAction = @"action";

@implementation LUActionControllerBase

+ (instancetype)controllerWithActionRegistry:(LUActionRegistry *)actionRegistry
{
    return [[self alloc] initWithActionRegistry:actionRegistry];
}

- (instancetype)initWithActionRegistry:(LUActionRegistry *)actionRegistry
{
    self = [super initWithNibName:NSStringFromClass([self class]) bundle:nil];
    if (self)
    {
    }
    return self;
}

- (void)viewDidLoad
{
    [super viewDidLoad];
    
    LUTheme *theme = [LUTheme mainTheme];
    
    // background
    self.view.opaque = YES;
    self.view.backgroundColor = theme.tableColor;
    
    // table view
    _tableView.backgroundColor = theme.tableColor;
    
    // no actions warning
    _noActionsWarningView.backgroundColor = theme.tableColor;
    _noActionsWarningView.opaque = YES;
    _noActionsWarningLabel.font = theme.actionsWarningFont;
    _noActionsWarningLabel.textColor = theme.actionsWarningTextColor;
    
    [self updateNoActionWarningView];
    
    // accessibility
    LU_SET_ACCESSIBILITY_IDENTIFIER(_noActionsWarningView, @"No Actions Warning View");
}

#pragma mark -
#pragma mark No actions warning view

- (void)updateNoActionWarningView
{
    [self setNoActionsWarningViewHidden:NO];
}

- (void)setNoActionsWarningViewHidden:(BOOL)hidden
{
    _tableView.hidden = !hidden;
    _filterBar.hidden = !hidden;
    _noActionsWarningView.hidden = hidden;
}

#pragma mark -
#pragma Interface Builder actions

- (IBAction)onInfoButton:(id)sender
{
    if (LUConsoleIsFreeVersion)
    {
        [[NSNotificationCenter defaultCenter] postNotificationName:LUConsoleCheckFullVersionNotification object:nil userInfo:@{ LUConsoleCheckFullVersionNotificationSource : @"actions" }];
        
        [[UIApplication sharedApplication] openURL:[NSURL URLWithString:@"https://goo.gl/TMnxBe"]];
    }
    else
    {
        [[UIApplication sharedApplication] openURL:[NSURL URLWithString:@"https://goo.gl/in0obv"]];
    }
}

@end
