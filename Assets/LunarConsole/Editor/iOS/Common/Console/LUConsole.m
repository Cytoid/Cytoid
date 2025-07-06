//
//  LUConsole.m
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


#import "LUConsole.h"

#import "Lunar.h"

@interface LUConsole ()
{
    LUConsoleLogEntryList * _entries;
}

@end

@implementation LUConsole

- (instancetype)initWithCapacity:(NSUInteger)capacity trimCount:(NSUInteger)trimCount
{
    self = [super init];
    if (self)
    {
        _entries = [[LUConsoleLogEntryList alloc] initWithCapacity:capacity trimCount:trimCount];
    }
    return self;
}


#pragma mark -
#pragma mark Entries

- (LUConsoleLogEntry *)entryAtIndex:(NSUInteger)index
{
    return [_entries entryAtIndex:index];
}

- (void)logMessage:(LULogMessage *)message stackTrace:(NSString *)stackTrace type:(LUConsoleLogType)type
{
    NSUInteger oldTotalCount   = _entries.totalCount;   // total count before we added a new item
    NSUInteger oldTrimmedCount = _entries.trimmedCount; // trimmed count before we added a new item
    
    LUConsoleLogEntry *entry = [[LUConsoleLogEntry alloc] initWithType:type message:message stackTrace:stackTrace];
    NSInteger index = [_entries addEntry:entry];

    NSInteger trimmed = _entries.trimmedCount - oldTrimmedCount;
    if (trimmed > 0) // more items are trimmed
    {
        if ([_delegate respondsToSelector:@selector(lunarConsole:didRemoveRange:)])
        {
            [_delegate lunarConsole:self didRemoveRange:NSMakeRange(0, trimmed)];
        }
    }
    
    if (oldTotalCount != _entries.totalCount)
    {
        [_delegate lunarConsole:self didAddEntryAtIndex:index trimmedCount:trimmed];
    }
    else if (index != -1)
    {
        [_delegate lunarConsole:self didUpdateEntryAtIndex:index trimmedCount:trimmed];
    }
    
}

- (void)clear
{
    [_entries clear];
    if ([_delegate respondsToSelector:@selector(lunarConsoleDidClearEntries:)])
    {
        [_delegate lunarConsoleDidClearEntries:self];
    }
}

- (NSString *)getText
{
    return [_entries getText];
}

#pragma mark -
#pragma mark Properties

- (NSUInteger)capacity
{
    return _entries.capacity;
}

- (NSUInteger)entriesCount
{
    return _entries.count;
}

- (NSUInteger)trimmedCount
{
    return _entries.trimmedCount;
}

- (NSUInteger)trimCount
{
    return _entries.trimCount;
}

- (BOOL)isTrimmed
{
    return _entries.isTrimmed;
}

- (void)setCollapsed:(BOOL)collapsed
{
    _entries.collapsed = collapsed;
}

- (BOOL)isCollapsed
{
    return _entries.isCollapsed;
}

@end
