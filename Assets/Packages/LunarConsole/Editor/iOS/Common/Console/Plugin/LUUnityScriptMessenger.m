//
//  LUUnityScriptMessenger.m
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


#import "LUUnityScriptMessenger.h"

#include "Lunar.h"

extern void UnitySendMessage(const char *objectName, const char *methodName, const char *message);

@interface LUUnityScriptMessenger ()
{
    NSString * _targetName;
    NSString * _methodName;
}

@end

@implementation LUUnityScriptMessenger

- (instancetype)initWithTargetName:(NSString *)targetName methodName:(NSString *)methodName
{
    self = [super init];
    if (self)
    {
        if (targetName.length == 0)
        {
            NSLog(@"Can't create script messenger: target name is nil or empty");
            self = nil;
            return nil;
        }
        
        if (methodName.length == 0)
        {
            NSLog(@"Can't create script messenger: method name is nil or empty");
            self = nil;
            return nil;
        }
        
        _targetName = [targetName copy];
        _methodName = [methodName copy];
    }
    return self;
}


- (void)sendMessageName:(NSString *)name
{
    [self sendMessageName:name params:nil];
}

- (void)sendMessageName:(NSString *)name params:(NSDictionary *)params
{
    NSMutableDictionary *dict = [NSMutableDictionary dictionaryWithObjectsAndKeys:name, @"name", nil];
    if (params.count > 0)
    {
        [dict addEntriesFromDictionary:params];
    }
    
    NSString *paramString = LUSerializeDictionaryToString(dict);
    UnitySendMessage(_targetName.UTF8String, _methodName.UTF8String, paramString.UTF8String);
}

@end
