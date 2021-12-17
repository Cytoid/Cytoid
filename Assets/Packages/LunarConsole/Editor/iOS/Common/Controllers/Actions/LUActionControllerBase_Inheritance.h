//
//  LUActionControllerBase_Inheritance.h
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


#import "LUActionControllerBase.h"

@interface LUActionControllerBase ()

@property (nonatomic, weak) IBOutlet UIView       * noActionsWarningView;
@property (nonatomic, weak) IBOutlet UILabel      * noActionsWarningLabel;
@property (nonatomic, weak) IBOutlet UIView       * actionsView;
@property (nonatomic, weak) IBOutlet UITableView  * tableView;
@property (nonatomic, weak) IBOutlet UISearchBar  * filterBar;
@property (nonatomic, weak) IBOutlet UIButton     * learnMoreButton;

- (void)updateNoActionWarningView;
- (void)setNoActionsWarningViewHidden:(BOOL)hidden;

@end
