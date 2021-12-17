//
//  LUNotificationCenter.m
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


#import "LUNotificationCenter.h"

#import "Lunar.h"

static id<LUNotificationCenterImpl> _impl;

@interface LUNotificationCenterDefault : NSObject <LUNotificationCenterImpl>

@end

@implementation LUNotificationCenter

+ (void)initialize
{
    if ([self class] == [LUNotificationCenter class])
    {
        [self setImpl:[LUNotificationCenterDefault new]];
    }
}

+ (void)setImpl:(id<LUNotificationCenterImpl>)impl
{
    _impl = impl ? impl : [LUNotificationCenterDefault new];
}

+ (void)addObserver:(id)observer selector:(SEL)selector name:(NSNotificationName)name object:(id)object
{
    [_impl addObserver:observer selector:selector name:name object:object];
}

+ (void)removeObserver:(id)observer
{
    [_impl removeObserver:observer];
}

+ (void)removeObserver:(id)observer name:(NSNotificationName)name object:(id)object
{
    [_impl removeObserver:observer name:name object:object];
}

+ (void)postNotificationName:(NSNotificationName)name object:(id)object
{
    [self postNotificationName:name object:object userInfo:nil];
}

+ (void)postNotificationName:(NSNotificationName)name object:(id)object userInfo:(NSDictionary *)userInfo
{
    [_impl postNotificationName:name object:object userInfo:userInfo];
}

@end

@implementation LUNotificationCenterDefault

- (void)addObserver:(id)observer selector:(SEL)selector name:(NSNotificationName)name object:(id)object
{
    [[NSNotificationCenter defaultCenter] addObserver:observer selector:selector name:name object:object];
}

- (void)removeObserver:(id)observer
{
    [[NSNotificationCenter defaultCenter] removeObserver:observer];
}

- (void)removeObserver:(id)observer name:(NSNotificationName)name object:(id)object
{
    [[NSNotificationCenter defaultCenter] removeObserver:observer name:name object:object];
}

- (void)postNotificationName:(NSNotificationName)name object:(id)object userInfo:(NSDictionary *)userInfo
{
    [[NSNotificationCenter defaultCenter] postNotificationName:name object:object userInfo:userInfo];
}

@end
