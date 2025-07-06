//
//  LUViewController.m
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


#import "LUViewController.h"

#import "Lunar.h"

@interface LUViewController ()

@property (nonatomic, weak) LUConsolePopupController *popupController;

@end

@implementation LUViewController

#pragma mark -
#pragma mark Status Bar Management

- (BOOL)prefersStatusBarHidden
{
    return YES;
}

#pragma mark -
#pragma mark Interface Orientation

- (BOOL)shouldAutorotate
{
    return NO;
}

#pragma mark -
#pragma mark Popup controller

- (CGSize)preferredPopupSize
{
    return CGSizeZero;
}

- (void)setPopupController:(LUConsolePopupController *)controller
{
    _popupController = controller;
}

#pragma mark -
#pragma mark Child controllers

- (void)addChildController:(UIViewController *)childController withFrame:(CGRect)frame
{
    [self addChildViewController:childController];
    childController.view.frame = frame;
    [self.view addSubview:childController.view];
    [childController didMoveToParentViewController:self];
}

- (void)removeChildController:(UIViewController *)childController
{
    [childController willMoveToParentViewController:nil];
    [childController.view removeFromSuperview];
    [childController removeFromParentViewController];
}

@end
