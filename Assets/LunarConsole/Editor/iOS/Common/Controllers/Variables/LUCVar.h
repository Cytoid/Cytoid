//
//  LUCVar.h
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


#import "LUEntry.h"

typedef enum : NSUInteger {
    LUCVarTypeUnknown,
    LUCVarTypeBoolean,
    LUCVarTypeInteger,
    LUCVarTypeFloat,
    LUCVarTypeString,
    LUCVarTypeEnum
} LUCVarType;

typedef enum : NSUInteger {
	LUCVarFlagsNone = 0,
	LUCVarFlagsHidden = 1 << 1,
	LUCVarFlagsNoArchive = 1 << 2
} LUCVarFlags;

typedef struct _LUCVarRange {
    CGFloat min;
    CGFloat max;
} LUCVarRange;

NS_INLINE LUCVarRange LUMakeCVarRange(CGFloat min, CGFloat max) {
    LUCVarRange r;
    r.min = min;
    r.max = max;
    return r;
}

extern NSString * const LUCVarTypeNameBoolean;
extern NSString * const LUCVarTypeNameInteger;
extern NSString * const LUCVarTypeNameFloat;
extern NSString * const LUCVarTypeNameString;
extern NSString * const LUCVarTypeNameEnum;
extern NSString * const LUCVarTypeNameUnknown;

@interface LUCVar : LUEntry

@property (nonatomic, readonly) LUCVarType type;
@property (nonatomic, strong) NSString *value;
@property (nonatomic, strong) NSString *defaultValue;
@property (nonatomic, readonly) BOOL isDefaultValue;
@property (nonatomic, strong) NSArray<NSString *> *values;
@property (nonatomic, assign) LUCVarFlags flags;
@property (nonatomic, assign) LUCVarRange range;
@property (nonatomic, readonly) BOOL hasRange;

+ (instancetype)variableWithId:(int)entryId name:(NSString *)name value:(NSString *)value defaultValue:(NSString *)defaultValue type:(LUCVarType)type cellClass:(Class)cellClass;
- (instancetype)initWithId:(int)entryId name:(NSString *)name value:(NSString *)value defaultValue:(NSString *)defaultValue type:(LUCVarType)type cellClass:(Class)cellClass;

+ (LUCVarType)typeForName:(NSString *)type;
+ (NSString *)typeNameForType:(LUCVarType)type;

- (void)resetToDefaultValue;
- (BOOL)hasFlag:(LUCVarFlags)flag;

@end
