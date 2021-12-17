//
//  LUConsoleSettingsController.m
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


#import <objc/runtime.h>

#import "Lunar.h"

#import "LUConsoleSettingsController.h"

static const NSInteger kTagName = 1;
static const NSInteger kTagButton = 2;
static const NSInteger kTagInput = 3;
static const NSInteger kTagSwitch = 4;
static const NSInteger kTagLockButton = 5;

typedef enum : NSUInteger {
    LUSettingTypeBool,
    LUSettingTypeInt,
    LUSettingTypeDouble,
    LUSettingTypeEnum
} LUSettingType;

static NSDictionary *_propertyTypeLookup;
static NSArray *_proOnlyFeaturesLookup;

static id<LUTextFieldInputValidator> _integerValidator;
static id<LUTextFieldInputValidator> _floatValidator;

@class LUConsoleSetting;

@protocol LUConsoleSettingDelegate <NSObject>

- (void)consoleSettingDidChange:(LUConsoleSetting *)setting;

@end

@interface LUConsoleSetting : NSObject

@property (nonatomic, weak) id<LUConsoleSettingDelegate> delegate;
@property (nonatomic, readonly, weak) id target;
@property (nonatomic, readonly) NSString *name;
@property (nonatomic, readonly) LUSettingType type;
@property (nonatomic, readonly) NSString *title;
@property (nonatomic, strong) id value;
@property (nonatomic, assign) BOOL boolValue;
@property (nonatomic, assign) int intValue;
@property (nonatomic, assign) double doubleValue;

@property (nonatomic, readonly, nullable) NSArray<NSString *> *values;
@property (nonatomic, readonly) BOOL proOnly;

- (instancetype)initWithTarget:(id)target name:(NSString *)name type:(LUSettingType)type title:(NSString *)title proOnly:(BOOL)proOnly;
- (instancetype)initWithTarget:(id)target name:(NSString *)name type:(LUSettingType)type title:(NSString *)title proOnly:(BOOL)proOnly values:(nullable NSArray<NSString *> *)values;

@end

@implementation LUConsoleSetting

- (instancetype)initWithTarget:(id)target name:(NSString *)name type:(LUSettingType)type title:(NSString *)title proOnly:(BOOL)proOnly
{
    return [self initWithTarget:target name:name type:type title:title proOnly:proOnly values:nil];
}

- (instancetype)initWithTarget:(id)target name:(NSString *)name type:(LUSettingType)type title:(NSString *)title  proOnly:(BOOL)proOnly values:(nullable NSArray<NSString *> *)values
{
    self = [super init];
    if (self) {
        _target = target;
        _name = name;
        _type = type;
        _title = title;
        _values = values;
		_proOnly = proOnly;
    }
    return self;
}

- (id)value
{
    return [_target valueForKey:_name];
}

- (void)setValue:(id)value
{
    [_target setValue:value forKey:_name];
    if ([_delegate respondsToSelector:@selector(consoleSettingDidChange:)]) {
        [_delegate consoleSettingDidChange:self];
    }
}

- (BOOL)boolValue
{
    return [[self value] boolValue];
}

- (void)setBoolValue:(BOOL)boolValue
{
    [self setValue:[NSNumber numberWithBool:boolValue]];
}

- (int)intValue
{
    return [[self value] intValue];
}

- (void)setIntValue:(int)intValue
{
    [self setValue:[NSNumber numberWithInt:intValue]];
}

- (double)doubleValue
{
    return [[self value] doubleValue];
}

- (void)setDoubleValue:(double)doubleValue
{
    [self setValue:[NSNumber numberWithDouble:doubleValue]];
}

@end

@interface LUConsoleSettingsSection : NSObject

@property (nonatomic, readonly) NSString *title;
@property (nonatomic, readonly) NSArray<LUConsoleSetting *> *entries;

- (instancetype)initWithTitle:(NSString *)title entries:(NSArray<LUConsoleSetting *> *)entries;

@end

@implementation LUConsoleSettingsSection

- (instancetype)initWithTitle:(NSString *)title entries:(NSArray<LUConsoleSetting *> *)entries
{
    self = [super init];
    if (self) {
        _title = title;
        _entries = entries;
    }
    return self;
}

@end

@interface LUConsoleSettingsController () <UITableViewDataSource, LUConsolePopupControllerDelegate> {
    NSArray<LUConsoleSettingsSection *> *_sections;
    LUPluginSettings *_settings;
}

@property (nonatomic, weak) IBOutlet UITableView *tableView;

@end

@interface LUConsoleSettingsController () <LUConsoleSettingDelegate, LUTextFieldInputDelegate>
@end

@implementation LUConsoleSettingsController

+ (void)initialize
{
    if ([self class] == [LUConsoleSettingsController class]) {
        _integerValidator = [LUTextFieldIntegerInputValidator new];
        _floatValidator = [LUTextFieldFloatInputValidator new];
    }
}

- (instancetype)initWithSettings:(LUPluginSettings *)settings
{
    self = [super initWithNibName:NSStringFromClass([self class]) bundle:nil];
    if (self) {
        _settings = settings;
        _sections = [self listSections:settings];
    }
    return self;
}


#pragma mark -
#pragma mark View

- (void)viewDidLoad
{
    [super viewDidLoad];

    LUTheme *theme = [LUTheme mainTheme];
    self.tableView.backgroundColor = theme.tableColor;

    self.popupTitle = @"Settings";
    self.popupIcon = theme.settingsIconImage;
}

- (CGSize)preferredPopupSize
{
	const CGFloat rowHeight = 44;
	const CGFloat headerHeight = 28;
	
	CGFloat height = _sections.count * headerHeight;
	for (LUConsoleSettingsSection *section in _sections) {
		height += section.entries.count * rowHeight;
	}
	
	return CGSizeMake(0, height);
}

#pragma mark -
#pragma mark UITableViewDataSource

- (NSInteger)numberOfSectionsInTableView:(UITableView *)tableView
{
    return _sections.count;
}

- (NSInteger)tableView:(UITableView *)tableView numberOfRowsInSection:(NSInteger)section
{
    return _sections[section].entries.count;
}

- (NSString *)tableView:(UITableView *)tableView titleForHeaderInSection:(NSInteger)section
{
    return _sections[section].title;
}

- (UITableViewCell *)tableView:(UITableView *)tableView cellForRowAtIndexPath:(NSIndexPath *)indexPath
{
    LUConsoleSetting *setting = _sections[indexPath.section].entries[indexPath.row];
    UITableViewCell *cell = [tableView dequeueReusableCellWithIdentifier:@"Setting Cell"];
    if (cell == nil) {
        cell = (UITableViewCell *)[[NSBundle mainBundle] loadNibNamed:@"LUSettingsTableCell" owner:self options:nil].firstObject;
    }

    LUTheme *theme = [LUTheme mainTheme];

    cell.contentView.backgroundColor = indexPath.row % 2 == 0 ? theme.backgroundColorLight : theme.backgroundColorDark;

    UILabel *nameLabel = [cell.contentView viewWithTag:kTagName];
    LUButton *enumButton = [cell.contentView viewWithTag:kTagButton];
    LUTextField *inputField = [cell.contentView viewWithTag:kTagInput];
    LUSwitch *boolSwitch = [cell.contentView viewWithTag:kTagSwitch];
	UIButton *lockButton = [cell.contentView viewWithTag:kTagLockButton];

	BOOL available = LUConsoleIsFullVersion || !setting.proOnly;
	
    enumButton.hidden = YES;
    inputField.hidden = YES;
    boolSwitch.hidden = YES;
	lockButton.hidden = available;

    nameLabel.font = theme.font;
    nameLabel.textColor = available ? theme.cellLog.textColor : theme.settingsTextColorUnavailable;
    nameLabel.text = setting.title;

    switch (setting.type) {
        case LUSettingTypeBool:
            boolSwitch.hidden = NO;
			boolSwitch.enabled = available;
            boolSwitch.on = [setting boolValue];
            boolSwitch.userData = setting;
            [boolSwitch addTarget:self action:@selector(onToggleBoolean:) forControlEvents:UIControlEventValueChanged];
            boolSwitch.enabled = available;
            break;
        case LUSettingTypeInt:
            inputField.hidden = NO;
			inputField.enabled = available;
            inputField.text = [NSString stringWithFormat:@"%d", [setting intValue]];
            inputField.textValidator = _integerValidator;
            inputField.textInputDelegate = self;
            inputField.userData = setting;
            break;
        case LUSettingTypeDouble:
            inputField.hidden = NO;
			inputField.enabled = available;
            inputField.text = [NSString stringWithFormat:@"%g", [setting doubleValue]];
            inputField.textValidator = _floatValidator;
            inputField.textInputDelegate = self;
            inputField.userData = setting;
            break;
        case LUSettingTypeEnum:
            enumButton.hidden = NO;
			enumButton.enabled = available;
            int index = [setting intValue];
            [enumButton setTitle:setting.values[index] forState:UIControlStateNormal];
            enumButton.titleLabel.font = theme.enumButtonFont;
            enumButton.userData = setting;
            [enumButton setTitleColor:theme.enumButtonTitleColor forState:UIControlStateNormal];
            if (enumButton.allTargets.count == 0) {
                [enumButton addTarget:self action:@selector(enumButtonClicked:) forControlEvents:UIControlEventTouchUpInside];
            }
            break;
    }
	
	if (!available) {
		[lockButton addTarget:self action:@selector(lockButtonClick:) forControlEvents:UIControlEventTouchUpInside];
	}

    return cell;
}

- (void)enumButtonClicked:(LUButton *)button
{
    LUConsoleSetting *setting = button.userData;

    LUEnumPickerViewController *picker = [[LUEnumPickerViewController alloc] initWithValues:setting.values initialIndex:[setting intValue]];
    picker.userData = @[ setting, button ];
    picker.popupTitle = setting.title;
    picker.popupIcon = [LUTheme mainTheme].settingsIconImage;

    LUConsolePopupController *popupController = [[LUConsolePopupController alloc] initWithContentController:picker];
    [popupController presentFromController:self.parentViewController animated:YES];
    popupController.popupDelegate = self;
}

- (void)lockButtonClick:(id)sender
{
	NSArray *actions = @[
        [[LUAlertAction alloc] initWithTitle:@"Close" handler:nil],
		[[LUAlertAction alloc] initWithTitle:@"Learn More" handler:^(LUAlertAction *action) {
			[[NSNotificationCenter defaultCenter] postNotificationName:LUConsoleCheckFullVersionNotification
																object:nil
															  userInfo:@{ LUConsoleCheckFullVersionNotificationSource : @"settings" }];
			
			[[UIApplication sharedApplication] openURL:[NSURL URLWithString:@"https://goo.gl/UHej7v"]];
		}],
	];
	[LUUIHelper showAlertViewWithTitle:@"PRO only feature2" message:@"Not available in FREE version" actions:actions];
}

#pragma mark -
#pragma mark UITableViewDelegate

- (void)tableView:(UITableView *)tableView willDisplayHeaderView:(UIView *)view forSection:(NSInteger)section
{
    if ([view isKindOfClass:[UITableViewHeaderFooterView class]]) {
        LUTheme *theme = [LUTheme mainTheme];

        UITableViewHeaderFooterView *headerView = (UITableViewHeaderFooterView *)view;
        headerView.textLabel.font = theme.actionsGroupFont;
        headerView.textLabel.textColor = theme.actionsGroupTextColor;
        headerView.contentView.backgroundColor = theme.actionsGroupBackgroundColor;
    }
}

#pragma mark -
#pragma mark LUTextFieldInputDelegate

- (void)textFieldDidEndEditing:(LUTextField *)textField
{
    LUConsoleSetting *setting = textField.userData;
    switch (setting.type) {
        case LUSettingTypeInt: {
            NSInteger value;
            LUStringTryParseInteger(textField.text, &value);
            setting.intValue = (int)value;
            break;
        }
        case LUSettingTypeDouble: {
            float value;
            LUStringTryParseFloat(textField.text, &value);
            setting.doubleValue = value;
            break;
        }
        case LUSettingTypeBool:
        case LUSettingTypeEnum:
            break;
    }
}

- (void)textFieldInputDidBecomeInvalid:(LUTextField *)textField
{
    LUDisplayAlertView(@"Input Error", [NSString stringWithFormat:@"Invalid value: '%@'", textField.text]);
}

#pragma mark -
#pragma mark LUConsolePopupControllerDelegate

- (void)popupControllerDidDismiss:(LUConsolePopupController *)controller
{
    LUEnumPickerViewController *pickerController = (LUEnumPickerViewController *)controller.contentController;

    LUConsoleSetting *setting = pickerController.userData[0];
    UIButton *button = pickerController.userData[1];
    setting.intValue = (int)pickerController.selectedIndex;
    [button setTitle:pickerController.selectedValue forState:UIControlStateNormal];

    [controller dismissAnimated:YES];
}

#pragma mark -
#pragma mark Controls

- (void)onToggleBoolean:(LUSwitch *)swtch
{
    LUConsoleSetting *setting = swtch.userData;
    setting.value = swtch.isOn ? @YES : @NO;
}

#pragma mark -
#pragma mark Entries

- (NSArray<LUConsoleSettingsSection *> *)listSections:(LUPluginSettings *)settings
{
    NSArray *sections = @[
        [[LUConsoleSettingsSection alloc] initWithTitle:@"Common"
        entries:@[
            [[LUConsoleSetting alloc] initWithTarget:settings
                                                name:@"richTextTags"
                                                type:LUSettingTypeBool
                                               title:@"Enable Rich Text Tags"
                                             proOnly:NO]
        ]],
        [[LUConsoleSettingsSection alloc] initWithTitle:@"Exception Warning"
                                                entries:@[
                                                    [[LUConsoleSetting alloc] initWithTarget:settings.exceptionWarning
                                                                                        name:@"displayMode"
                                                                                        type:LUSettingTypeEnum
                                                                                       title:@"Display Mode"
																					 proOnly:NO
                                                                                      values:@[ @"None", @"Errors", @"Exceptions", @"All" ]]
                                                ]],
        [[LUConsoleSettingsSection alloc] initWithTitle:@"Log Overlay"
                                                entries:@[
                                                    [[LUConsoleSetting alloc] initWithTarget:settings.logOverlay
                                                                                        name:@"enabled"
                                                                                        type:LUSettingTypeBool
                                                                                       title:@"Enabled"
																					 proOnly:YES],
                                                    [[LUConsoleSetting alloc] initWithTarget:settings.logOverlay
                                                                                        name:@"maxVisibleLines"
                                                                                        type:LUSettingTypeInt
                                                                                       title:@"Max Visible Lines"
																					 proOnly:YES],
                                                    [[LUConsoleSetting alloc] initWithTarget:settings.logOverlay
                                                                                        name:@"timeout"
                                                                                        type:LUSettingTypeDouble
                                                                                       title:@"Timeout"
																					 proOnly:YES]
                                                ]],
    ];

    for (LUConsoleSettingsSection *section in sections) {
        for (LUConsoleSetting *setting in section.entries) {
            setting.delegate = self;
        }
    }

    return sections;
}

#pragma mark -
#pragma mark LUConsoleSettingDelegate

- (void)consoleSettingDidChange:(LUConsoleSetting *)setting
{
    [LUNotificationCenter postNotificationName:LUNotificationSettingsDidChange object:nil];
}

@end
