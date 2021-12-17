//
//  LUSortedList.m
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

#import "LUSortedList.h"

@interface LUSortedList ()
{
    NSMutableArray * _array;
}

@end

@implementation LUSortedList

- (instancetype)init
{
    self = [super init];
    if (self)
    {
        _array = [NSMutableArray new];
        _sortingEnabled = YES;
    }
    return self;
}

#pragma mark -
#pragma mark Objects

- (nonnull id)objectAtIndex:(NSUInteger)index
{
    return [_array objectAtIndex:index];
}

- (nonnull id)objectAtIndexedSubscript:(NSUInteger)index
{
    return [_array objectAtIndexedSubscript:index];
}

- (NSUInteger)addObject:(nonnull id)object
{
    LUAssert(object != nil);
    LUAssertMsgv([object respondsToSelector:@selector(compare:)],
                 @"Can't add non-comparable object to a sorted list: %@", [object class]);
    
    if (object != nil)
    {
        if (_sortingEnabled && [object respondsToSelector:@selector(compare:)])
        {
            // TODO: use binary search to insert in a sorted order
            for (NSUInteger i = 0; i < _array.count; ++i)
            {
                NSComparisonResult comparisonResult = [object compare:_array[i]];
                if (comparisonResult == NSOrderedAscending)
                {
                    [_array insertObject:object atIndex:i];
                    return i;
                }
                else if (comparisonResult == NSOrderedSame)
                {
                    _array[i] = object;
                    return i;
                }
            }
        }
        
        [_array addObject:object];
        return _array.count - 1;
    }
    
    return NSNotFound;
}

- (void)removeObject:(nonnull id)object
{
    LUAssert(object != nil);
    if (object != nil)
    {
        [_array removeObject:object];
    }
}

- (void)removeObjectAtIndex:(NSUInteger)index
{
    [_array removeObjectAtIndex:index];
}

- (void)removeAllObjects
{
    [_array removeAllObjects];
}

- (NSInteger)indexOfObject:(nonnull id)object
{
    LUAssert(object != nil);
    return object != nil ? [_array indexOfObject:object] : -1;
}

#pragma mark -
#pragma mark NSFastEnumeration

- (NSUInteger)countByEnumeratingWithState:(NSFastEnumerationState *)state objects:(id __unsafe_unretained [])buffer count:(NSUInteger)len
{
    return [_array countByEnumeratingWithState:state objects:buffer count:len];
}

#pragma mark -
#pragma mark Properties

- (NSUInteger)count
{
    return _array.count;
}

- (NSArray *)innerArray
{
    return _array;
}

@end
