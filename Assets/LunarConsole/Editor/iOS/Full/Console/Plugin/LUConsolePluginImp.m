//
//  LUConsolePluginImp.m
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


#import "LUConsolePluginImp.h"
#import "Lunar-Full.h"

BOOL LUConsoleIsFreeVersion = NO;
BOOL LUConsoleIsFullVersion = YES;

static NSString *const kScriptMessageSetVariable = @"console_variable_set";
static NSString *const kScriptMessageAction = @"console_action";

@interface LUConsolePluginImp () {
    __weak LUConsolePlugin *_plugin;
    LUWindow *_overlayWindow;
}

@end

@implementation LUConsolePluginImp

- (instancetype)initWithPlugin:(LUConsolePlugin *)plugin
{
    self = [super init];
    if (self) {
        _plugin = plugin;
        [self registerNotifications];
    }
    return self;
}

#pragma mark -
#pragma mark Notifications

- (void)registerNotifications
{
    [self registerNotificationName:LUActionControllerDidChangeVariable
                          selector:@selector(actionControllerDidChangeVariableNotification:)];

    [self registerNotificationName:LUActionControllerDidSelectAction
                          selector:@selector(actionControllerDidSelectActionNotification:)];
}

- (void)actionControllerDidChangeVariableNotification:(NSNotification *)notification
{
    LUCVar *variable = [notification.userInfo objectForKey:LUActionControllerDidChangeVariableKeyVariable];
    LUAssert(variable);

    if (variable) {
        NSDictionary *params = @{
            @"id" : [NSNumber numberWithInt:variable.actionId],
            @"value" : variable.value
        };
        [_plugin sendScriptMessageName:kScriptMessageSetVariable params:params];
    }
}

- (void)actionControllerDidSelectActionNotification:(NSNotification *)notification
{
    LUAction *action = [notification.userInfo objectForKey:LUActionControllerDidSelectActionKeyAction];
    LUAssert(action);

    if (action) {
        NSDictionary *params = @{ @"id" : [NSNumber numberWithInt:action.actionId] };
        [_plugin sendScriptMessageName:kScriptMessageAction params:params];
    }
}

- (void)showOverlay
{
    if (_overlayWindow == nil) {
        LUConsoleOverlayController *controller = [LUConsoleOverlayController controllerWithConsole:_plugin.console
                                                                                          settings:_plugin.settings.logOverlay];

		CGRect windowFrame = [LUUIHelper safeAreaRect];
        _overlayWindow = [[LUWindow alloc] initWithFrame:windowFrame];
        _overlayWindow.userInteractionEnabled = NO;
        _overlayWindow.rootViewController = controller;
        _overlayWindow.opaque = YES;
        _overlayWindow.hidden = NO;
    }
}

- (void)hideOverlay
{
    if (_overlayWindow != nil) {
        _overlayWindow.rootViewController = nil;
        _overlayWindow.hidden = YES;
        _overlayWindow = nil;
    }
}

@end
