//
//  LUAvailability.h
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


#import <Foundation/Foundation.h>

#ifndef __IPHONE_9_0
#define __IPHONE_9_0     90000
#endif

#define LU_SYSTEM_VERSION_MIN __IPHONE_9_0

#define LU_IOS_VERSION_AVAILABLE(sys_ver) lunar_ios_version_available(sys_ver)
#define LU_IOS_MIN_VERSION_AVAILABLE (LU_IOS_VERSION_AVAILABLE(LU_SYSTEM_VERSION_MIN))
#define LU_SELECTOR_AVAILABLE(obj, sel) [(obj) respondsToSelector:@selector(sel)]
#define LU_CLASS_AVAILABLE(className) (NSClassFromString(@#className) != nil)

typedef NSUInteger LUSystemVersion;

BOOL lunar_ios_version_available(LUSystemVersion version);
