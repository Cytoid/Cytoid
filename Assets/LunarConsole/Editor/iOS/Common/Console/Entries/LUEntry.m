//
//  LUEntry.m
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


#import "LUEntry.h"

#import "Lunar.h"

@implementation LUEntry

- (instancetype)initWithId:(int)actionId name:(NSString *)name
{
    self = [super init];
    if (self)
    {
        if (name.length == 0)
        {
            NSLog(@"Can't create an entry: name is nil or empty");
            self = nil;
            return nil;
        }
        
        _actionId = actionId;
        _name = name;
    }
    return self;
}


#pragma mark -
#pragma mark NSComparisonMethods

- (NSComparisonResult)compare:(LUEntry *)other
{
    return [_name compare:other.name];
}

#pragma mark -
#pragma mark Equality

- (BOOL)isEqual:(id)object
{
    if ([object isKindOfClass:[LUEntry class]])
    {
        LUEntry *entry = object;
        return self.actionId == entry.actionId && [self.name isEqualToString:entry.name];
    }
    
    return NO;
}

#pragma mark -
#pragma mark Description

- (NSString *)description
{
    return [NSString stringWithFormat:@"%d: %@", self.actionId, self.name];
}

#pragma mark -
#pragma mark UITableView

- (UITableViewCell *)tableView:(UITableView *)tableView cellAtIndex:(NSUInteger)index
{
    LU_SHOULD_IMPLEMENT_METHOD
    return nil;
}

- (CGSize)cellSizeForTableView:(UITableView *)tableView
{
    LU_SHOULD_IMPLEMENT_METHOD
    return CGSizeMake(CGRectGetWidth(tableView.bounds), 44);
}

@end
