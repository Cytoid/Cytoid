//
//  LUSortedList.h
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

@interface LUSortedList : NSObject<NSFastEnumeration>

@property (nonatomic, readonly) NSUInteger count;
@property (nonatomic, readonly, nonnull) NSArray *innerArray;
@property (nonatomic, assign, getter=isSortingEnabled) BOOL sortingEnabled;

- (nonnull id)objectAtIndex:(NSUInteger)index;
- (nonnull id)objectAtIndexedSubscript:(NSUInteger)index;

- (NSUInteger)addObject:(nonnull id)object;
- (void)removeObject:(nonnull id)object;
- (void)removeObjectAtIndex:(NSUInteger)index;
- (void)removeAllObjects;

- (NSInteger)indexOfObject:(nonnull id)object;

@end
