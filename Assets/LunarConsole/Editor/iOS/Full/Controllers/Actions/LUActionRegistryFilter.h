//
//  LUActionRegistryFilter.h
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

#import "LUActionRegistry.h"

@class LUActionRegistryFilter;

@protocol LUActionRegistryFilterDelegate <NSObject>

- (void)actionRegistryFilter:(LUActionRegistryFilter *)registryFilter didAddAction:(LUAction *)action atIndex:(NSUInteger)index;
- (void)actionRegistryFilter:(LUActionRegistryFilter *)registryFilter didRemoveAction:(LUAction *)action atIndex:(NSUInteger)index;
- (void)actionRegistryFilter:(LUActionRegistryFilter *)registry didRegisterVariable:(LUCVar *)variable atIndex:(NSUInteger)index;
- (void)actionRegistryFilter:(LUActionRegistryFilter *)registry didChangeVariable:(LUCVar *)variable atIndex:(NSUInteger)index;

@end

@interface LUActionRegistryFilter : NSObject

@property (nonatomic, readonly) LUActionRegistry *registry;
@property (nonatomic, readonly) BOOL isFiltering;
@property (nonatomic, weak) id<LUActionRegistryFilterDelegate> delegate;
@property (nonatomic, readonly) NSArray *actions;
@property (nonatomic, readonly) NSArray *variables;

+ (instancetype)filterWithActionRegistry:(LUActionRegistry *)actionRegistry;
- (instancetype)initWithActionRegistry:(LUActionRegistry *)actionRegistry;

- (BOOL)setFilterText:(NSString *)filterText;

@end
