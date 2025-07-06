//
//  LUSerializableObject.m
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


#import "LUSerializableObject.h"

#import "Lunar.h"

@interface LUSerializableObject () <NSCoding>
{
    NSString * _filename;
}

@end

@implementation LUSerializableObject

+ (instancetype)loadFromFile:(NSString *)filename
{
    return [self loadFromFile:filename initDefault:YES];
}

+ (instancetype)loadFromFile:(NSString *)filename initDefault:(BOOL)initDefault
{
    id object = LUDeserializeObject(filename);
    if (object != nil)
    {
        [object setFilename:filename];
        return object;
    }
    
    return initDefault ? [[self alloc] initWithFilename:filename] : nil;
}

- (instancetype)initWithFilename:(NSString *)filename
{
    self = [super init];
    if (self)
    {
        _filename = filename;
        [self initDefaults];
    }
    return self;
}

#pragma mark -
#pragma mark NSCoding

- (instancetype)initWithCoder:(NSCoder *)aDecoder
{
    self = [super init];
    if (self)
    {
        [self initDefaults];
        
        NSInteger version = [aDecoder decodeIntegerForKey:@"version"];
        if (version == [[self class] version])
        {
            [self deserializeWithDecoder:aDecoder];
        }
    }
    return self;
}

- (void)encodeWithCoder:(NSCoder *)aCoder
{
    [aCoder encodeInteger:[[self class] version] forKey:@"version"];
    [self serializeWithCoder:aCoder];
}

#pragma mark -
#pragma mark Save

- (BOOL)save
{
    return LUSerializeObject(self, _filename);
}

#pragma mark -
#pragma mark Inheritance

- (void)initDefaults
{
}

- (void)serializeWithCoder:(NSCoder *)coder
{
}

- (void)deserializeWithDecoder:(NSCoder *)decoder
{
}

#pragma mark -
#pragma mark Getters/Setters

- (void)setFilename:(NSString *)filename
{
    _filename = filename;
}

@end
