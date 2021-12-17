//
//  LUConsolePopupController.m
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


#import "LUConsolePopupController.h"
#import "Lunar.h"

NSString * const LUConsolePopupControllerWillAppearNotification = @"LUConsolePopupControllerWillAppearNotification";
NSString * const LUConsolePopupControllerWillDisappearNotification = @"LUConsolePopupControllerWillDisappearNotification";

@interface LUViewController (PopupController)

- (void)setPopupController:(LUConsolePopupController *)controller;

@end

@interface LUConsolePopupButton ()

@property (nonatomic, strong) UIImage *icon;

@property (nonatomic, weak) id target;
@property (nonatomic, assign) SEL action;

@end

@implementation LUConsolePopupButton

+ (instancetype)buttonWithIcon:(UIImage *)icon target:(id)target action:(SEL)action
{
    return [[self alloc] initWithIcon:icon target:target action:action];
}

- (instancetype)initWithIcon:(UIImage *)icon target:(id)target action:(SEL)action
{
    self = [super init];
    if (self)
    {
        self.icon = icon;
        self.target = target;
        self.action = action;
    }
    return self;
}

@end

@interface LUConsolePopupController ()

@property (nonatomic, weak) IBOutlet UIImageView *iconImageView;
@property (nonatomic, weak) IBOutlet UILabel *titleLabel;
@property (nonatomic, weak) IBOutlet UIView *popupView;
@property (nonatomic, weak) IBOutlet UIView *contentView;
@property (nonatomic, weak) IBOutlet UIView *bottomBarView;
@property (nonatomic, weak) IBOutlet UIButton *closeButton;
@property (nonatomic, weak) IBOutlet UIButton *learnMoreButton;

@property (nonatomic, weak) IBOutlet NSLayoutConstraint *contentWidthConstraint;
@property (nonatomic, weak) IBOutlet NSLayoutConstraint *contentHeightConstraint;

@end

@implementation LUConsolePopupController

- (instancetype)initWithContentController:(LUViewController *)contentController
{
    self = [super initWithNibName:NSStringFromClass([self class]) bundle:nil];
    if (self)
    {
        _contentController = contentController;
        [_contentController setPopupController:self];
    }
    return self;
}

- (void)viewDidLoad
{
    [super viewDidLoad];
    
    // colors
    self.view.backgroundColor = [UIColor colorWithRed:0 green:0 blue:0 alpha:0.5];
    
    LUTheme *theme = [LUTheme mainTheme];
    
    _popupView.backgroundColor = theme.tableColor;
    _contentView.backgroundColor = theme.tableColor;
    _bottomBarView.backgroundColor = theme.tableColor;
    
    _contentView.translatesAutoresizingMaskIntoConstraints = NO;
    _bottomBarView.translatesAutoresizingMaskIntoConstraints = NO;
    
    _popupView.layer.borderColor = [[UIColor colorWithRed:0.37 green:0.37 blue:0.37 alpha:1.0] CGColor];
    _popupView.layer.borderWidth = 2;
    
    _titleLabel.textColor = theme.cellLog.textColor;
    
    // content controller
    [self addContentController:_contentController];
    
    // popup buttons
    [self addPopupButtons];
    
    // "Learn more..." button
    _learnMoreButton.hidden = YES;
	
	LU_SET_ACCESSIBILITY_IDENTIFIER(_closeButton, @"Popup Controller Close Button");
}

- (void)viewWillAppear:(BOOL)animated
{
    [super viewWillAppear:animated];
    
    [LUNotificationCenter postNotificationName:LUConsolePopupControllerWillAppearNotification object:nil];
}

- (void)viewWillDisappear:(BOOL)animated
{
    [super viewWillDisappear:animated];
    
    [LUNotificationCenter postNotificationName:LUConsolePopupControllerWillDisappearNotification object:nil];
}

- (void)viewDidLayoutSubviews
{
    [super viewDidLayoutSubviews];
    
    CGSize fullSize = self.view.bounds.size;
    CGFloat contentWidth, contentHeight;
    
    if (fullSize.width < fullSize.height)
    {
        contentWidth = MAX(320, 2 * fullSize.width / 3) - 2 * 20;
        contentHeight = 1.5 * contentWidth;
    }
    else
    {
        contentHeight = MAX(320, 2 * fullSize.height / 3) - 2 * 20;
        contentWidth = 1.5 * contentHeight;
    }
    
    CGSize preferredSize = [_contentController preferredPopupSize];
    if (preferredSize.width > 0) contentWidth = preferredSize.width;
    if (preferredSize.height > 0) contentHeight = preferredSize.height;
    
    self.contentWidthConstraint.constant = contentWidth;
    self.contentHeightConstraint.constant = contentHeight;
}

#pragma mark -
#pragma mark Popup buttons

- (void)addPopupButtons
{
    if (_contentController.popupButtons.count > 0)
    {
        NSMutableArray *constraints = [NSMutableArray new];
        UIButton *prevButton = nil;
        for (LUConsolePopupButton *pb in _contentController.popupButtons)
        {
            UIButton *button = [UIButton buttonWithType:UIButtonTypeCustom];
            [button addTarget:pb.target action:pb.action forControlEvents:UIControlEventTouchUpInside];
            [button setImage:pb.icon forState:UIControlStateNormal];
            button.translatesAutoresizingMaskIntoConstraints = NO;
            [_bottomBarView addSubview:button];
            
            [constraints addObject:[NSLayoutConstraint constraintWithItem:button
                                                                attribute:NSLayoutAttributeWidth
                                                                relatedBy:NSLayoutRelationEqual
                                                                   toItem:_closeButton
                                                                attribute:NSLayoutAttributeWidth
                                                               multiplier:1.0
                                                                 constant:0]];
            
            [constraints addObject:[NSLayoutConstraint constraintWithItem:button
                                                                attribute:NSLayoutAttributeHeight
                                                                relatedBy:NSLayoutRelationEqual
                                                                   toItem:_closeButton
                                                                attribute:NSLayoutAttributeHeight
                                                               multiplier:1.0
                                                                 constant:0]];
            
            [constraints addObject:[NSLayoutConstraint constraintWithItem:button
                                                                attribute:NSLayoutAttributeCenterY
                                                                relatedBy:NSLayoutRelationEqual
                                                                   toItem:_closeButton
                                                                attribute:NSLayoutAttributeCenterY
                                                               multiplier:1.0
                                                                 constant:0]];
            
            if (prevButton)
            {
                [constraints addObject:[NSLayoutConstraint constraintWithItem:button
                                                                    attribute:NSLayoutAttributeLeft
                                                                    relatedBy:NSLayoutRelationEqual
                                                                       toItem:prevButton
                                                                    attribute:NSLayoutAttributeRight
                                                                   multiplier:1.0
                                                                     constant:0]];
            }
            else
            {
                [constraints addObject:[NSLayoutConstraint constraintWithItem:button
                                                                    attribute:NSLayoutAttributeLeft
                                                                    relatedBy:NSLayoutRelationEqual
                                                                       toItem:_bottomBarView
                                                                    attribute:NSLayoutAttributeLeft
                                                                   multiplier:1.0
                                                                     constant:0]];

            }
            
            prevButton = button;
        }
        
        [NSLayoutConstraint activateConstraints:constraints];
    }
}

#pragma mark -
#pragma mark Content controller

- (void)addContentController:(LUViewController *)contentController
{
    [self addChildViewController:contentController];
    [_contentView addSubview:contentController.view];
    contentController.view.frame = _contentView.bounds;
    [contentController didMoveToParentViewController:self];
    
    // title and icon
    _titleLabel.text = contentController.popupTitle;
    _iconImageView.image = contentController.popupIcon;
    
    // setting up layout constraints
    contentController.view.translatesAutoresizingMaskIntoConstraints = NO;
    NSArray *constraints = @[
        [NSLayoutConstraint constraintWithItem:contentController.view
                                     attribute:NSLayoutAttributeWidth
                                     relatedBy:NSLayoutRelationEqual
                                        toItem:_contentView
                                     attribute:NSLayoutAttributeWidth
                                    multiplier:1.0
                                      constant:0.0],
        [NSLayoutConstraint constraintWithItem:contentController.view
                                     attribute:NSLayoutAttributeHeight
                                     relatedBy:NSLayoutRelationEqual
                                        toItem:_contentView
                                     attribute:NSLayoutAttributeHeight
                                    multiplier:1.0
                                      constant:0.0],
        [NSLayoutConstraint constraintWithItem:contentController.view
                                     attribute:NSLayoutAttributeCenterX
                                     relatedBy:NSLayoutRelationEqual
                                        toItem:_contentView
                                     attribute:NSLayoutAttributeCenterX
                                    multiplier:1.0
                                      constant:0.0],
        [NSLayoutConstraint constraintWithItem:contentController.view
                                     attribute:NSLayoutAttributeCenterY
                                     relatedBy:NSLayoutRelationEqual
                                        toItem:_contentView
                                     attribute:NSLayoutAttributeCenterY
                                    multiplier:1.0
                                      constant:0.0]
     
    ];
    
    [NSLayoutConstraint activateConstraints:constraints];
}

#pragma mark -
#pragma mark Actions

- (IBAction)onClose:(id)sender
{
    if ([_popupDelegate respondsToSelector:@selector(popupControllerDidDismiss:)])
    {
        [_popupDelegate popupControllerDidDismiss:self];
    }
}

#pragma mark -
#pragma mark Presentation

- (void)presentFromController:(UIViewController *)controller animated:(BOOL)animated
{
    // add as child view controller
    [controller addChildViewController:self];
    self.view.frame = controller.view.bounds;
    [controller.view addSubview:self.view];
    [self didMoveToParentViewController:controller];
    
    // animate
    if (animated)
    {
        self.view.alpha = 0;
        [UIView animateWithDuration:0.4 animations:^{
            self.view.alpha = 1;
        }];
    }
}

- (void)dismissAnimated:(BOOL)animated
{
    if (animated)
    {
        [UIView animateWithDuration:0.4 animations:^{
            self.view.alpha = 0;
        } completion:^(BOOL finished) {
            [self willMoveToParentViewController:nil];
            [self.view removeFromSuperview];
            [self removeFromParentViewController];
        }];
    }
    else
    {
        [self willMoveToParentViewController:nil];
        [self.view removeFromSuperview];
        [self removeFromParentViewController];
    }
}

- (void)setLearnMoreTitle:(NSString *)title target:(id)target action:(SEL)action
{
    // FIXME: store button details and set params in viewDidLoad
    dispatch_async(dispatch_get_main_queue(), ^{
        self->_learnMoreButton.hidden = NO;
        [self->_learnMoreButton setTitle:title forState:UIControlStateNormal];
        [self->_learnMoreButton addTarget:target
                                   action:action
                         forControlEvents:UIControlEventTouchUpInside];
    });
}

@end
