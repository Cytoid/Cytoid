//
//  LUMutableArray.h
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

@interface LUMutableArray : NSObject<NSFastEnumeration>

@property (nonatomic, readonly) NSUInteger count;           // number of items added (exluding trimmed)
@property (nonatomic, readonly) NSUInteger totalCount;      // total items added (might be more that count if trimmed)
@property (nonatomic, readonly) NSUInteger capacity;        // can't be larger than that
@property (nonatomic, readonly) NSUInteger trimCount;       // trimmed by this amount when overflows
@property (nonatomic, readonly) NSInteger  trimmedCount;    // number of items trimmed (0 if not overfloat)
@property (nonatomic, readonly) BOOL isTrimmed;             // some elements has been removed to avoid an overfloat

+ (instancetype)listWithCapacity:(NSUInteger)capacity trimCount:(NSUInteger)trimCount;
- (instancetype)initWithCapacity:(NSUInteger)capacity trimCount:(NSUInteger)trimCount;

- (void)addObject:(id)object;
- (id)objectAtIndex:(NSUInteger)index;
- (id)objectAtIndexedSubscript:(NSUInteger)index;

- (void)removeAllObjects;

@end
