//
//  LUConsoleLogMenuController.m
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

#import "LUConsoleLogMenuController.h"

#define kButtonBackgroundImage @"lunar_console_log_button_background.png"

@interface LUConsoleLogMenuControllerButton () {
    NSString *_title;
    id _target;
    SEL _action;
}

- (UIButton *)UIButton;

@end

@implementation LUConsoleLogMenuControllerButton

+ (instancetype)buttonWithTitle:(NSString *)title target:(id)target action:(SEL)action
{
    return [[self alloc] initWithTitle:title target:target action:action];
}

- (instancetype)initWithTitle:(NSString *)title target:(id)target action:(SEL)action
{
    self = [super init];
    if (self) {
        _title = title;
        _target = target;
        _action = action;
    }
    return self;
}


- (UIButton *)UIButton
{
    LUTheme *theme = [LUTheme mainTheme];
    UIColor *textColor = _textColor ? _textColor : theme.contextMenuTextColor;
    UIColor *textHighlightedColor = _textHighlightedColor ? _textHighlightedColor : theme.contextMenuTextHighlightColor;

    UIButton *button = [UIButton buttonWithType:UIButtonTypeCustom];
    [button setTitle:_title forState:UIControlStateNormal];
    [button addTarget:_target action:_action forControlEvents:UIControlEventTouchUpInside];
    [button setBackgroundColor:theme.contextMenuBackgroundColor];
    [button setTitleColor:textColor forState:UIControlStateNormal];
    [button setTitleColor:textHighlightedColor forState:UIControlStateHighlighted];
    [button setTranslatesAutoresizingMaskIntoConstraints:NO];
    [button setContentHorizontalAlignment:UIControlContentHorizontalAlignmentLeft];
    [button setContentEdgeInsets:UIEdgeInsetsMake(0, 15, 0, 0)];
    return button;
}

@end

@interface LUConsoleLogMenuController () {
    NSMutableArray *_buttons;
}

@property (nonatomic, weak) IBOutlet UIView *contentView;

@end

@implementation LUConsoleLogMenuController

- (instancetype)init
{
    self = [super initWithNibName:NSStringFromClass([self class]) bundle:nil];
    if (self) {
        _buttons = [NSMutableArray new];
    }
    return self;
}


- (void)viewDidLoad
{
    [super viewDidLoad];

    // background tap
    UITapGestureRecognizer *recognizer = [[UITapGestureRecognizer alloc] initWithTarget:self
                                                                                 action:@selector(handleBackgroundTap:)];
    [self.view addGestureRecognizer:recognizer];

    // colors
    LUTheme *theme = [LUTheme mainTheme];

    // background
    _contentView.backgroundColor = theme.contextMenuBackgroundColor;

    // border radius
    _contentView.layer.borderColor = [[UIColor colorWithRed:0.37 green:0.37 blue:0.37 alpha:1.0] CGColor];
    _contentView.layer.cornerRadius = 3.0;

    // shadow
    _contentView.layer.shadowColor = [UIColor blackColor].CGColor;
    _contentView.layer.shadowOpacity = 0.5;
    _contentView.layer.shadowRadius = 5.0;


    // add buttons
    [self addButtonsToView:_contentView];

    // update constraints
    [self updateViewConstraints];
}

#pragma mark -
#pragma mark Buttons

- (LUConsoleLogMenuControllerButton *)addButtonTitle:(NSString *)title target:(id)target action:(SEL)action
{
    LUConsoleLogMenuControllerButton *button = [[LUConsoleLogMenuControllerButton alloc] initWithTitle:title target:target action:action];
    [_buttons addObject:button];
    return button;
}

- (void)addButtonsToView:(UIView *)view
{
    UIButton *prevButton = nil;

    for (LUConsoleLogMenuControllerButton *buttonData in _buttons) {
        UIButton *button = [buttonData UIButton];

        // close controller on button
        [button addTarget:self action:@selector(onButton:) forControlEvents:UIControlEventTouchUpInside];

        // add to parent
        [view addSubview:button];

        // content hugging priority so button won't expand
        [button setContentHuggingPriority:UILayoutPriorityRequired forAxis:UILayoutConstraintAxisVertical];

        // set horizontal center
        [NSLayoutConstraint constraintWithItem:button
                                     attribute:NSLayoutAttributeCenterX
                                     relatedBy:NSLayoutRelationEqual
                                        toItem:view
                                     attribute:NSLayoutAttributeCenterX
                                    multiplier:1.0
                                      constant:0]
            .active = YES;

        // set equal width with a parent
        [NSLayoutConstraint constraintWithItem:button
                                     attribute:NSLayoutAttributeWidth
                                     relatedBy:NSLayoutRelationEqual
                                        toItem:view
                                     attribute:NSLayoutAttributeWidth
                                    multiplier:1.0
                                      constant:0]
            .active = YES;

        // set top margin
        if (prevButton) {
            [NSLayoutConstraint constraintWithItem:button
                                         attribute:NSLayoutAttributeTop
                                         relatedBy:NSLayoutRelationEqual
                                            toItem:prevButton
                                         attribute:NSLayoutAttributeBottom
                                        multiplier:1.0
                                          constant:8]
                .active = YES;
        } else {
            [NSLayoutConstraint constraintWithItem:button
                                         attribute:NSLayoutAttributeTop
                                         relatedBy:NSLayoutRelationEqual
                                            toItem:view
                                         attribute:NSLayoutAttributeTopMargin
                                        multiplier:1.0
                                          constant:0]
                .active = YES;
        }

        prevButton = button;
    }

    // set top margin
    [NSLayoutConstraint constraintWithItem:prevButton
                                 attribute:NSLayoutAttributeBottom
                                 relatedBy:NSLayoutRelationEqual
                                    toItem:view
                                 attribute:NSLayoutAttributeBottomMargin
                                multiplier:1.0
                                  constant:0]
        .active = YES;
}

- (void)onButton:(id)sender
{
    [self requestClose];
}

#pragma mark -
#pragma mark Background tap recognizer

- (void)handleBackgroundTap:(UIGestureRecognizer *)recognizer
{
    [self requestClose];
}

#pragma mark -
#pragma mark Delegate notifications

- (void)requestClose
{
    if ([_delegate respondsToSelector:@selector(menuControllerDidRequestClose:)]) {
        [_delegate menuControllerDidRequestClose:self];
    }
}

@end
