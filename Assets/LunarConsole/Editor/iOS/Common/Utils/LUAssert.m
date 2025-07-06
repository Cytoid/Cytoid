//
//  LUAssert.m
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


#import "LUAssert.h"

#import "Lunar.h"

static LUAssertHandler _assertHandler;

void __lunar_assert(const char* expression, const char* file, int line, const char* function)
{
    __lunar_assert_msg(expression, file, line, function, @"");
}

void __lunar_assert_msg(const char* expression, const char* file, int line, const char* function, NSString *message)
{
    __lunar_assert_msgv(expression, file, line, function, message);
}

void __lunar_assert_msgv(const char* expressionCStr, const char* fileCStr, int line, const char* functionCStr, NSString *format, ...)
{
    va_list ap;
    va_start(ap, format);
    
    NSString *message = [[NSString alloc] initWithFormat:format arguments:ap];
    NSString *consoleMessage = [[NSString alloc] initWithFormat:@"LUNAR/ASSERT: (%s) in %s:%d %s message:'%@'",
                                expressionCStr, fileCStr, line, functionCStr, message];
    
    NSLog(@"%@", consoleMessage);
    
    if (_assertHandler)
    {
        _assertHandler(consoleMessage);
    }
    
    
    va_end(ap);
}
void LUAssertSetHandler(LUAssertHandler handler)
{
    if (_assertHandler != handler)
    {
        _assertHandler = [handler copy];
    }
}
