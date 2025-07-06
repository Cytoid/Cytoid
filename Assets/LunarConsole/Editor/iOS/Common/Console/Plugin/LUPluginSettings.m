//
//  LUPluginSettings.m
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


#import "LUPluginSettings.h"

NSNotificationName const LUNotificationSettingsDidChange = @"SettingsDidChange";

static NSString *NSStringFromGesture(LUConsoleGesture gesture)
{
    switch (gesture) {
        case LUConsoleGestureSwipeDown:
            return @"swipe_down";
        case LUConsoleGestureNone:
            return @"none";
    }
}

static LUConsoleGesture parseGesture(id value)
{
    if ([value isKindOfClass:[NSString class]]) {
        if ([value isEqualToString:@"swipe_down"]) {
            return LUConsoleGestureSwipeDown;
        }
        if ([value isEqualToString:@"none"]) {
            return LUConsoleGestureNone;
        }

        return LUConsoleGestureSwipeDown;
    }
    return (LUConsoleGesture)[value intValue];
}

static NSString *NSStringFromDisplayMode(LUDisplayMode mode)
{
    switch (mode) {
        case LUDisplayModeNone:
            return @"none";
        case LUDisplayModeErrors:
            return @"errors";
        case LUDisplayModeExceptions:
            return @"exceptions";
        case LUDisplayModeAll:
            return @"all";
    }
}

static LUDisplayMode parseDisplayMode(id value)
{
    if ([value isKindOfClass:[NSString class]]) {
        if ([value isEqualToString:@"all"]) {
            return LUDisplayModeAll;
        }
        if ([value isEqualToString:@"errors"]) {
            return LUDisplayModeErrors;
        }
        if ([value isEqualToString:@"exceptions"]) {
            return LUDisplayModeExceptions;
        }
        if ([value isEqualToString:@"none"]) {
            return LUDisplayModeNone;
        }

        return LUDisplayModeAll;
    }
    return (LUDisplayMode)[value intValue];
}

@implementation LUExceptionWarningSettings

- (instancetype)initWithDictionary:(NSDictionary *)dict
{
    self = [super init];
    if (self) {
        _displayMode = parseDisplayMode(dict[@"displayMode"]);
    }
    return self;
}

#pragma mark -
#pragma mark NSCoding

- (void)encodeWithCoder:(NSCoder *)coder
{
    [coder encodeObject:NSStringFromDisplayMode(self.displayMode) forKey:@"displayMode"];
}

- (nullable instancetype)initWithCoder:(NSCoder *)decoder
{
    self = [super init];
    if (self) {
        _displayMode = parseDisplayMode([decoder decodeObjectForKey:@"displayMode"]);
    }
    return self;
}

#pragma mark -
#pragma mark Equality

- (BOOL)isEqual:(id)object
{
    if (self == object) {
        return YES;
    }

    if (![object isKindOfClass:[self class]]) {
        return NO;
    }

    LUExceptionWarningSettings *other = object;
    return self.displayMode == other.displayMode;
}

@end

@implementation LUColor

- (instancetype)initWithDictionary:(NSDictionary *)dict
{
    self = [super init];
    if (self) {
        _r = (UInt8)[dict[@"r"] intValue];
        _g = (UInt8)[dict[@"g"] intValue];
        _b = (UInt8)[dict[@"b"] intValue];
        _a = (UInt8)[dict[@"a"] intValue];
    }
    return self;
}

- (UIColor *)UIColor
{
    static const float multiplier = 1.0 / 255.0;
    return [UIColor colorWithRed:multiplier * _r green:multiplier * _g blue:multiplier * _b alpha:multiplier * _a];
}

#pragma mark -
#pragma mark NSCoding

- (void)encodeWithCoder:(NSCoder *)coder
{
    [coder encodeInteger:self.r forKey:@"r"];
    [coder encodeInteger:self.g forKey:@"g"];
    [coder encodeInteger:self.b forKey:@"b"];
    [coder encodeInteger:self.a forKey:@"a"];
}

- (nullable instancetype)initWithCoder:(NSCoder *)decoder
{
    self = [super init];
    if (self) {
        _r = [decoder decodeIntegerForKey:@"r"];
        _g = [decoder decodeIntegerForKey:@"g"];
        _b = [decoder decodeIntegerForKey:@"b"];
        _a = [decoder decodeIntegerForKey:@"a"];
    }
    return self;
}

#pragma mark -
#pragma mark Equality

- (BOOL)isEqual:(id)object
{
    if (self == object) {
        return YES;
    }

    if (![object isKindOfClass:[self class]]) {
        return NO;
    }

    LUColor *other = object;
    return self.r == other.r && self.g == other.g && self.b == other.b && self.a == other.a;
}

@end

@implementation LULogColors

- (instancetype)initWithDictionary:(NSDictionary *)dict
{
    self = [super init];
    if (self) {
        _exception = [[LULogEntryColors alloc] initWithDictionary:dict[@"exception"]];
        _error = [[LULogEntryColors alloc] initWithDictionary:dict[@"error"]];
        _warning = [[LULogEntryColors alloc] initWithDictionary:dict[@"warning"]];
        _debug = [[LULogEntryColors alloc] initWithDictionary:dict[@"debug"]];
    }
    return self;
}

#pragma mark -
#pragma mark NSCoding

- (void)encodeWithCoder:(NSCoder *)coder
{
    [coder encodeObject:_exception forKey:@"exception"];
    [coder encodeObject:_error forKey:@"error"];
    [coder encodeObject:_warning forKey:@"warning"];
    [coder encodeObject:_debug forKey:@"debug"];
}

- (nullable instancetype)initWithCoder:(NSCoder *)decoder
{
    self = [super init];
    if (self) {
        _exception = [decoder decodeObjectForKey:@"exception"];
        _error = [decoder decodeObjectForKey:@"error"];
        _warning = [decoder decodeObjectForKey:@"warning"];
        _debug = [decoder decodeObjectForKey:@"debug"];
    }
    return self;
}

#pragma mark -
#pragma mark Equality

- (BOOL)isEqual:(id)object
{
    if (self == object) {
        return YES;
    }

    if (![object isKindOfClass:[self class]]) {
        return NO;
    }

    LULogColors *other = object;
    return [self.exception isEqual:other.exception] &&
           [self.error isEqual:other.error] &&
           [self.warning isEqual:other.warning] &&
           [self.debug isEqual:other.debug];
}

@end

@implementation LULogEntryColors

- (instancetype)initWithDictionary:(NSDictionary *)dict
{
    self = [super init];
    if (self) {
        _foreground = [[LUColor alloc] initWithDictionary:dict[@"foreground"]];
        _background = [[LUColor alloc] initWithDictionary:dict[@"background"]];
    }
    return self;
}

#pragma mark -
#pragma mark NSCoding

- (void)encodeWithCoder:(NSCoder *)coder
{
    [coder encodeObject:self.foreground forKey:@"foreground"];
    [coder encodeObject:self.background forKey:@"background"];
}

- (nullable instancetype)initWithCoder:(NSCoder *)decoder
{
    self = [super init];
    if (self) {
        _foreground = [decoder decodeObjectForKey:@"foreground"];
        _background = [decoder decodeObjectForKey:@"background"];
    }
    return self;
}

#pragma mark -
#pragma mark Equality

- (BOOL)isEqual:(id)object
{
    if (self == object) {
        return YES;
    }

    if (![object isKindOfClass:[self class]]) {
        return NO;
    }

    LULogEntryColors *other = object;
    return [self.foreground isEqual:other.foreground] &&
           [self.background isEqual:other.background];
}

@end

@implementation LULogOverlaySettings

- (instancetype)initWithDictionary:(NSDictionary *)dict
{
    self = [super init];
    if (self) {
        _enabled = [dict[@"enabled"] boolValue];
        _maxVisibleLines = [dict[@"maxVisibleLines"] intValue];
        _timeout = [dict[@"timeout"] doubleValue];
        _colors = [[LULogColors alloc] initWithDictionary:dict[@"colors"]];
    }
    return self;
}

#pragma mark -
#pragma mark NSCoding

- (void)encodeWithCoder:(NSCoder *)coder
{
    [coder encodeBool:self.enabled forKey:@"enabled"];
    [coder encodeInteger:self.maxVisibleLines forKey:@"maxVisibleLines"];
    [coder encodeDouble:self.timeout forKey:@"timeout"];
    [coder encodeObject:self.colors forKey:@"colors"];
}

- (nullable instancetype)initWithCoder:(NSCoder *)decoder
{
    self = [super init];
    if (self) {
        _enabled = [decoder decodeBoolForKey:@"enabled"];
        _maxVisibleLines = [decoder decodeIntegerForKey:@"maxVisibleLines"];
        _timeout = [decoder decodeDoubleForKey:@"timeout"];
        _colors = [decoder decodeObjectForKey:@"colors"];
    }
    return self;
}

#pragma mark -
#pragma mark Equality

- (BOOL)isEqual:(id)object
{
    if (self == object) {
        return YES;
    }

    if (![object isKindOfClass:[self class]]) {
        return NO;
    }

    LULogOverlaySettings *other = object;
    return [self.colors isEqual:other.colors] &&
           self.isEnabled == other.isEnabled &&
           self.maxVisibleLines == other.maxVisibleLines &&
           self.timeout == other.timeout;
}

@end

@implementation LUPluginSettings

- (instancetype)initWithDictionary:(NSDictionary *)dict
{
    self = [super init];
    if (self) {
        _exceptionWarning = [[LUExceptionWarningSettings alloc] initWithDictionary:dict[@"exceptionWarning"]];
        _logOverlay = [[LULogOverlaySettings alloc] initWithDictionary:dict[@"logOverlay"]];
        _capacity = [dict[@"capacity"] intValue];
        _trim = [dict[@"trim"] intValue];
        _gesture = parseGesture(dict[@"gesture"]);
        _richTextTags = [dict[@"richTextTags"] boolValue];
        _sortActions = [dict[@"sortActions"] boolValue];
        _sortVariables = [dict[@"sortVariables"] boolValue];
        _emails = dict[@"emails"];
    }
    return self;
}

#pragma mark -
#pragma mark NSCoding

- (void)encodeWithCoder:(NSCoder *)coder
{
    [coder encodeObject:self.exceptionWarning forKey:@"exceptionWarning"];
    [coder encodeObject:self.logOverlay forKey:@"logOverlay"];
    [coder encodeInteger:self.capacity forKey:@"capacity"];
    [coder encodeInteger:self.trim forKey:@"trim"];
    [coder encodeObject:NSStringFromGesture(self.gesture) forKey:@"gesture"];
    [coder encodeBool:self.richTextTags forKey:@"richTextTags"];
    [coder encodeBool:self.sortActions forKey:@"sortActions"];
    [coder encodeBool:self.sortVariables forKey:@"sortVariables"];
    [coder encodeObject:self.emails forKey:@"emails"];
}

- (nullable instancetype)initWithCoder:(NSCoder *)decoder
{
    self = [super init];
    if (self) {
        _exceptionWarning = [decoder decodeObjectForKey:@"exceptionWarning"];
        _logOverlay = [decoder decodeObjectForKey:@"logOverlay"];
        _capacity = [decoder decodeIntegerForKey:@"capacity"];
        _trim = [decoder decodeIntegerForKey:@"trim"];
        _gesture = parseGesture([decoder decodeObjectForKey:@"gesture"]);
        _richTextTags = [decoder decodeBoolForKey:@"richTextTags"];
        _sortActions = [decoder decodeBoolForKey:@"sortActions"];
        _sortVariables = [decoder decodeBoolForKey:@"sortVariables"];
        _emails = [decoder decodeObjectForKey:@"emails"];
    }
    return self;
}

#pragma mark -
#pragma mark Equality

- (BOOL)isEqual:(id)object
{
    if (self == object) {
        return YES;
    }

    if (![object isKindOfClass:[self class]]) {
        return NO;
    }

    LUPluginSettings *other = object;
    return [self.logOverlay isEqual:other.logOverlay] &&
           [self.exceptionWarning isEqual:other.exceptionWarning] &&
           self.capacity == other.capacity &&
           self.trim == other.trim &&
           self.gesture == other.gesture &&
           self.richTextTags == other.richTextTags &&
           self.sortActions == other.sortActions &&
           self.sortVariables == other.sortVariables &&
           [self.emails isEqualToArray:other.emails];
}

@end
