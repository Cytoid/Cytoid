//
//  LUActionRegistryFilter.m
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


#import "Lunar-Full.h"

#import "LUActionRegistryFilter.h"

@interface LUActionRegistryFilter () <LUActionRegistryDelegate>
{
    NSString * _filterText;
}

@property (nonatomic, strong) NSMutableArray * filteredActions;
@property (nonatomic, strong) NSMutableArray * filteredVariables;

@end

@implementation LUActionRegistryFilter

+ (instancetype)filterWithActionRegistry:(LUActionRegistry *)actionRegistry
{
    return [[self alloc] initWithActionRegistry:actionRegistry];
}

- (instancetype)initWithActionRegistry:(LUActionRegistry *)actionRegistry
{
    self = [super init];
    if (self)
    {
        _registry = actionRegistry;
        _registry.delegate = self;
    }
    return self;
}

- (void)dealloc
{
    if (_registry.delegate == self)
    {
        _registry.delegate = nil;
    }
}

#pragma mark -
#pragma mark Filtering

- (BOOL)setFilterText:(NSString *)filterText
{
    if (![_filterText isEqualToString:filterText]) // filter text has changed
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

/// applies filter to already filtered items
- (BOOL)appendFilter
{
    if (self.isFiltering)
    {
        self.filteredActions = [self filterEntries:_filteredActions];
        self.filteredVariables = [self filterEntries:_filteredVariables];
        return YES;
    }
    
    return [self applyFilter];
}

/// setup filtering for the list
- (BOOL)applyFilter
{
    if (_filterText.length > 0)
    {
        self.filteredActions = [self filterEntries:_registry.actions];
        self.filteredVariables = [self filterEntries:_registry.variables];
        return YES;
    }
    
    return [self removeFilter];
}

- (BOOL)removeFilter
{
    if (self.isFiltering)
    {
        self.filteredActions = nil;
        self.filteredVariables = nil;
        return YES;
    }
    
    return NO;
}

- (NSMutableArray *)filterEntries:(NSArray *)entries
{
    NSMutableArray *filteredEntries = [NSMutableArray array];
    for (LUAction *entry in entries)
    {
        if ([self filterEntry:entry])
        {
            [filteredEntries addObject:entry];
        }
    }
    
    return filteredEntries;
}

- (BOOL)filterEntry:(LUEntry *)entry
{
    return _filterText.length == 0 ||
        [entry.name rangeOfString:_filterText options:NSCaseInsensitiveSearch].location != NSNotFound;
}

- (NSUInteger)filteredArray:(NSMutableArray *)array addEntry:(LUEntry *)entry
{
    // insert in the sorted order
    for (NSUInteger index = 0; index < array.count; ++index)
    {
        NSComparisonResult comparisonResult = [entry compare:array[index]];
        if (comparisonResult == NSOrderedAscending)
        {
            [array insertObject:entry atIndex:index];
            return index;
        }
        else if (comparisonResult == NSOrderedSame)
        {
            return index; // filtered group exists
        }
    }
    
    [array addObject:entry];
    return array.count - 1;
}

- (NSUInteger)filteredArray:(NSMutableArray *)array indexOfEntry:(LUEntry *)entry
{
    for (NSUInteger index = 0; index < array.count; ++index)
    {
        LUEntry *existing = array[index];
        if (existing.actionId == entry.actionId)
        {
            return index;
        }
    }
    
    return NSNotFound;
}

#pragma mark -
#pragma mark LUActionRegistryDelegate

- (void)actionRegistry:(LUActionRegistry *)registry didAddAction:(LUAction *)action atIndex:(NSUInteger)index
{
    if (self.isFiltering)
    {
        if (![self filterEntry:action])
        {
            return;
        }
        
        index = [self filteredArray:_filteredActions addEntry:action];
    }
    
    [_delegate actionRegistryFilter:self didAddAction:action atIndex:index];
}

- (void)actionRegistry:(LUActionRegistry *)registry didRemoveAction:(LUAction *)action atIndex:(NSUInteger)index
{
    if (self.isFiltering)
    {
        index = [self filteredArray:_filteredActions indexOfEntry:action];
        if (index == NSNotFound)
        {
            return;
        }
        
        action = [_filteredActions objectAtIndex:index];
        [_filteredActions removeObjectAtIndex:index];
    }
    
    [_delegate actionRegistryFilter:self didRemoveAction:action atIndex:index];
}

- (void)actionRegistry:(LUActionRegistry *)registry didRegisterVariable:(LUCVar *)variable atIndex:(NSUInteger)index
{
    if (self.isFiltering)
    {
        if (![self filterEntry:variable])
        {
            return;
        }
        
        index = [self filteredArray:_filteredVariables addEntry:variable];
    }
    
    [_delegate actionRegistryFilter:self didRegisterVariable:variable atIndex:index];
}

- (void)actionRegistry:(LUActionRegistry *)registry didDidChangeVariable:(LUCVar *)variable atIndex:(NSUInteger)index
{
    if (self.isFiltering)
    {
        if (![self filterEntry:variable])
        {
            return;
        }
        
        index = [self filteredArray:_filteredVariables indexOfEntry:variable];
    }
    
    [_delegate actionRegistryFilter:self didChangeVariable:variable atIndex:index];
}

#pragma mark -
#pragma mark Properties

- (NSArray *)actions
{
    return [self isFiltering] ? _filteredActions : _registry.actions;
}

- (NSArray *)variables
{
    return [self isFiltering] ? _filteredVariables : _registry.variables;
}

- (BOOL)isFiltering
{
    return _filteredActions != nil || _filteredVariables != nil;
}

@end
