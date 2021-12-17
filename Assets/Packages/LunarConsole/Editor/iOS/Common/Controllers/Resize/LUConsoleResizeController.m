//
//  LUConsoleResizeController.m
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


#import "LUConsoleResizeController.h"

#import "Lunar.h"

static const CGFloat kMinWidth = 320;
static const CGFloat kMinHeight = 320;

inline static CGFloat clampf(CGFloat value, CGFloat min, CGFloat max)
{
	return value < min ? min : (value > max ? max : value);
}

typedef enum : NSUInteger {
    LUConsoleResizeOperationNone        = 0,
    LUConsoleResizeOperationMove        = 1 << 1,
    LUConsoleResizeOperationTop         = 1 << 2,
    LUConsoleResizeOperationBottom      = 1 << 3,
    LUConsoleResizeOperationLeft        = 1 << 4,
    LUConsoleResizeOperationRight       = 1 << 5,
    LUConsoleResizeOperationTopLeft     = LUConsoleResizeOperationTop | LUConsoleResizeOperationLeft,
    LUConsoleResizeOperationTopRight    = LUConsoleResizeOperationTop | LUConsoleResizeOperationRight,
    LUConsoleResizeOperationBottomLeft  = LUConsoleResizeOperationBottom | LUConsoleResizeOperationLeft,
    LUConsoleResizeOperationBottomRight = LUConsoleResizeOperationBottom | LUConsoleResizeOperationRight
} LUConsoleResizeOperation;

@interface LUConsoleResizeController ()
{
    UITouch * _initialTouch;
    LUConsoleResizeOperation _resizeOperation;
}

@property (weak, nonatomic) IBOutlet UILabel *hintLabel;
@property (weak, nonatomic) IBOutlet UIView *resizeTopBar;
@property (weak, nonatomic) IBOutlet UIView *resizeBottomBar;
@property (weak, nonatomic) IBOutlet UIView *resizeLeftBar;
@property (weak, nonatomic) IBOutlet UIView *resizeRightBar;
@property (weak, nonatomic) IBOutlet UIView *resizeTopLeftBar;
@property (weak, nonatomic) IBOutlet UIView *resizeTopRightBar;
@property (weak, nonatomic) IBOutlet UIView *resizeBottomLeftBar;
@property (weak, nonatomic) IBOutlet UIView *resizeBottomRightBar;

@property (nonatomic, assign) CGSize maxSize;
@property (weak, nonatomic) NSLayoutConstraint *topConstraint;
@property (weak, nonatomic) NSLayoutConstraint *leadingConstraint;
@property (weak, nonatomic) NSLayoutConstraint *bottomConstraint;
@property (weak, nonatomic) NSLayoutConstraint *trailingConstraint;

@end

@implementation LUConsoleResizeController

- (instancetype)initWithMaxSize:(CGSize)maxSize topConstraint:(NSLayoutConstraint *)topConstraint leadingConstraint:(NSLayoutConstraint *)leadingConstraint bottomConstraint:(NSLayoutConstraint *)bottomConstraint trailingConstraint:(NSLayoutConstraint *)trailingConstraint {
    self = [super initWithNibName:NSStringFromClass([self class]) bundle:nil];
    if (self)
    {
		_maxSize = maxSize;
		_topConstraint = topConstraint;
		_leadingConstraint = leadingConstraint;
		_bottomConstraint = bottomConstraint;
		_trailingConstraint = trailingConstraint;
    }
    return self;
}

- (void)viewDidLoad
{
    [super viewDidLoad];
    
    LUTheme *theme = [LUTheme mainTheme];
    
    self.view.backgroundColor =
    _resizeTopBar.backgroundColor =
    _resizeBottomBar.backgroundColor =
    _resizeLeftBar.backgroundColor =
    _resizeRightBar.backgroundColor =
    _resizeTopLeftBar.backgroundColor =
    _resizeTopRightBar.backgroundColor =
    _resizeBottomLeftBar.backgroundColor =
    _resizeBottomRightBar.backgroundColor = theme.tableColor;
    
    _hintLabel.font = theme.actionsWarningFont; // TODO: make a separate entry
    _hintLabel.textColor = theme.actionsTextColor; // TODO: make a separate entry
}

#pragma mark -
#pragma mark Touch handling

- (void)touchesBegan:(NSSet *)touches withEvent:(UIEvent *)event
{
    if (_initialTouch == nil)
    {
        _initialTouch = [touches anyObject];
        _resizeOperation = [self lookupResizeOperationForView:_initialTouch.view];
    }
}

- (void)touchesMoved:(NSSet *)touches withEvent:(UIEvent *)event
{
	if (_initialTouch == nil)
	{
		return;
	}
	
	UIEdgeInsets insets = UIEdgeInsetsMake(
        self.topConstraint.constant,
        self.leadingConstraint.constant,
        self.bottomConstraint.constant,
        self.trailingConstraint.constant
    );
	
	CGPoint touchPoint = [[touches anyObject] locationInView:self.view];
	CGPoint previous = [[touches anyObject] previousLocationInView:self.view];
	
	CGFloat deltaX = touchPoint.x - previous.x;
	CGFloat deltaY = touchPoint.y - previous.y;

	if (_resizeOperation & LUConsoleResizeOperationTop)
    {
		insets.top = clampf(insets.top + deltaY, 0, _maxSize.height - (kMinHeight + insets.bottom));
    }
    else if (_resizeOperation & LUConsoleResizeOperationBottom)
    {
		insets.bottom = clampf(insets.bottom - deltaY, 0, _maxSize.height - (kMinHeight + insets.top));
    }

    if (_resizeOperation & LUConsoleResizeOperationLeft)
    {
		insets.left = clampf(insets.left + deltaX, 0, _maxSize.width - (kMinWidth + insets.right));
    }
    else if (_resizeOperation & LUConsoleResizeOperationRight)
    {
		insets.right = clampf(insets.right - deltaX, 0, _maxSize.width - (kMinWidth + insets.left));
    }

    if (_resizeOperation == LUConsoleResizeOperationMove)
    {
		deltaX = clampf(deltaX, -insets.left, insets.right);
		deltaY = clampf(deltaY, -insets.top, insets.bottom);
		
		insets.left += deltaX;
		insets.right -= deltaX;
		insets.top += deltaY;
		insets.bottom -= deltaY;
    }
	
	self.topConstraint.constant = insets.top;
	self.leadingConstraint.constant = insets.left;
	self.bottomConstraint.constant = insets.bottom;
	self.trailingConstraint.constant = insets.right;
}

- (void)touchesEnded:(NSSet<UITouch *> *)touches withEvent:(UIEvent *)event
{
    _initialTouch = nil;
}

#pragma mark -
#pragma mark Actions

- (IBAction)onClose:(id)sender
{
    if ([_delegate respondsToSelector:@selector(consoleResizeControllerDidClose:)])
    {
        [_delegate consoleResizeControllerDidClose:self];
    }
}

#pragma mark -
#pragma mark Helpers

- (LUConsoleResizeOperation)lookupResizeOperationForView:(UIView *)view
{
    if (view == _resizeTopBar)
        return LUConsoleResizeOperationTop;
    if (view == _resizeBottomBar)
        return LUConsoleResizeOperationBottom;
    if (view == _resizeLeftBar)
        return LUConsoleResizeOperationLeft;
    if (view == _resizeRightBar)
        return LUConsoleResizeOperationRight;
    if (view == _resizeTopLeftBar)
        return LUConsoleResizeOperationTopLeft;
    if (view == _resizeTopRightBar)
        return LUConsoleResizeOperationTopRight;
    if (view == _resizeBottomLeftBar)
        return LUConsoleResizeOperationBottomLeft;
    if (view == _resizeBottomRightBar)
        return LUConsoleResizeOperationBottomRight;
    
    return LUConsoleResizeOperationMove;
}

@end
