//
//  LUUIHelper.m
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


#import "LUUIHelper.h"
#import "LULittleHelper.h"
#import "LUAvailability.h"

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"

@interface LUAlertView : UIAlertView

@property (nonatomic, strong) NSArray<LUAlertAction *> *actions;

-(instancetype)initWithTitle:(NSString *)title message:(NSString *)message actions:(NSArray<LUAlertAction *> *)actions;

@end

@implementation LUAlertView

- (instancetype)initWithTitle:(NSString *)title message:(NSString *)message actions:(NSArray<LUAlertAction *> *)actions
{
	self = [super initWithTitle:title message:message delegate:self cancelButtonTitle:nil otherButtonTitles:nil];
	if (self) {
		_actions = actions;
		for (LUAlertAction *action in actions) {
			[self addButtonWithTitle:action.title];
		}
	}
	return self;
}

- (void)alertView:(UIAlertView *)alertView clickedButtonAtIndex:(NSInteger)buttonIndex
{
	if (_actions[buttonIndex].handler) {
		_actions[buttonIndex].handler(_actions[buttonIndex]);
	}
}

@end

#pragma clang diagnostic pop

@implementation LUAlertAction

- (instancetype)initWithTitle:(NSString *)title handler:(void (^)(LUAlertAction * _Nonnull))handler
{
	self = [super init];
	if (self) {
		_title = title;
		_handler = handler;
	}
	return self;
}

@end

@implementation LUUIHelper

+ (void)showAlertViewWithTitle:(NSString *)title message:(NSString *)message
{
	NSArray *actions = @[[[LUAlertAction alloc] initWithTitle:title handler:nil]];
	[self showAlertViewWithTitle:title message:message actions:actions];
}

+ (void)showAlertViewWithTitle:(NSString *)title message:(NSString *)message actions:(NSArray<LUAlertAction *> *)actions
{
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
	LUAlertView *alertView = [[LUAlertView alloc] initWithTitle:title
														message:message
														actions:actions];
	[alertView show];
#pragma clang diagnostic pop
}

+ (void)view:(UIView *)view centerInParent:(UIView *)parent
{
	view.translatesAutoresizingMaskIntoConstraints = NO;
	NSArray *constraints = @[
		[NSLayoutConstraint constraintWithItem:view
									 attribute:NSLayoutAttributeLeading
									 relatedBy:NSLayoutRelationEqual
										toItem:parent
									 attribute:NSLayoutAttributeLeading
									multiplier:1.0
									  constant:0],
		[NSLayoutConstraint constraintWithItem:view
									 attribute:NSLayoutAttributeTrailing
									 relatedBy:NSLayoutRelationEqual
										toItem:parent
									 attribute:NSLayoutAttributeTrailing
									multiplier:1.0
									  constant:0],
        [NSLayoutConstraint constraintWithItem:view
									 attribute:NSLayoutAttributeTop
									 relatedBy:NSLayoutRelationEqual
										toItem:parent
									 attribute:NSLayoutAttributeTop
									multiplier:1.0
									  constant:0],
		[NSLayoutConstraint constraintWithItem:view
									 attribute:NSLayoutAttributeBottom
									 relatedBy:NSLayoutRelationEqual
										toItem:parent
									 attribute:NSLayoutAttributeBottom
									multiplier:1.0
									  constant:0]
	];
	[NSLayoutConstraint activateConstraints:constraints];
}

+ (CGRect)safeAreaRect
{
	if (@available(iOS 11.0, *)) {
		return [UIApplication sharedApplication].keyWindow.safeAreaLayoutGuide.layoutFrame;
	}
	
	return [UIScreen mainScreen].bounds;
}

@end
