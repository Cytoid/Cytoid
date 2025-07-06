//
//  LUAvailability.m
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


#import <UIKit/UIKit.h>

#import "LUAvailability.h"

static LUSystemVersion _systemVersion;

BOOL lunar_ios_version_available(LUSystemVersion version)
{
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        
        NSArray *tokens = [[UIDevice currentDevice].systemVersion componentsSeparatedByString:@"."];
        int multiplier = 10000;
        
        _systemVersion = 0;
        for (int i = 0; i < tokens.count; ++i)
        {
            int val = [[tokens objectAtIndex:i] intValue];
            _systemVersion += val * multiplier;
            multiplier /= 100;
        }
        
        NSLog(@"System version: %ld", (unsigned long)_systemVersion);
    });
    
    return _systemVersion >= version;
}