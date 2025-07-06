//
//  LUActionRegistry.m
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

#import "LUActionRegistry.h"

@interface LUActionRegistry()
{
    LUSortedList * _actions;
    LUSortedList * _variables;
}
@end

@implementation LUActionRegistry

+ (instancetype)registry
{
    return [[self alloc] init];
}

- (instancetype)init
{
    self = [super init];
    if (self)
    {
        _actions = [LUSortedList new];
        _variables = [LUSortedList new];
    }
    return self;
}

#pragma mark -
#pragma mark Actions

- (LUAction *)registerActionWithId:(int)actionId name:(NSString *)actionName
{
    NSUInteger actionIndex = [self indexOfActionWithName:actionName];
    if (actionIndex == NSNotFound)
    {
        LUAction *action = [LUAction actionWithId:actionId name:actionName];
        actionIndex = [_actions addObject:action];
        [_delegate actionRegistry:self didAddAction:action atIndex:actionIndex];
    }
    
    return _actions[actionIndex];
}

- (BOOL)unregisterActionWithId:(int)actionId
{
    for (NSInteger actionIndex = _actions.count - 1; actionIndex >= 0; --actionIndex)
    {
        LUAction *action = _actions[actionIndex];
        if (action.actionId == actionId)
        {
            [_actions removeObjectAtIndex:actionIndex];
            [_delegate actionRegistry:self didRemoveAction:action atIndex:actionIndex];
            
            return YES;
        }
    }
    
    return NO;
}

- (NSUInteger)indexOfActionWithName:(NSString *)actionName
{
    // TODO: more optimized search
    for (NSUInteger index = 0; index < _actions.count; ++index)
    {
        LUAction *action = _actions[index];
        if ([action.name isEqualToString:actionName])
        {
            return index;
        }
    }
    
    return NSNotFound;
}

#pragma mark -
#pragma mark Variables

- (LUCVar *)registerVariableWithId:(int)variableId name:(NSString *)name typeName:(NSString *)typeName value:(NSString *)value defaultValue:(NSString *)defaultValue
{
    return [self registerVariableWithId:variableId name:name typeName:typeName value:value defaultValue:defaultValue values:nil];
}

- (LUCVar *)registerVariableWithId:(int)variableId name:(NSString *)name typeName:(NSString *)typeName value:(NSString *)value defaultValue:(NSString *)defaultValue values:(NSArray<NSString *> *)values
{
    LUCVarType type = [LUCVar typeForName:typeName];
    if (type == LUCVarTypeUnknown)
    {
        NSLog(@"Unknown variable type: %@", typeName);
        return nil;
    }
    
    LUCVar *variable = [LUCVarFactory variableWithId:variableId name:name value:value defaultValue:defaultValue type:type];
    variable.values = values;
    
    NSInteger index = [_variables addObject:variable];
    [_delegate actionRegistry:self didRegisterVariable:variable atIndex:index];
    
    return variable;
}

- (void)setValue:(NSString *)value forVariableWithId:(int)variableId
{
    NSUInteger index = [self indexOfVariableWithId:variableId];
    if (index != NSNotFound)
    {
        LUCVar *cvar = [_variables objectAtIndex:index];
        cvar.value = value;
        [_delegate actionRegistry:self didDidChangeVariable:cvar atIndex:index];
    }
    else
    {
        NSLog(@"Can't server cvar value: variable id %d not found", variableId);
    }
}

- (NSUInteger)indexOfVariableWithId:(int)variableId
{
    NSUInteger index = 0;
    for (LUCVar *cvar in _variables)
    {
        if (cvar.actionId == variableId)
        {
            return index;
        }
        
        ++index;
    }
    
    return NSNotFound;
}

- (LUCVar *)variableWithId:(int)variableId
{
    NSUInteger index = [self indexOfVariableWithId:variableId];
    return index != NSNotFound ? _variables[index] : nil;
}

#pragma mark -
#pragma mark Properties

- (NSArray *)actions
{
    return _actions.innerArray;
}

- (NSArray *)variables
{
    return _variables.innerArray;
}

#pragma mark -
#pragma mark Properties

- (BOOL)actionSortingEnabled
{
    return _actions.isSortingEnabled;
}

- (void)setActionSortingEnabled:(BOOL)actionSortingEnabled
{
    _actions.sortingEnabled = actionSortingEnabled;
}

- (BOOL)variableSortingEnabled
{
    return _variables.isSortingEnabled;
}

- (void)setVariableSortingEnabled:(BOOL)variableSortingEnabled
{
    _variables.sortingEnabled = variableSortingEnabled;
}

@end
