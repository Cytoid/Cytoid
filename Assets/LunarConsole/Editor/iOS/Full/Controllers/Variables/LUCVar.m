//
//  LUCVar.m
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


#import "LUCVar.h"

#import "Lunar-Full.h"

NSString * const LUCVarTypeNameBoolean = @"Boolean";
NSString * const LUCVarTypeNameInteger = @"Integer";
NSString * const LUCVarTypeNameFloat   = @"Float";
NSString * const LUCVarTypeNameString  = @"String";
NSString * const LUCVarTypeNameEnum    = @"Enum";
NSString * const LUCVarTypeNameUnknown = @"Unknown";

@interface LUCVar ()
{
    Class _cellClass;
}

@end

@implementation LUCVar

+ (instancetype)variableWithId:(int)entryId
                          name:(NSString *)name
                         value:(NSString *)value
                  defaultValue:(NSString *)defaultValue
                          type:(LUCVarType)type
                     cellClass:(Class)cellClass
{
    return [[self alloc] initWithId:entryId name:name value:value defaultValue:defaultValue type:type cellClass:cellClass];
}

- (instancetype)initWithId:(int)entryId name:(NSString *)name
                     value:(NSString *)value
              defaultValue:(NSString *)defaultValue
                      type:(LUCVarType)type
                 cellClass:(Class)cellClass
{
    self = [super initWithId:entryId name:name];
    if (self)
    {
        _value = value;
        _defaultValue = defaultValue;
        _cellClass = cellClass;
        _type = type;
        _range = LUMakeCVarRange(NAN, NAN);
    }
    return self;
}

#pragma mark -
#pragma mark Default value

- (void)resetToDefaultValue
{
    _value = _defaultValue;
}

#pragma mark -
#pragma mark Flags

- (BOOL)hasFlag:(LUCVarFlags)flag
{
    return self.flags & flag;
}

#pragma mark -
#pragma mark UITableView

- (UITableViewCell *)tableView:(UITableView *)tableView cellAtIndex:(NSUInteger)index
{
    NSString *identifier = NSStringFromClass(_cellClass);
    LUCVarTableViewCell *cell = (LUCVarTableViewCell *)[tableView dequeueReusableCellWithIdentifier:identifier];
    if (cell == nil)
    {
        cell = [[_cellClass alloc] initWithReuseIdentifier:identifier];
    }
    
    [cell setupVariable:self];
    
    return cell;
}

#pragma mark -
#pragma mark Lookup 

+ (LUCVarType)typeForName:(NSString *)type
{
    if ([type isEqualToString:LUCVarTypeNameBoolean]) return LUCVarTypeBoolean;
    if ([type isEqualToString:LUCVarTypeNameInteger]) return LUCVarTypeInteger;
    if ([type isEqualToString:LUCVarTypeNameFloat])   return LUCVarTypeFloat;
    if ([type isEqualToString:LUCVarTypeNameString])  return LUCVarTypeString;
    if ([type isEqualToString:LUCVarTypeNameEnum])    return LUCVarTypeEnum;
    
    return LUCVarTypeUnknown;
}

+ (NSString *)typeNameForType:(LUCVarType)type
{
    switch (type)
    {
        case LUCVarTypeBoolean: return LUCVarTypeNameBoolean;
        case LUCVarTypeInteger: return LUCVarTypeNameInteger;
        case LUCVarTypeFloat:   return LUCVarTypeNameFloat;
        case LUCVarTypeString:  return LUCVarTypeNameString;
        case LUCVarTypeEnum:    return LUCVarTypeNameEnum;
        case LUCVarTypeUnknown: return LUCVarTypeNameUnknown;
    }
}

#pragma mark -
#pragma mark Properties

- (BOOL)isDefaultValue
{
    return [_value isEqualToString:_defaultValue];
}

- (BOOL)hasRange
{
    return !isnan(_range.min) && !isnan(_range.max);
}

@end
