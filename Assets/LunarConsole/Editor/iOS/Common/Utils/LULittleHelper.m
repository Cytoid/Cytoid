//
//  LULittleHelper.m
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


#import "LULittleHelper.h"

#import "Lunar.h"

void LUDisplayAlertView(NSString *title, NSString *message)
{
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
    UIAlertView *alertView = [[UIAlertView alloc] initWithTitle:title
                                                        message:message
                                                       delegate:nil
                                              cancelButtonTitle:@"OK"
                                              otherButtonTitles:nil];
    [alertView show];
#pragma clang diagnostic pop
}

CGRect LUGetScreenBounds()
{
    if (LU_IOS_VERSION_AVAILABLE(__IPHONE_8_0)) {
        return [UIScreen mainScreen].bounds;
    }

    CGRect screenSize = [UIScreen mainScreen].bounds;
    if (LUIsLandscapeInterfaceOrientation()) {
        CGFloat width = CGRectGetWidth(screenSize);
        CGFloat height = CGRectGetHeight(screenSize);
        screenSize.size = CGSizeMake(height, width);
    }

    return screenSize;
}

UIInterfaceOrientation LUGetInterfaceOrientation()
{
    return [UIApplication sharedApplication].statusBarOrientation;
}

BOOL LUIsPortraitInterfaceOrientation(void)
{
    return UIInterfaceOrientationIsPortrait(LUGetInterfaceOrientation());
}

BOOL LUIsLandscapeInterfaceOrientation()
{
    return UIInterfaceOrientationIsLandscape(LUGetInterfaceOrientation());
}
