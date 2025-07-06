//
//  LUActionController.m
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

#import "LUActionController.h"
#import "LUActionControllerBase_Inheritance.h"

static const NSInteger kSectionIndexActions = 0;
static const NSInteger kSectionIndexVariables = 1;
static const NSInteger kSectionCount = 2;

@interface LUActionController () <UITableViewDataSource, UITableViewDelegate, UISearchBarDelegate, LUActionRegistryFilterDelegate>
{
    LUActionRegistryFilter * _actionRegistryFilter;
}

@end

@implementation LUActionController

- (instancetype)initWithActionRegistry:(LUActionRegistry *)actionRegistry
{
    self = [super initWithActionRegistry:actionRegistry];
    if (self)
    {
        if (actionRegistry == nil)
        {
            NSLog(@"Can't create action controller: action register is nil");
            
            self = nil;
            return nil;
        }
        
        [self registerNotifications];
        
        _actionRegistryFilter = [[LUActionRegistryFilter alloc] initWithActionRegistry:actionRegistry];
        _actionRegistryFilter.delegate = self;
    }
    return self;
}

- (void)dealloc
{
    [self unregisterNotifications];
}

- (void)viewDidLoad
{
    [super viewDidLoad];
    
    self.tableView.dataSource = self;
    self.tableView.delegate = self;
    self.filterBar.delegate = self;
    self.noActionsWarningLabel.text = @"No actions or variables";
}

#pragma mark -
#pragma mark Notifications

- (void)registerNotifications
{
    [LUNotificationCenter addObserver:self
                             selector:@selector(consoleControllerDidResizeNotification:)
                                 name:LUConsoleControllerDidResizeNotification
                               object:nil];
}

- (void)unregisterNotifications
{
    [LUNotificationCenter removeObserver:self];
}

- (void)consoleControllerDidResizeNotification:(NSNotification *)notification
{
    [self.tableView reloadRowsAtIndexPaths:self.tableView.indexPathsForVisibleRows withRowAnimation:UITableViewRowAnimationNone];
}

#pragma mark -
#pragma mark Filtering

- (void)filterByText:(NSString *)text
{
    BOOL changed = [_actionRegistryFilter setFilterText:text];
    if (changed)
    {
        [self.tableView reloadData];
    }
}

#pragma mark -
#pragma mark UITableViewDataSource

- (NSInteger)numberOfSectionsInTableView:(UITableView *)tableView
{
    return kSectionCount;
}

- (NSInteger)tableView:(UITableView *)tableView numberOfRowsInSection:(NSInteger)section
{
    if (section == kSectionIndexActions)
    {
        return _actionRegistryFilter.actions.count;
    }
    
    if (section == kSectionIndexVariables)
    {
        return _actionRegistryFilter.variables.count;
    }
    
    LUAssertMsgv(section < kSectionCount, @"Unexpected section index: %ld", (long) section);
    return 0;
}

- (NSString *)tableView:(UITableView *)tableView titleForHeaderInSection:(NSInteger)section
{
    if (section == kSectionIndexActions)
    {
        return [self filteredActionCount] > 0 ? @"Actions" : @"";
    }
    
    if (section == kSectionIndexVariables)
    {
        return [self filteredVariableCount] > 0 ? @"Variables" : @"";
    }
    
    LUAssertMsgv(section < kSectionCount, @"Unexpected section index: %ld", (long) section);
    return @"";
}

- (UITableViewCell *)tableView:(UITableView *)tableView cellForRowAtIndexPath:(NSIndexPath *)indexPath
{
    NSInteger section = indexPath.section;
    NSInteger index = indexPath.row;
    
    if (section == kSectionIndexActions)
    {
        return [self tableView:tableView actionCellForRowAtIndex:index];
    }
    
    if (section == kSectionIndexVariables)
    {
        return [self tableView:tableView variableCellForRowAtIndex:index];
    }
    
    LUAssertMsgv(section < kSectionCount, @"Unexpected section index: %ld", (long) section);
    return nil;
}

#pragma mark -
#pragma mark UITableViewDelegate

- (void)tableView:(UITableView *)tableView willDisplayHeaderView:(UIView *)view forSection:(NSInteger)section
{
    if ([view isKindOfClass:[UITableViewHeaderFooterView class]])
    {
        LUTheme *theme = [LUTheme mainTheme];
        
        UITableViewHeaderFooterView *headerView = (UITableViewHeaderFooterView *)view;
        headerView.textLabel.font = theme.actionsGroupFont;
        headerView.textLabel.textColor = theme.actionsGroupTextColor;
        headerView.contentView.backgroundColor = theme.actionsGroupBackgroundColor;
    }
}

- (void)tableView:(UITableView *)tableView didSelectRowAtIndexPath:(NSIndexPath *)indexPath
{
    [tableView deselectRowAtIndexPath:indexPath animated:NO];
    
    if (indexPath.section == kSectionIndexActions)
    {
        LUAction *action = [self actionAtIndex:indexPath.row];
        NSDictionary *userInfo = @{ LUActionControllerDidSelectActionKeyAction : action };
        [LUNotificationCenter postNotificationName:LUActionControllerDidSelectAction object:nil userInfo:userInfo];
    }
}

#pragma mark -
#pragma mark UISearchBarDelegate

- (BOOL)searchBarShouldBeginEditing:(UISearchBar *)searchBar
{
    [searchBar setShowsCancelButton:YES animated:YES];
    return YES;
}

- (BOOL)searchBarShouldEndEditing:(UISearchBar *)searchBar
{
    [searchBar setShowsCancelButton:NO animated:YES];
    return YES;
}

- (void)searchBarCancelButtonClicked:(UISearchBar *)searchBar
{
    [searchBar setShowsCancelButton:NO animated:YES];
    [searchBar resignFirstResponder];
}

- (void)searchBar:(UISearchBar *)searchBar textDidChange:(NSString *)searchText
{
    [self filterByText:searchText];
    
    if (searchText.length == 0)
    {
        dispatch_async(dispatch_get_main_queue(), ^{
            [searchBar resignFirstResponder];
        });
    }
}

- (void)searchBarSearchButtonClicked:(UISearchBar *)searchBar
{
    [searchBar resignFirstResponder];
}

#pragma mark -
#pragma mark LUActionRegistryFilterDelegate

- (void)actionRegistryFilter:(LUActionRegistryFilter *)registryFilter didAddAction:(LUAction *)action atIndex:(NSUInteger)index
{
    NSArray *array = [NSArray arrayWithObject:[NSIndexPath indexPathForRow:index inSection:kSectionIndexActions]];
    [self.tableView insertRowsAtIndexPaths:array withRowAnimation:UITableViewRowAnimationNone];
    
    [self updateNoActionWarningView];
}

- (void)actionRegistryFilter:(LUActionRegistryFilter *)registryFilter didRemoveAction:(LUAction *)action atIndex:(NSUInteger)index
{
    NSArray *array = [NSArray arrayWithObject:[NSIndexPath indexPathForRow:index inSection:kSectionIndexActions]];
    [self.tableView deleteRowsAtIndexPaths:array withRowAnimation:UITableViewRowAnimationNone];
    
    [self updateNoActionWarningView];
}

- (void)actionRegistryFilter:(LUActionRegistryFilter *)registry didRegisterVariable:(LUCVar *)variable atIndex:(NSUInteger)index
{
    NSArray *array = [NSArray arrayWithObject:[NSIndexPath indexPathForRow:index inSection:kSectionIndexVariables]];
    [self.tableView insertRowsAtIndexPaths:array withRowAnimation:UITableViewRowAnimationNone];
    
    [self updateNoActionWarningView];
}

- (void)actionRegistryFilter:(LUActionRegistryFilter *)registry didChangeVariable:(LUCVar *)variable atIndex:(NSUInteger)index
{
    NSArray *array = [NSArray arrayWithObject:[NSIndexPath indexPathForRow:index inSection:kSectionIndexVariables]];
    [self.tableView reloadRowsAtIndexPaths:array withRowAnimation:UITableViewRowAnimationNone];
}

#pragma mark -
#pragma mark Actions

- (UITableViewCell *)tableView:(UITableView *)tableView actionCellForRowAtIndex:(NSInteger)index
{
    LUAction *action = [self actionAtIndex:index];
    
    LUTheme *theme = [LUTheme mainTheme];
    
    LUActionTableViewCell *cell = [tableView dequeueReusableCellWithIdentifier:@"action"];
    if (cell == nil)
    {
        cell = [LUActionTableViewCell cellWithReuseIdentifier:@"action"];
    }
    cell.title = action.name;
    cell.cellColor = index % 2 == 0 ? theme.actionsBackgroundColorDark : theme.actionsBackgroundColorLight;
    
    return cell;
}

- (LUAction *)actionAtIndex:(NSInteger)index
{
    return _actionRegistryFilter.actions[index];
}

- (NSInteger)filteredActionCount
{
    return _actionRegistryFilter.actions.count;
}

- (NSInteger)actionCount
{
    return _actionRegistryFilter.registry.actions.count;
}

#pragma mark -
#pragma mark Variables

- (UITableViewCell *)tableView:(UITableView *)tableView variableCellForRowAtIndex:(NSInteger)index
{
    LUTheme *theme = [LUTheme mainTheme];
    
    LUCVar *cvar = [self variableAtIndex:index];
    LUCVarTableViewCell *cell = (LUCVarTableViewCell *)[cvar tableView:tableView cellAtIndex:index];
    cell.contentView.backgroundColor = index % 2 == 0 ? theme.actionsBackgroundColorDark : theme.actionsBackgroundColorLight;
    cell.presentingController = self;
    return cell;
}

- (LUCVar *)variableAtIndex:(NSInteger)index
{
    return _actionRegistryFilter.variables[index];
}

- (NSInteger)filteredVariableCount
{
    return _actionRegistryFilter.variables.count;
}

- (NSInteger)variableCount
{
    return _actionRegistryFilter.registry.variables.count;
}

#pragma mark -
#pragma mark No actions warning view

- (void)updateNoActionWarningView
{
    BOOL hasContent = [self actionCount] > 0 || [self variableCount] > 0;
	[self setNoActionsWarningViewHidden:hasContent];
}

- (void)setNoActionsWarningViewHidden:(BOOL)hidden
{
    self.actionsView.hidden = !hidden;
    self.noActionsWarningView.hidden = hidden;
}

@end
