//
//  LUEnumPickerViewController.m
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


#import "LUEnumPickerViewController.h"
#import "LUTheme.h"

@interface LUEnumPickerViewController () <UITableViewDataSource, UITableViewDelegate>

@property (nonatomic, weak) IBOutlet UITableView *tableView;

@property (nonatomic, strong) NSArray<NSString *> *values;
@property (nonatomic, assign) NSUInteger selectedIndex;

@end

@implementation LUEnumPickerViewController

- (instancetype)initWithValues:(NSArray<NSString *> *)values initialIndex:(NSUInteger)index
{
    self = [super initWithNibName:NSStringFromClass([self class]) bundle:nil];
    if (self) {
        if (index < 0 || index >= values.count) {
            return nil;
        }
        _values = values;
        _selectedIndex = index;
    }

    return self;
}

- (void)viewDidLoad
{
	[super viewDidLoad];
	
	self.tableView.dataSource = self;
	self.tableView.delegate = self;
	self.tableView.backgroundColor = [LUTheme mainTheme].tableColor;
}

- (CGSize)preferredPopupSize
{
    CGFloat rowHeight = 44.0;
    CGFloat height = MIN(self.values.count * rowHeight, 320);
    return CGSizeMake(0, height);
}

#pragma mark -
#pragma mark UITableViewDataSource

- (NSInteger)tableView:(UITableView *)tableView numberOfRowsInSection:(NSInteger)section
{
    return _values.count;
}

- (UITableViewCell *)tableView:(UITableView *)tableView cellForRowAtIndexPath:(NSIndexPath *)indexPath
{
    static NSString *const kCellIdentifier = @"CellIdentifier";

    UITableViewCell *cell = [tableView dequeueReusableCellWithIdentifier:kCellIdentifier];
    if (cell == nil) {
        cell = [[UITableViewCell alloc] initWithStyle:UITableViewCellStyleDefault reuseIdentifier:kCellIdentifier];
    }

    NSUInteger index = indexPath.row;
    cell.textLabel.text = _values[index];
    if (index == _selectedIndex) {
        cell.accessoryType = UITableViewCellAccessoryCheckmark;
    }
	
	LUTheme *theme = [LUTheme mainTheme];
	UIColor *backgroundColor = index % 2 == 0 ? theme.actionsBackgroundColorDark : theme.actionsBackgroundColorLight;
	cell.contentView.superview.backgroundColor = backgroundColor;
	cell.textLabel.font = theme.actionsFont;
	cell.textLabel.textColor = theme.actionsTextColor;
	cell.selectedBackgroundView.backgroundColor = [UIColor blackColor];

    return cell;
}

#pragma mark -
#pragma mark UITableViewDelegate

- (void)tableView:(UITableView *)tableView didSelectRowAtIndexPath:(NSIndexPath *)indexPath
{
    NSInteger index = indexPath.row;
    if (index != _selectedIndex) {
        [tableView cellForRowAtIndexPath:indexPath].accessoryType = UITableViewCellAccessoryCheckmark;
        for (NSIndexPath *path in [tableView indexPathsForVisibleRows]) {
            if (path.row == _selectedIndex) {
                [tableView cellForRowAtIndexPath:path].accessoryType = UITableViewCellAccessoryNone;
                break;
            }
        }
        _selectedIndex = index;
    }
    [tableView deselectRowAtIndexPath:indexPath animated:YES];
}

#pragma mark -
#pragma mark Properties

- (NSString *)selectedValue
{
    return _values[_selectedIndex];
}

@end
