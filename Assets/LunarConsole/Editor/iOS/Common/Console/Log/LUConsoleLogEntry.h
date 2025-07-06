//
//  LUConsoleLogEntry.h
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


#import <UIKit/UIKit.h>

typedef enum : uint8_t {
    LUConsoleLogTypeError,
    LUConsoleLogTypeAssert,
    LUConsoleLogTypeWarning,
    LUConsoleLogTypeLog,
    LUConsoleLogTypeException
} LUConsoleLogType;

typedef uint8_t LUConsoleLogTypeMask;

#define LU_CONSOLE_LOG_TYPE_COUNT 5
#define LU_CONSOLE_LOG_TYPE_MASK(TYPE)     (1 << (TYPE))

#define LU_IS_CONSOLE_LOG_TYPE_VALID(TYPE) ((TYPE) >= 0 && (TYPE) < LU_CONSOLE_LOG_TYPE_COUNT)
#define LU_IS_CONSOLE_LOG_TYPE_ERROR(TYPE) ((TYPE) == LUConsoleLogTypeException || \
                                            (TYPE) == LUConsoleLogTypeError || \
                                            (TYPE) == LUConsoleLogTypeAssert)

@class LULogMessage;

@interface LUConsoleLogEntry : NSObject

@property (nonatomic, readonly) LUConsoleLogType type;
@property (nonatomic, readonly) LULogMessage * message;
@property (nonatomic, readonly) NSString * stackTrace;
@property (nonatomic, readonly) UIImage  * icon;
@property (nonatomic, readonly) BOOL hasStackTrace;

+ (instancetype)entryWithType:(LUConsoleLogType)type message:(LULogMessage *)message stackTrace:(NSString *)stackTrace;
- (instancetype)initWithType:(LUConsoleLogType)type message:(LULogMessage *)message stackTrace:(NSString *)stackTrace;

- (UITableViewCell *)tableView:(UITableView *)tableView cellAtIndex:(NSUInteger)index;
- (CGSize)cellSizeForTableView:(UITableView *)tableView;

@end

/// Console entry for holding collapsed items
@interface LUConsoleCollapsedLogEntry : LUConsoleLogEntry

/// Index in the entry list
@property (nonatomic, assign) NSInteger index;

/// Total amount of encounters
@property (nonatomic, assign) NSInteger count;

+ (instancetype)entryWithEntry:(LUConsoleLogEntry *)entry;
- (instancetype)initWithEntry:(LUConsoleLogEntry *)entry;

- (void)increaseCount;

@end

/// Console entry for displaying in overlay view
@interface LUConsoleOverlayLogEntry : LUConsoleLogEntry

/// Scheduled removal date (after becoming visible)
@property (nonatomic, strong) NSDate *removalDate;

- (instancetype)initWithEntry:(LUConsoleLogEntry *)entry;

@end
