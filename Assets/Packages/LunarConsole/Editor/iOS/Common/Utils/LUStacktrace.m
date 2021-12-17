//
//  LUStacktrace.m
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


#import "LUStacktrace.h"

static NSString * const MARKER_AT = @" (at ";
static NSString * const MARKER_ASSETS = @"/Assets/";

@implementation LUStacktrace

+ (NSString *)optimizeStacktrace:(NSString *)stacktrace
{
    if (stacktrace.length > 0)
    {
        NSArray *lines = [stacktrace componentsSeparatedByString:@"\n"];
        NSMutableArray *newLines = [NSMutableArray arrayWithCapacity:lines.count];
        for (NSString *line in lines)
        {
            [newLines addObject:[self optimizeLine:line]];
        }
        
        return [newLines componentsJoinedByString:@"\n"];
    }
    
    return nil;
}
             
+ (NSString *)optimizeLine:(NSString *)line
{
    NSRange startRange = [line rangeOfString:MARKER_AT];
    if (startRange.location == NSNotFound) return line;
    
    NSRange endRange = [line rangeOfString:MARKER_ASSETS options:NSBackwardsSearch];
    if (endRange.location == NSNotFound) return line;
    
    NSString *s1 = [line substringWithRange:NSMakeRange(0, startRange.location + startRange.length)];
    NSString *s2 = [line substringFromIndex:endRange.location + 1];
    
    return [s1 stringByAppendingString:s2];
}

@end
