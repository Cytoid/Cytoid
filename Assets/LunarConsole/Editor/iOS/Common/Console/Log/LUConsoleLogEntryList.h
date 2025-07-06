//
//  LUConsoleLogEntryList.h
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

#import "LUConsoleLogEntry.h"

@interface LUConsoleLogEntryList : NSObject

@property (nonatomic, readonly) NSUInteger capacity;
@property (nonatomic, readonly) NSUInteger count;
@property (nonatomic, readonly) NSUInteger totalCount;
@property (nonatomic, readonly) NSUInteger trimmedCount;
@property (nonatomic, readonly) NSUInteger trimCount;
@property (nonatomic, readonly) BOOL isTrimmed;

@property (nonatomic, readonly) NSString *filterText;
@property (nonatomic, readonly) BOOL isFiltering;

@property (nonatomic, readonly) NSUInteger logCount;
@property (nonatomic, readonly) NSUInteger warningCount;
@property (nonatomic, readonly) NSUInteger errorCount;

@property (nonatomic, assign, getter=isCollapsed) BOOL collapsed;

+ (instancetype)listWithCapacity:(NSUInteger)capacity trimCount:(NSUInteger)trimCount;
- (instancetype)initWithCapacity:(NSUInteger)capacity trimCount:(NSUInteger)trimCount;

/// Adds entry to the list. Return the entry index or -1 if the entry was filtered out
- (NSInteger)addEntry:(LUConsoleLogEntry *)entry;
- (LUConsoleLogEntry *)entryAtIndex:(NSUInteger)index;
- (void)clear;

- (BOOL)setFilterByText:(NSString *)filterText;
- (BOOL)setFilterByLogType:(LUConsoleLogType)logType disabled:(BOOL)disabled;
- (BOOL)setFilterByLogTypeMask:(LUConsoleLogTypeMask)logTypeMask disabled:(BOOL)disabled;

- (BOOL)isFilterLogTypeEnabled:(LUConsoleLogType)type;

- (NSString *)getText;

@end
