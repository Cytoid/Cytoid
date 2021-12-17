//
//  LUConsoleLogEntryLookupTable.m
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


#import "LUConsoleLogEntryLookupTable.h"

#import "Lunar.h"

@interface LUConsoleLogEntryLookupTable ()
{
    NSMutableDictionary * _table; // TODO: replace with a search trie
}

@end

@implementation LUConsoleLogEntryLookupTable

- (instancetype)init
{
    self = [super init];
    if (self)
    {
        _table = [[NSMutableDictionary alloc] init];
    }
    return self;
}


- (LUConsoleCollapsedLogEntry *)addEntry:(LUConsoleLogEntry *)entry
{
    LUAssert(entry);
    
    NSString *message = entry.message.text;
    if (message)
    {
        LUConsoleCollapsedLogEntry *collapsedEntry = [_table objectForKey:message];
        if (collapsedEntry == nil)
        {
            collapsedEntry = [LUConsoleCollapsedLogEntry entryWithEntry:entry];
            [_table setObject:collapsedEntry forKey:message];
        }
        else
        {
            [collapsedEntry increaseCount];
        }
        
        return collapsedEntry;
    }
    
    LUAssert(message);
    return nil;
}

- (void)removeEntry:(LUConsoleCollapsedLogEntry *)entry
{
    LUAssert(entry);
    
    NSString *message = entry.message.text;
    if (message)
    {
        [_table removeObjectForKey:message];
    }
}

- (void)clear
{
    [_table removeAllObjects];
}

@end
