//
//  LUDefines.h
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


#if !defined(LU_INLINE)
# if defined(__STDC_VERSION__) && __STDC_VERSION__ >= 199901L
#  define LU_INLINE static inline
# elif defined(__cplusplus)
#  define LU_INLINE static inline
# elif defined(__GNUC__)
#  define LU_INLINE static __inline__
# else
#  define LU_INLINE static
# endif
#endif

#define LU_SHOULD_IMPLEMENT_METHOD \
    NSLog(@"%@ should implement %@ method", NSStringFromClass([self class]), NSStringFromSelector(_cmd));

#if __has_feature(objc_arc)
    #define LU_WEAK __weak
#else
    #define LU_WEAK
#endif

#if LUNAR_CONSOLE_DEVELOPMENT
    #define LU_SET_ACCESSIBILITY_IDENTIFIER(VIEW, IDENTIFIER) { (VIEW).isAccessibilityElement = YES; (VIEW).accessibilityIdentifier = (IDENTIFIER); }
#else
    #define LU_SET_ACCESSIBILITY_IDENTIFIER(VIEW, IDENTIFIER)
#endif
