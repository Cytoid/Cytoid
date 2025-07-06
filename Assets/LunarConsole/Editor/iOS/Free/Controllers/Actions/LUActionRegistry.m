//
//  LUActionRegistry.m
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

#import "LUActionRegistry.h"

@implementation LUActionRegistry

+ (instancetype)registry
{
    return [[self alloc] init];
}

- (instancetype)init
{
    self = [super init];
    if (self)
    {
    }
    return self;
}

#pragma mark -
#pragma mark Actions

- (LUAction *)registerActionWithId:(int)actionId name:(NSString *)actionName
{
    return nil;
}

- (BOOL)unregisterActionWithId:(int)actionId
{
    return NO;
}

#pragma mark -
#pragma mark Variables

- (LUCVar *)registerVariableWithId:(int)variableId name:(NSString *)name typeName:(NSString *)typeName value:(NSString *)value defaultValue:(NSString *)defaultValue
{
    return nil;
}

- (LUCVar *)registerVariableWithId:(int)variableId name:(NSString *)name typeName:(NSString *)type value:(NSString *)value defaultValue:(NSString *)defaultValue values:(NSArray<NSString *> *)values
{
    return nil;
}

- (void)setValue:(NSString *)value forVariableWithId:(int)variableId
{
}

- (LUCVar *)variableWithId:(int)variableId
{
    return nil;
}

@end
