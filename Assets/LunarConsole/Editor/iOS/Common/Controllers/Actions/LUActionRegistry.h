//
//  LUActionRegistry.h
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

@class LUAction;
@class LUActionRegistry;
@class LUCVar;

@protocol LUActionRegistryDelegate <NSObject>

- (void)actionRegistry:(LUActionRegistry *)registry didAddAction:(LUAction *)action atIndex:(NSUInteger)index;
- (void)actionRegistry:(LUActionRegistry *)registry didRemoveAction:(LUAction *)action atIndex:(NSUInteger)index;
- (void)actionRegistry:(LUActionRegistry *)registry didRegisterVariable:(LUCVar *)variable atIndex:(NSUInteger)index;
- (void)actionRegistry:(LUActionRegistry *)registry didDidChangeVariable:(LUCVar *)variable atIndex:(NSUInteger)index;

@end

@interface LUActionRegistry : NSObject

@property (nonatomic, readonly) NSArray *actions;
@property (nonatomic, readonly) NSArray *variables;

@property (nonatomic, weak) id<LUActionRegistryDelegate> delegate;

@property (nonatomic, assign) BOOL actionSortingEnabled;
@property (nonatomic, assign) BOOL variableSortingEnabled;

+ (instancetype)registry;

- (LUAction *)registerActionWithId:(int)actionId name:(NSString *)name;
- (BOOL)unregisterActionWithId:(int)actionId;

- (LUCVar *)registerVariableWithId:(int)variableId name:(NSString *)name typeName:(NSString *)type value:(NSString *)value defaultValue:(NSString *)defaultValue;
- (LUCVar *)registerVariableWithId:(int)variableId name:(NSString *)name typeName:(NSString *)type value:(NSString *)value defaultValue:(NSString *)defaultValue values:(NSArray<NSString *> *)values;
- (void)setValue:(NSString *)value forVariableWithId:(int)variableId;
- (LUCVar *)variableWithId:(int)variableId;

@end
