//
//  LUMutableArray.m
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


#import "LUMutableArray.h"

#import "Lunar.h"

@interface LUMutableArray ()
{
    NSMutableArray * _objects;
}

@end

@implementation LUMutableArray

+ (instancetype)listWithCapacity:(NSUInteger)capacity trimCount:(NSUInteger)trimCount
{
    return [[self alloc] initWithCapacity:capacity trimCount:trimCount];
}

- (instancetype)initWithCapacity:(NSUInteger)capacity trimCount:(NSUInteger)trimCount
{
    self = [super init];
    if (self)
    {
        _capacity   = capacity;
        _trimCount  = trimCount;
        _objects    = [NSMutableArray new];
    }
    return self;
}


#pragma mark -
#pragma mark Objects

- (void)addObject:(id)object
{
    LUAssert(object);
    if (object)
    {
        if (_objects.count == _capacity) // overflows?
        {
            [_objects removeObjectsInRange:NSMakeRange(0, _trimCount)]; // trim objects to get extra space
        }
        [_objects addObject:object];
        
        ++_totalCount; // keep track of the total amount of objects added
    }
}

- (id)objectAtIndex:(NSUInteger)index
{
    return [_objects objectAtIndex:index];
}

- (id)objectAtIndexedSubscript:(NSUInteger)index
{
    return [_objects objectAtIndexedSubscript:index];
}

- (void)removeAllObjects
{
    _objects = [NSMutableArray new];
    _totalCount = 0;
}

#pragma mark -
#pragma mark NSFastEnumeration

- (NSUInteger)countByEnumeratingWithState:(NSFastEnumerationState *)state objects:(id __unsafe_unretained [])buffer count:(NSUInteger)len
{
    return [_objects countByEnumeratingWithState:state objects:buffer count:len];
}

#pragma mark -
#pragma mark Properties

- (NSUInteger)count
{
    return _objects.count;
}

- (NSInteger)trimmedCount
{
    return _totalCount - _objects.count;
}

- (BOOL)isTrimmed
{
    return [self trimmedCount] > 0;
}

@end
