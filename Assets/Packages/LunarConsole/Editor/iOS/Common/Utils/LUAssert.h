//
//  LUAssert.h
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

typedef void (^LUAssertHandler)(NSString *message);

#define LUAssert(expression) if (!(expression)) __lunar_assert(#expression, __FILE__, __LINE__, __FUNCTION__)
#define LUAssertMsg(expression, msg) if (!(expression)) __lunar_assert_msg(#expression, __FILE__, __LINE__, __FUNCTION__, (msg))
#define LUAssertMsgv(expression, msg, ...) if (!(expression)) __lunar_assert_msgv(#expression, __FILE__, __LINE__, __FUNCTION__, (msg), __VA_ARGS__)

void __lunar_assert(const char* expression, const char* file, int line, const char* function);
void __lunar_assert_msg(const char* expression, const char* file, int line, const char* function, NSString *message);
void __lunar_assert_msgv(const char* expression, const char* file, int line, const char* function, NSString *format, ...);
void LUAssertSetHandler(LUAssertHandler handler);