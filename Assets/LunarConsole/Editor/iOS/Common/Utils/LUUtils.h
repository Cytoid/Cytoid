//
//  LUUtils.h
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
#import "LUAvailability.h"
#import "LUDefines.h"
#import "LUFileUtils.h"
#import "LUImageUtils.h"
#import "LULittleHelper.h"
#import "LUMutableArray.h"
#import "LUNotificationCenter.h"
#import "LUObject.h"
#import "LUSerializableObject.h"
#import "LUSerializationUtils.h"
#import "LUSortedList.h"
#import "LUStacktrace.h"
#import "LUStringUtils.h"
#import "LUThreading.h"

LU_INLINE BOOL
LUFloatApprox(float a, float b)
{
    return fabsf(a - b) < 0.00001;
}
