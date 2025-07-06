//
//  LUConsoleLogController.m
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


#import <MessageUI/MessageUI.h>

#import "LUConsoleLogController.h"

#import "Lunar.h"

static const CGFloat kMinWidthToResizeSearchBar = 480;

@interface LUConsoleLogController () <LunarConsoleDelegate, LUToggleButtonDelegate,
                                      UITableViewDataSource, UITableViewDelegate,
                                      UISearchBarDelegate,
                                      MFMailComposeViewControllerDelegate,
                                      LUTableViewTouchDelegate,
                                      LUConsoleLogMenuControllerDelegate,
                                      LUConsolePopupControllerDelegate> {
    LU_WEAK LUConsolePlugin *_plugin;
}

@property (weak, nonatomic, readonly) LUConsole *console;

@property (nonatomic, weak) IBOutlet UILabel *statusBar;
@property (nonatomic, weak) IBOutlet UILabel *overflowWarningLabel;

@property (nonatomic, weak) IBOutlet LUTableView *tableView;
@property (nonatomic, weak) IBOutlet UISearchBar *filterBar;
@property (nonatomic, weak) IBOutlet UIView *controlButtonsView;

@property (nonatomic, weak) IBOutlet LUConsoleLogTypeButton *logButton;
@property (nonatomic, weak) IBOutlet LUConsoleLogTypeButton *warningButton;
@property (nonatomic, weak) IBOutlet LUConsoleLogTypeButton *errorButton;

@property (nonatomic, weak) IBOutlet LUToggleButton *scrollLockButton;

@property (nonatomic, assign) BOOL scrollLocked;

@end

@implementation LUConsoleLogController

+ (instancetype)controllerWithPlugin:(LUConsolePlugin *)plugin
{
    return [[[self class] alloc] initWithPlugin:plugin];
}

- (instancetype)initWithPlugin:(LUConsolePlugin *)plugin
{
    self = [super initWithNibName:NSStringFromClass([self class]) bundle:nil];
    if (self) {
        _plugin = plugin; // weak variable: no retain here
        [self registerNotifications];
    }
    return self;
}

- (void)dealloc
{
    [self unregisterNotifications];

    if (self.console.delegate == self) {
        self.console.delegate = nil;
    }
}

- (void)viewDidLoad
{
    [super viewDidLoad];

    self.console.delegate = self;

    // scroll lock
    self.scrollLocked = YES; // scroll is locked by default
    self.scrollLockButton.on = self.scrollLocked;
    self.scrollLockButton.delegate = self;

    LUTheme *theme = [self theme];

    // title
    self.title = @"Logs";

    // background
    self.view.opaque = YES;
    self.view.backgroundColor = theme.tableColor;

    // table view
    self.tableView.dataSource = self;
    self.tableView.delegate = self;
    self.tableView.touchDelegate = self;
    self.tableView.backgroundColor = theme.tableColor;

    // "status bar" view
    UITapGestureRecognizer *statusBarTapGestureRecognizer = [[UITapGestureRecognizer alloc] initWithTarget:self
                                                                                                    action:@selector(onStatusBarTap:)];
    [self.statusBar addGestureRecognizer:statusBarTapGestureRecognizer];

    self.statusBar.backgroundColor = theme.statusBarColor;
    self.statusBar.textColor = theme.statusBarTextColor;
    self.statusBar.text = [NSString stringWithFormat:@"Lunar Console v%@", _version ? _version : @"?.?.?"];

    // log type buttons
    self.logButton.on = ![self.console.entries isFilterLogTypeEnabled:LUConsoleLogTypeLog];
    self.logButton.delegate = self;

    self.warningButton.on = ![self.console.entries isFilterLogTypeEnabled:LUConsoleLogTypeWarning];
    self.warningButton.delegate = self;

    self.errorButton.on = ![self.console.entries isFilterLogTypeEnabled:LUConsoleLogTypeError];
    self.errorButton.delegate = self;

    // control buttons
    self.controlButtonsView.backgroundColor = theme.tableColor;

    // filter text
    self.filterBar.text = self.console.entries.filterText;
    self.filterBar.delegate = self;

    // log entries count
    [self updateEntriesCount];

    // overflow warning
    dispatch_async(dispatch_get_main_queue(), ^{
        // give the table a chance to layout
        [self updateOverflowWarning];

        // scroll to the end
        if (self.scrollLocked) {
            [self scrollToBottomAnimated:NO];
        }
    });
}

- (void)viewWillAppear:(BOOL)animated
{
    [super viewWillAppear:animated];
    [self.navigationController setNavigationBarHidden:YES animated:animated];
}

- (void)viewWillDisappear:(BOOL)animated
{
    [super viewWillDisappear:animated];
    [self.navigationController setNavigationBarHidden:NO animated:animated];
}

- (void)didReceiveMemoryWarning
{
    [super didReceiveMemoryWarning];

    // TODO: clean up cells
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
    [_tableView reloadRowsAtIndexPaths:_tableView.indexPathsForVisibleRows withRowAnimation:UITableViewRowAnimationNone];
}

#pragma mark -
#pragma mark Filtering

- (void)filterByText:(NSString *)text
{
    BOOL shouldReload = [self.console.entries setFilterByText:text];
    if (shouldReload) {
        [self reloadData];
    }
}

- (void)setFilterByLogTypeMask:(LUConsoleLogTypeMask)logTypeMask disabled:(BOOL)disabled
{
    BOOL shouldReload = [self.console.entries setFilterByLogTypeMask:logTypeMask disabled:disabled];
    if (shouldReload) {
        [self reloadData];
    }
}

#pragma mark -
#pragma mark Collapsing

- (void)setCollapsed:(BOOL)collapsed
{
    self.console.collapsed = !self.console.isCollapsed;
    [self reloadData];
}

#pragma mark -
#pragma mark Actions

- (IBAction)onClear:(id)sender
{
    // clear entries
    [self.console clear];

    // update entries count
    [self updateEntriesCount];
}

- (IBAction)onCopy:(id)sender
{
    NSString *text = [self.console getText];
    [self copyTextToClipboard:text];
}

- (IBAction)onEmail:(id)sender
{
    if (![MFMailComposeViewController canSendMail]) {
        LUDisplayAlertView(@"Lunar Mobile Console", @"Log email cannot be sent.\nMake sure your device is set up for sending email.");
        return;
    }

    NSString *bundleName = [[NSBundle mainBundle].infoDictionary objectForKey:@"CFBundleName"];
    NSString *text = [self.console getText];

    MFMailComposeViewController *controller = [[MFMailComposeViewController alloc] init];
    [controller setMailComposeDelegate:self];
    [controller setSubject:[NSString stringWithFormat:@"%@ console log", bundleName]];
    [controller setMessageBody:text isHTML:NO];
    if (_emails.count > 0) {
        [controller setToRecipients:_emails];
    }
    if (controller) {
        [self presentViewController:controller animated:YES completion:nil];
    }
}

- (IBAction)onSettings:(id)sender
{
    LUConsoleSettingsController *controller = [[LUConsoleSettingsController alloc] initWithSettings:_plugin.settings];
    LUConsolePopupController *popupController = [[LUConsolePopupController alloc] initWithContentController:controller];
    popupController.popupDelegate = self;
    [popupController presentFromController:self.parentViewController animated:YES];
}

- (void)onStatusBarTap:(UITapGestureRecognizer *)recognizer
{
    _scrollLockButton.on = NO;
    [self scrollToTopAnimated:YES];
}

- (IBAction)onMoreButton:(id)sender
{
    LUConsoleLogMenuController *controller = [LUConsoleLogMenuController new];

    // toggle collapse button
    if (self.console.isCollapsed) {
        [controller addButtonTitle:@"Expand" target:self action:@selector(onExpandButton:)];
    } else {
        [controller addButtonTitle:@"Collapse" target:self action:@selector(onCollapseButton:)];
    }

    // resize button
    [controller addButtonTitle:@"Move/Resize" target:self action:@selector(onResizeButton:)];

    // help
    [controller addButtonTitle:@"Help" target:self action:@selector(onHelpButton:)];

    // PRO version
    if (LUConsoleIsFreeVersion) {
        LUConsoleLogMenuControllerButton *button = [controller addButtonTitle:@"Get PRO Version" target:self action:@selector(onGetProButton:)];
        button.textColor = [LUTheme mainTheme].contextMenuTextProColor;
        button.textHighlightedColor = [LUTheme mainTheme].contextMenuTextProHighlightColor;
    }

    [controller setDelegate:self];

    // add as child view controller
    [self addChildOverlayController:controller animated:NO];
}

#pragma mark -
#pragma mark LunarConsoleDelegate

- (void)lunarConsole:(LUConsole *)console didAddEntryAtIndex:(NSInteger)index trimmedCount:(NSUInteger)trimmedCount
{
    if (trimmedCount > 0) {
        // show warning
        [self showOverflowCount:console.trimmedCount];

        // update cells
        [_tableView beginUpdates];

        [self removeCellsCount:trimmedCount];

        if (index != -1) {
            [self insertCellAt:index];
        }

        [_tableView endUpdates];
    } else if (index != -1) {
        [self insertCellAt:index];
    }

    // update entries count
    [self updateEntriesCount];
}

- (void)lunarConsole:(LUConsole *)console didUpdateEntryAtIndex:(NSInteger)index trimmedCount:(NSUInteger)trimmedCount
{
    if (trimmedCount > 0) {
        // show warning
        [self showOverflowCount:console.trimmedCount];

        // update cells
        [_tableView beginUpdates];

        [self removeCellsCount:trimmedCount];

        if (index != -1) {
            [self reloadCellAt:index];
        }

        [_tableView endUpdates];
    } else if (index != -1) {
        [self reloadCellAt:index];
    }

    // update entries count
    [self updateEntriesCount];
}

- (void)lunarConsoleDidClearEntries:(LUConsole *)console
{
    [self reloadData];
    [self updateOverflowWarning];
}

#pragma mark -
#pragma mark LUToggleButtonDelegate

- (void)toggleButtonStateChanged:(LUToggleButton *)button
{
    if (button == _scrollLockButton) {
        self.scrollLocked = button.isOn;
    } else {
        LUConsoleLogTypeMask mask = 0;
        if (button == _logButton) {
            mask |= LU_CONSOLE_LOG_TYPE_MASK(LUConsoleLogTypeLog);
        } else if (button == _warningButton) {
            mask |= LU_CONSOLE_LOG_TYPE_MASK(LUConsoleLogTypeWarning);
        } else if (button == _errorButton) {
            mask |= LU_CONSOLE_LOG_TYPE_MASK(LUConsoleLogTypeException) |
                    LU_CONSOLE_LOG_TYPE_MASK(LUConsoleLogTypeError) |
                    LU_CONSOLE_LOG_TYPE_MASK(LUConsoleLogTypeAssert);
        }

        [self setFilterByLogTypeMask:mask disabled:button.isOn];
    }
}

#pragma mark -
#pragma mark Scrolling

- (void)scrollToBottomAnimated:(BOOL)animated
{
    if (self.console.entriesCount > 0) {
        NSIndexPath *path = [NSIndexPath indexPathForRow:self.console.entriesCount - 1 inSection:0];
        [_tableView scrollToRowAtIndexPath:path atScrollPosition:UITableViewScrollPositionBottom animated:animated];
    }
}

- (void)scrollToTopAnimated:(BOOL)animated
{
    if (self.console.entriesCount > 0) {
        NSIndexPath *path = [NSIndexPath indexPathForRow:0 inSection:0];
        [_tableView scrollToRowAtIndexPath:path atScrollPosition:UITableViewScrollPositionBottom animated:animated];
    }
}

#pragma mark -
#pragma mark UITableViewDataSource

- (NSInteger)tableView:(UITableView *)tableView numberOfRowsInSection:(NSInteger)section
{
    return self.console.entriesCount;
}

- (UITableViewCell *)tableView:(UITableView *)tableView cellForRowAtIndexPath:(NSIndexPath *)indexPath
{
    LUConsoleLogEntry *entry = [self entryForRowAtIndexPath:indexPath];
    return [entry tableView:tableView cellAtIndex:indexPath.row];
}

#pragma mark -
#pragma mark UITableViewDelegate

- (void)tableView:(UITableView *)tableView didSelectRowAtIndexPath:(NSIndexPath *)indexPath
{
    LUConsoleLogEntry *entry = [self entryForRowAtIndexPath:indexPath];

    LUConsoleLogDetailsController *controller = [[LUConsoleLogDetailsController alloc] initWithEntry:entry];
    LUConsolePopupController *popupController = [[LUConsolePopupController alloc] initWithContentController:controller];
    popupController.popupDelegate = self;
    [popupController presentFromController:self.parentViewController animated:YES];

    [tableView deselectRowAtIndexPath:indexPath animated:YES];
}

- (CGFloat)tableView:(UITableView *)tableView heightForRowAtIndexPath:(NSIndexPath *)indexPath
{
    LUConsoleLogEntry *entry = [self entryForRowAtIndexPath:indexPath];
    return [entry cellSizeForTableView:tableView].height;
}

- (BOOL)tableView:(UITableView *)tableView shouldShowMenuForRowAtIndexPath:(NSIndexPath *)indexPath
{
    return YES;
}

- (BOOL)tableView:(UITableView *)tableView canPerformAction:(SEL)action forRowAtIndexPath:(NSIndexPath *)indexPath withSender:(id)sender
{
    return action == @selector(copy:);
}

- (void)tableView:(UITableView *)tableView performAction:(SEL)action forRowAtIndexPath:(NSIndexPath *)indexPath withSender:(id)sender
{
    LUConsoleLogEntry *entry = [self entryForRowAtIndexPath:indexPath];
    [self copyTextToClipboard:entry.message.text];
}

#pragma mark -
#pragma mark UISearchBarDelegate

- (BOOL)searchBarShouldBeginEditing:(UISearchBar *)searchBar
{
    [searchBar setShowsCancelButton:YES animated:YES];

    if (CGRectGetWidth(self.view.bounds) < kMinWidthToResizeSearchBar) {
        [self setLogButtonsVisible:NO animated:YES];
    }

    return YES;
}

- (BOOL)searchBarShouldEndEditing:(UISearchBar *)searchBar
{
    [searchBar setShowsCancelButton:NO animated:YES];

    if (CGRectGetWidth(self.view.bounds) < kMinWidthToResizeSearchBar) {
        [self setLogButtonsVisible:YES animated:YES];
    }

    return YES;
}

- (void)setLogButtonsVisible:(BOOL)visible animated:(BOOL)animated
{
    if (animated) {
        [UIView animateWithDuration:0.4
                         animations:^{
                             [self setLogButtonsVisible:visible];
                         }];
    } else {
        [self setLogButtonsVisible:visible];
    }
}

- (void)setLogButtonsVisible:(BOOL)visible
{
    self.logButton.hidden = self.errorButton.hidden = self.warningButton.hidden = !visible;
}

- (void)searchBarCancelButtonClicked:(UISearchBar *)searchBar
{
    [searchBar setShowsCancelButton:NO animated:YES];
    [searchBar resignFirstResponder];
}

- (void)searchBar:(UISearchBar *)searchBar textDidChange:(NSString *)searchText
{
    [self filterByText:searchText];

    if (searchText.length == 0) {
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
#pragma mark MFMailComposeViewControllerDelegate

- (void)mailComposeController:(MFMailComposeViewController *)controller didFinishWithResult:(MFMailComposeResult)result error:(nullable NSError *)error
{
    if (error != nil) {
        LUDisplayAlertView(@"Lunar Mobile Console", [NSString stringWithFormat:@"Log was not sent: %@", error]);
    } else if (result != MFMailComposeResultSent) {
        LUDisplayAlertView(@"Lunar Mobile Console", @"Log was not sent");
    }

    [controller dismissViewControllerAnimated:YES completion:nil];
}

#pragma mark -
#pragma mark LUTableViewTouchDelegate

- (void)tableView:(LUTableView *)tableView touchesBegan:(NSSet *)touches withEvent:(UIEvent *)event
{
    _scrollLockButton.on = NO;
}

#pragma mark -
#pragma mark LUConsoleLogMenuControllerDelegate

- (void)menuControllerDidRequestClose:(LUConsoleLogMenuController *)controller
{
    [self removeChildOverlayController:controller animated:NO];
}

#pragma mark -
#pragma mark LUConsolePopupControllerDelegate

- (void)popupControllerDidDismiss:(LUConsolePopupController *)controller
{
    [controller dismissAnimated:YES];
}

#pragma mark -
#pragma mark UIScrollViewDelegate

- (void)scrollViewWillBeginDragging:(UIScrollView *)scrollView
{
    _scrollLockButton.on = NO;
}

#pragma mark -
#pragma mark Actions

- (void)onCollapseButton:(id)sender
{
    [self setCollapsed:YES];
}

- (void)onExpandButton:(id)sender
{
    [self setCollapsed:NO];
}

- (void)onResizeButton:(id)sender
{
    if ([_resizeDelegate respondsToSelector:@selector(consoleLogControllerDidRequestResize:)]) {
        [_resizeDelegate consoleLogControllerDidRequestResize:self];
    }
}

- (void)onHelpButton:(id)sender
{
    [[UIApplication sharedApplication] openURL:[NSURL URLWithString:@"https://goo.gl/5Z8ovV"]];
}

- (void)onLearnAboutProButton:(id)sender
{
    [[NSNotificationCenter defaultCenter] postNotificationName:LUConsoleCheckFullVersionNotification
                                                        object:nil
                                                      userInfo:@{ LUConsoleCheckFullVersionNotificationSource : @"settings" }];

    [[UIApplication sharedApplication] openURL:[NSURL URLWithString:@"https://goo.gl/TMnxBe"]];
}

- (void)onGetProButton:(id)sender
{
    [[NSNotificationCenter defaultCenter] postNotificationName:LUConsoleCheckFullVersionNotification
                                                        object:nil
                                                      userInfo:@{ LUConsoleCheckFullVersionNotificationSource : @"menu" }];

    [[UIApplication sharedApplication] openURL:[NSURL URLWithString:@"https://goo.gl/TMnxBe"]];
}

#pragma mark -
#pragma mark Overflow

- (void)updateOverflowWarning
{
    NSUInteger trimmedCount = self.console.trimmedCount;
    if (trimmedCount > 0) {
        [self showOverflowCount:trimmedCount];
    } else {
        [self hideOverflowCount];
    }
}

- (void)showOverflowCount:(NSUInteger)count
{
    NSString *text = count > 999 ? @"Too much output: 999+ items trimmed" : [NSString stringWithFormat:@"Too much output: %d item(s) trimmed", (int)count];

    self.overflowWarningLabel.text = text;
    self.overflowWarningLabel.hidden = NO;
}

- (void)hideOverflowCount
{
    self.overflowWarningLabel.hidden = YES;
}

#pragma mark -
#pragma mark Cell manipulations

- (void)removeCellsCount:(NSInteger)count
{
    if (count == 1) {
        NSArray *deleteIndices = [[NSArray alloc] initWithObjects:[NSIndexPath indexPathForRow:0 inSection:0], nil];
        [_tableView deleteRowsAtIndexPaths:deleteIndices withRowAnimation:UITableViewRowAnimationNone];
    } else if (count > 1) {
        NSMutableArray *deleteIndices = [[NSMutableArray alloc] initWithCapacity:count];
        for (NSInteger rowIndex = 0; rowIndex < count; ++rowIndex) {
            [deleteIndices addObject:[NSIndexPath indexPathForRow:rowIndex inSection:0]];
        }
        [_tableView deleteRowsAtIndexPaths:deleteIndices withRowAnimation:UITableViewRowAnimationNone];
    }
}

- (void)insertCellAt:(NSInteger)index
{
    LUAssert(index >= 0 && index < self.console.entriesCount);

    NSArray *indices = [[NSArray alloc] initWithObjects:[NSIndexPath indexPathForRow:index inSection:0], nil];
    [_tableView insertRowsAtIndexPaths:indices withRowAnimation:UITableViewRowAnimationNone];

    // scroll to the end
    if (_scrollLocked) {
        [self scrollToBottomAnimated:NO];
    }
}

- (void)reloadCellAt:(NSInteger)index
{
    LUAssert(index >= 0 && index < self.console.entriesCount);

    NSArray *indices = [[NSArray alloc] initWithObjects:[NSIndexPath indexPathForRow:index inSection:0], nil];
    [_tableView reloadRowsAtIndexPaths:indices withRowAnimation:UITableViewRowAnimationNone];
}

- (void)updateEntriesCount
{
    LUConsoleLogEntryList *entries = self.console.entries;
    _logButton.count = entries.logCount;
    _warningButton.count = entries.warningCount;
    _errorButton.count = entries.errorCount;
}

#pragma mark -
#pragma mark Helpers

- (LUConsoleLogEntry *)entryForRowAtIndexPath:(NSIndexPath *)indexPath
{
    return [self.console entryAtIndex:indexPath.row];
}

- (LUConsoleLogEntry *)entryForRowAtIndex:(NSUInteger)index
{
    return [self.console entryAtIndex:index];
}

- (void)reloadData
{
    [_tableView reloadData];
}

- (void)copyTextToClipboard:(NSString *)text
{
    UIPasteboard *pasteboard = [UIPasteboard generalPasteboard];
    [pasteboard setString:text];
}

#pragma mark -
#pragma mark Controllers

- (void)addChildOverlayController:(UIViewController *)controller animated:(BOOL)animated
{
    [self parentController:self addChildOverlayController:controller animated:animated];
}

- (void)parentController:(UIViewController *)parentController addChildOverlayController:(UIViewController *)controller animated:(BOOL)animated
{
    // add as child view controller
    [parentController addChildViewController:controller];
    controller.view.frame = parentController.view.bounds;
    [parentController.view addSubview:controller.view];
    [controller didMoveToParentViewController:parentController];

    // animate
    if (animated) {
        controller.view.alpha = 0;
        [UIView animateWithDuration:0.4
                         animations:^{
                             controller.view.alpha = 1;
                         }];
    }
}

- (void)removeChildOverlayController:(UIViewController *)controller animated:(BOOL)animated
{
    if (animated) {
        [UIView animateWithDuration:0.4
            animations:^{
                controller.view.alpha = 0;
            }
            completion:^(BOOL finished) {
                [controller willMoveToParentViewController:nil];
                [controller.view removeFromSuperview];
                [controller removeFromParentViewController];
            }];
    } else {
        [controller willMoveToParentViewController:self];
        [controller.view removeFromSuperview];
        [controller removeFromParentViewController];
    }
}

#pragma mark -
#pragma mark Properties

- (LUConsole *)console
{
    return _plugin.console;
}

- (LUTheme *)theme
{
    return [LUTheme mainTheme];
}

@end
