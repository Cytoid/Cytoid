//
//  LUConsoleController.m
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


#import "LUConsoleController.h"

#import "Lunar.h"

static NSString *const kStateFilename = @"com.spacemadness.lunarmobileconsole.state.bin";

NSString *const LUConsoleControllerDidResizeNotification = @"LUConsoleControllerDidResizeNotification";

@interface LUConsoleController () <LUConsoleLogControllerResizeDelegate, LUConsoleResizeControllerDelegate>

@property (nonatomic, weak) IBOutlet UIScrollView *scrollView; // we need to be able to use "paging" scrolling between differenct controllers

@property (nonatomic, weak) IBOutlet UIView *contentView; // the container view for controllers and "button" bar at the bottom

// we need to keep constraints in order to properly resize content view with auto layout (can't just set frames)
@property (weak, nonatomic) IBOutlet NSLayoutConstraint *contentTrailingConstraint;
@property (weak, nonatomic) IBOutlet NSLayoutConstraint *contentBottomConstraint;
@property (weak, nonatomic) IBOutlet NSLayoutConstraint *contentLeadingConstraint;
@property (weak, nonatomic) IBOutlet NSLayoutConstraint *contentTopConstraint;

@property (nonatomic, weak) LUConsolePlugin *plugin;
@property (nonatomic, strong) NSArray<UIViewController *> *pageControllers;
@property (nonatomic, strong) LUConsoleControllerState *state;

@end

@implementation LUConsoleController

+ (instancetype)controllerWithPlugin:(LUConsolePlugin *)plugin
{
    return [[self alloc] initWithPlugin:plugin];
}

- (instancetype)initWithPlugin:(LUConsolePlugin *)plugin
{
    self = [super initWithNibName:NSStringFromClass([self class]) bundle:nil];
    if (self) {
        _plugin = plugin;
        _state = [LUConsoleControllerState loadFromFile:kStateFilename];
    }
    return self;
}

- (void)dealloc
{
    [self unregisterNotifications];
}

- (void)viewDidLoad
{
    [super viewDidLoad];

    // background
    self.view.opaque = YES;
    self.view.backgroundColor = [UIColor clearColor];

    // controllers
    LUConsoleLogController *logController = [LUConsoleLogController controllerWithPlugin:_plugin];
    logController.version = _plugin.version;
    logController.emails = _emails;
    logController.resizeDelegate = self;

    LUActionController *actionController = [LUActionController controllerWithActionRegistry:_plugin.actionRegistry];

    _pageControllers = @[ logController, actionController ];
    [self setPageControllers:_pageControllers];

    // notify delegate
    if ([_delegate respondsToSelector:@selector(consoleControllerDidOpen:)]) {
        [_delegate consoleControllerDidOpen:self];
    }

    self.contentView.translatesAutoresizingMaskIntoConstraints = NO;
    self.scrollView.translatesAutoresizingMaskIntoConstraints = NO;
	
	[self setControllerInsets:_state.controllerInsets];

    [self registerNotifications];
}

- (void)viewDidLayoutSubviews
{
    [super viewDidLayoutSubviews];

    // set paging
    CGSize pageSize = _scrollView.bounds.size;
    CGSize contentSize = CGSizeMake(_pageControllers.count * pageSize.width, pageSize.height);
    _scrollView.contentSize = contentSize;
}

- (void)setPageControllers:(NSArray<UIViewController *> *)controllers
{
    NSMutableArray *constraints = [NSMutableArray new];

    for (NSUInteger idx = 0; idx < controllers.count; ++idx) {
        UIViewController *controller = controllers[idx];
        UIViewController *prevController = idx > 0 ? controllers[idx - 1] : nil;
        UIViewController *nextController = idx < controllers.count - 1 ? controllers[idx + 1] : nil;

        controller.view.translatesAutoresizingMaskIntoConstraints = NO;

        // add child controller
        [self addChildViewController:controller];

        // add view
        [_scrollView addSubview:controller.view];

        // call notification
        [controller didMoveToParentViewController:controller];

        // width
        [constraints addObject:[NSLayoutConstraint constraintWithItem:controller.view
                                                            attribute:NSLayoutAttributeWidth
                                                            relatedBy:NSLayoutRelationEqual
                                                               toItem:_scrollView
                                                            attribute:NSLayoutAttributeWidth
                                                           multiplier:1.0
                                                             constant:0]];

        // height
        [constraints addObject:[NSLayoutConstraint constraintWithItem:controller.view
                                                            attribute:NSLayoutAttributeHeight
                                                            relatedBy:NSLayoutRelationEqual
                                                               toItem:_scrollView
                                                            attribute:NSLayoutAttributeHeight
                                                           multiplier:1.0
                                                             constant:0]];

        // vertical center
        [constraints addObject:[NSLayoutConstraint constraintWithItem:controller.view
                                                            attribute:NSLayoutAttributeCenterY
                                                            relatedBy:NSLayoutRelationEqual
                                                               toItem:_scrollView
                                                            attribute:NSLayoutAttributeCenterY
                                                           multiplier:1.0
                                                             constant:0]];

        // left
        if (prevController) {
            [constraints addObject:[NSLayoutConstraint constraintWithItem:controller.view
                                                                attribute:NSLayoutAttributeLeft
                                                                relatedBy:NSLayoutRelationEqual
                                                                   toItem:prevController.view
                                                                attribute:NSLayoutAttributeRight
                                                               multiplier:1.0
                                                                 constant:0]];
        } else {
            [constraints addObject:[NSLayoutConstraint constraintWithItem:controller.view
                                                                attribute:NSLayoutAttributeLeft
                                                                relatedBy:NSLayoutRelationEqual
                                                                   toItem:_scrollView
                                                                attribute:NSLayoutAttributeLeft
                                                               multiplier:1.0
                                                                 constant:0]];
        }

        // right
        if (nextController) {
            [constraints addObject:[NSLayoutConstraint constraintWithItem:controller.view
                                                                attribute:NSLayoutAttributeRight
                                                                relatedBy:NSLayoutRelationEqual
                                                                   toItem:nextController.view
                                                                attribute:NSLayoutAttributeLeft
                                                               multiplier:1.0
                                                                 constant:0]];
        } else {
            [constraints addObject:[NSLayoutConstraint constraintWithItem:controller.view
                                                                attribute:NSLayoutAttributeRight
                                                                relatedBy:NSLayoutRelationEqual
                                                                   toItem:_scrollView
                                                                attribute:NSLayoutAttributeRight
                                                               multiplier:1.0
                                                                 constant:0]];
        }
    }

    [NSLayoutConstraint activateConstraints:constraints];
}

#pragma mark -
#pragma mark Notifications

- (void)registerNotifications
{
    [LUNotificationCenter addObserver:self
                             selector:@selector(consolePopupControllerWillAppearNotification:)
                                 name:LUConsolePopupControllerWillAppearNotification
                               object:nil];

    [LUNotificationCenter addObserver:self
                             selector:@selector(consolePopupControllerWillDisappearNotification:)
                                 name:LUConsolePopupControllerWillDisappearNotification
                               object:nil];
}

- (void)unregisterNotifications
{
    [LUNotificationCenter removeObserver:self];
}

- (void)consolePopupControllerWillAppearNotification:(NSNotification *)notification
{
    self.scrollEnabled = NO;
}

- (void)consolePopupControllerWillDisappearNotification:(NSNotification *)notification
{
    self.scrollEnabled = YES;
}

#pragma mark -
#pragma mark Helpers

- (void)setContentHidden:(BOOL)hidden
{
    self.contentView.hidden = hidden;
}

- (void)setControllerInsets:(UIEdgeInsets)insets
{
	self.contentTopConstraint.constant = insets.top;
	self.contentBottomConstraint.constant = insets.bottom;
	self.contentLeadingConstraint.constant = insets.left;
	self.contentTrailingConstraint.constant = insets.right;
}

#pragma mark -
#pragma mark Actions

- (IBAction)onClose:(id)sender
{
    if ([_delegate respondsToSelector:@selector(consoleControllerDidClose:)]) {
        [_delegate consoleControllerDidClose:self];
    }
}

#pragma mark -
#pragma mark LUConsoleLogControllerResizeDelegate

- (void)consoleLogControllerDidRequestResize:(LUConsoleLogController *)controller
{
    [self setContentHidden:YES];

	CGSize maxSize = [LUUIHelper safeAreaRect].size;
	LUConsoleResizeController *resizeController = [[LUConsoleResizeController alloc] initWithMaxSize:maxSize
																					   topConstraint:self.contentTopConstraint
																				   leadingConstraint:self.contentLeadingConstraint
																					bottomConstraint:self.contentBottomConstraint
																				  trailingConstraint:self.contentTrailingConstraint];
    resizeController.delegate = self;
    [self addChildController:resizeController withFrame:self.contentView.frame];
	
	[LUUIHelper view:resizeController.view centerInParent:self.contentView];
}

#pragma mark -
#pragma mark LUConsoleResizeControllerDelegate

- (void)consoleResizeControllerDidClose:(LUConsoleResizeController *)controller
{
    [self removeChildController:controller];
    [self setContentHidden:NO];

    _state.controllerInsets = UIEdgeInsetsMake(
        self.contentTopConstraint.constant,
        self.contentLeadingConstraint.constant,
        self.contentBottomConstraint.constant,
        self.contentTrailingConstraint.constant
     );

    // post notification
    [LUNotificationCenter postNotificationName:LUConsoleControllerDidResizeNotification object:nil];
}

#pragma mark -
#pragma mark Properties

- (BOOL)scrollEnabled
{
    return _scrollView.scrollEnabled;
}

- (void)setScrollEnabled:(BOOL)scrollEnabled
{
    _scrollView.scrollEnabled = scrollEnabled;
}

@end

@implementation LUConsoleControllerState

#pragma mark -
#pragma mark Loading

+ (void)initialize
{
    if ([self class] == [LUConsoleControllerState class]) {
        [self setVersion:1];
        
        if (LU_IOS_MIN_VERSION_AVAILABLE)
        {
            // force linker to add these classes for Interface Builder
            [LUButton class];
            [LUConsoleLogTypeButton class];
            [LUSlider class];
            [LUSwitch class];
            [LUTableView class];
            [LUPassTouchView class];
            [LUTextField class];
        }
    }
}

#pragma mark -
#pragma mark Inheritance

- (void)initDefaults
{
    if (UI_USER_INTERFACE_IDIOM() == UIUserInterfaceIdiomPad) {
        CGSize screenSize = LUGetScreenBounds().size;
		_controllerInsets = UIEdgeInsetsMake(0, 0, 0.5 * screenSize.height, 0);
	} else {
		_controllerInsets = UIEdgeInsetsZero;
	}
}

- (void)serializeWithCoder:(NSCoder *)coder
{
    [coder encodeUIEdgeInsets:_controllerInsets forKey:@"controllerInsets"];
}

- (void)deserializeWithDecoder:(NSCoder *)decoder
{
    _controllerInsets = [decoder decodeUIEdgeInsetsForKey:@"controllerInsets"];
}

#pragma mark -
#pragma mark Properties

- (void)setControllerInsets:(UIEdgeInsets)controllerInsets
{
	_controllerInsets = controllerInsets;
	[self save];
}

@end
