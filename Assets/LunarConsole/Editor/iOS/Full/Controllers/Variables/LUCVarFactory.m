//
//  LUCVarFactory.m
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


#import "LUCVarFactory.h"

#import "Lunar-Full.h"

#import "LUCVarBooleanTableViewCell.h"
#import "LUCVarInputTableViewCell.h"
#import "LUCVarStringTableViewCell.h"

@implementation LUCVarFactory

+ (LUCVar *)variableWithId:(int)entryId name:(NSString *)name value:(NSString *)value defaultValue:(NSString *)defaultValue type:(LUCVarType)type
{
    Class cellClass = [self tableCellClassForVariableType:type];
    if (cellClass == NULL)
    {
        NSLog(@"Can't resolve cell class for variable type: %ld", (long) type);
        return nil;
    }
    
    return [LUCVar variableWithId:entryId
                             name:name
                            value:value
                     defaultValue:defaultValue
                             type:type
                        cellClass:cellClass];
}

+ (Class)tableCellClassForVariableType:(LUCVarType)type
{
    switch (type)
    {
        case LUCVarTypeBoolean: return [LUCVarBooleanTableViewCell class];
        case LUCVarTypeInteger: return [LUCVarInputTableViewCell class];
        case LUCVarTypeFloat:   return [LUCVarInputTableViewCell class];
        case LUCVarTypeString:  return [LUCVarStringTableViewCell class];
        case LUCVarTypeEnum:    return [LUCVarStringTableViewCell class];
        case LUCVarTypeUnknown: return NULL;
    }
}

@end
