//
//  LUConsoleLogEntryList.m
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


#import "LUConsoleLogEntryList.h"

#import "Lunar.h"

@interface LUConsoleLogEntryList ()
{
    LUMutableArray            * _entries;           // all entries
    LUMutableArray            * _filteredEntries;   // filtered entries
    LUMutableArray            * _currentEntries;    // current reference to entries (either all entries or filtered)
    LUConsoleLogEntryLookupTable * _entryLookup;       // lookup table for collapsed entries
    LUConsoleLogType            _logDisabledTypesMask;
}

@end

@implementation LUConsoleLogEntryList

+ (instancetype)listWithCapacity:(NSUInteger)capacity trimCount:(NSUInteger)trimCount
{
    return [[[self class] alloc] initWithCapacity:capacity trimCount:trimCount];
}

- (instancetype)initWithCapacity:(NSUInteger)capacity trimCount:(NSUInteger)trimCount
{
    self = [super init];
    if (self)
    {
        _entries = [[LUMutableArray alloc] initWithCapacity:capacity trimCount:trimCount];
        _currentEntries = _entries;
        _logDisabledTypesMask = 0;
    }
    return self;
}


#pragma mark -
#pragma mark Entries

- (NSInteger)addEntry:(LUConsoleLogEntry *)entry
{
    LUAssert(entry);
    if (entry != nil)
    {
        // count types
        LUConsoleLogType entryType = entry.type;
        if (entryType == LUConsoleLogTypeLog)
        {
            ++_logCount;
        }
        else if (entryType == LUConsoleLogTypeWarning)
        {
            ++_warningCount;
        }
        else if (LU_IS_CONSOLE_LOG_TYPE_ERROR(entryType))
        {
            ++_errorCount;
        }
        
        // add entry
        [_entries addObject:entry];
        
        // filter entry
        if (self.isFiltering)
        {
            if ([self filterEntry:entry])
            {
                if (_collapsed)
                {
                    LUConsoleCollapsedLogEntry *collapsedEntry = [_entryLookup addEntry:entry];
                    if (collapsedEntry.index < _filteredEntries.trimmedCount) // first encounter or trimmed?
                    {
                        collapsedEntry.index = _filteredEntries.totalCount;   // we use total count in case if list overflows
                        [_filteredEntries addObject:collapsedEntry];
                    }
                    
                    return collapsedEntry.index - _filteredEntries.trimmedCount;
                }
                
                [_filteredEntries addObject:entry];
                return _filteredEntries.count - 1;
            }
            
            return -1; // if item was rejected - we don't need to update table cells
        }
        
        return _entries.count - 1; // entry was added at the end of the list
    }
    
    return -1;
}

- (LUConsoleLogEntry *)entryAtIndex:(NSUInteger)index
{
    LUAssert(index >= 0 && index < _currentEntries.count);
    return [_currentEntries objectAtIndex:index];
}

- (void)clear
{
    [_entries removeAllObjects];
    [_filteredEntries removeAllObjects];
    [_entryLookup clear];
    
    _logCount = 0;
    _warningCount = 0;
    _errorCount = 0;
}

#pragma mark -
#pragma mark Collapsing

- (void)setCollapsed:(BOOL)collapsed
{
    _collapsed = collapsed;
    if (collapsed)
    {
        LUAssert(_entryLookup == nil);
        _entryLookup = [LUConsoleLogEntryLookupTable new];
    }
    else
    {
        _entryLookup = nil;
    }
    
    [self applyFilter]; // collapsed entries are just a special case of filtered items
}

#pragma mark -
#pragma mark Filtering

- (BOOL)setFilterByText:(NSString *)filterText
{
    if (_filterText != filterText) // filter text has changed
    {
        NSString *oldFilterText = _filterText;
        
        _filterText = filterText;
        
        if (filterText.length > oldFilterText.length && (oldFilterText.length == 0 || [filterText hasPrefix:oldFilterText])) // added more characters
        {
            return [self appendFilter];
        }
        
        return [self applyFilter];
    }
    
    return NO;
}

- (BOOL)setFilterByLogType:(LUConsoleLogType)logType disabled:(BOOL)disabled
{
    return [self setFilterByLogTypeMask:LU_CONSOLE_LOG_TYPE_MASK(logType) disabled:disabled];
}

- (BOOL)setFilterByLogTypeMask:(LUConsoleLogTypeMask)logTypeMask disabled:(BOOL)disabled
{
    LUConsoleLogType oldDisabledTypesMask = _logDisabledTypesMask;
    if (disabled)
    {
        _logDisabledTypesMask |= logTypeMask;
    }
    else
    {
        _logDisabledTypesMask &= ~logTypeMask;
    }
    
    if (oldDisabledTypesMask != _logDisabledTypesMask)
    {
        return disabled ? [self appendFilter] : [self applyFilter];
    }
    
    return NO;
}

- (BOOL)isFilterLogTypeEnabled:(LUConsoleLogType)type
{
    return (_logDisabledTypesMask & LU_CONSOLE_LOG_TYPE_MASK(type)) == 0;
}

/// applies filter to already filtered items
- (BOOL)appendFilter
{
    if (self.isFiltering)
    {
        [self useFilteredFromEntries:_filteredEntries];
        return YES;
    }
    
    return [self applyFilter];
}

/// setup filtering for the list
- (BOOL)applyFilter
{
    BOOL needsFiltering = _collapsed || _filterText.length > 0 || [self hasLogTypeFilters];
    if (needsFiltering)
    {
        [_entryLookup clear]; // if we have collapsed items - we need to rebuild the lookup
        
        [self useFilteredFromEntries:_entries];
        return YES;
    }
    
    return [self removeFilter];
}

- (BOOL)removeFilter
{
    if (self.isFiltering)
    {
        _currentEntries = _entries;
        
        _filteredEntries = nil;
        
        return YES;
    }
    
    return NO;
}

- (void)useFilteredFromEntries:(LUMutableArray *)entries
{
    LUMutableArray *filteredEntries = [self filterEntries:entries];
    
    // use filtered items
    _currentEntries = filteredEntries;
    
    // store filtered items
    _filteredEntries = filteredEntries;
}

- (LUMutableArray *)filterEntries:(LUMutableArray *)entries
{
    LUMutableArray *list = [LUMutableArray listWithCapacity:entries.capacity      // same capacity
                                                  trimCount:entries.trimCount];   // and trim policy as original

    if (_collapsed)
    {
        for (id entry in entries)
        {
            if ([self filterEntry:entry])
            {
                if ([entry isKindOfClass:[LUConsoleCollapsedLogEntry class]])
                {
                    LUConsoleCollapsedLogEntry *collapsedEntry = entry;
                    collapsedEntry.index = list.totalCount; // update item's position
                    [list addObject:collapsedEntry];
                }
                else
                {
                    LUConsoleCollapsedLogEntry *collapsedEntry = [_entryLookup addEntry:entry];
                    if (collapsedEntry.count == 1) // first encounter
                    {
                        collapsedEntry.index = list.totalCount;
                        [list addObject:collapsedEntry];
                    }
                }
            }
        }
    }
    else
    {
        for (LUConsoleLogEntry *entry in entries)
        {
            if ([self filterEntry:entry])
            {
                [list addObject:entry];
            }
        }
    }
    
    return list;
}

- (BOOL)filterEntry:(LUConsoleLogEntry *)entry
{
    // filter by log type
    if (_logDisabledTypesMask & LU_CONSOLE_LOG_TYPE_MASK(entry.type))
    {
        return NO;
    }
    
    // filter by message
    return _filterText.length == 0 || [entry.message.text rangeOfString:_filterText options:NSCaseInsensitiveSearch].location != NSNotFound;
}

#pragma mark -
#pragma mark Text

- (NSString *)getText
{
    NSMutableString *text = [NSMutableString string];
    
    NSUInteger index = 0;
    NSUInteger count = _entries.count;
    for (LUConsoleLogEntry *entry in _entries)
    {
        [text appendString:entry.message.text];
        if (entry.type == LUConsoleLogTypeException && entry.hasStackTrace)
        {
            [text appendString:@"\n"];
            [text appendString:entry.stackTrace];
        }
        if (++index < count)
        {
            [text appendString:@"\n"];
        }
    }
    
    return text;
}

#pragma mark -
#pragma mark Helpers

- (BOOL)hasLogTypeFilters
{
    return _logDisabledTypesMask != 0;
}

#pragma mark -
#pragma mark Properties

- (NSUInteger)capacity
{
    return _currentEntries.capacity;
}

- (NSUInteger)count
{
    return _currentEntries.count;
}

- (NSUInteger)totalCount
{
    return _currentEntries.totalCount;
}

- (NSUInteger)trimmedCount
{
    return _currentEntries.trimmedCount;
}

- (NSUInteger)trimCount
{
    return _currentEntries.trimCount;
}

- (BOOL)isTrimmed
{
    return _currentEntries.isTrimmed;
}

- (BOOL)isFiltering
{
    return _filteredEntries != nil;
}

@end
