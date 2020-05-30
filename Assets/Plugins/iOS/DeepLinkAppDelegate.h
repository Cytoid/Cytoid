#ifndef DeepLinkAppDelegate_h
#define DeepLinkAppDelegate_h

#import "UnityAppController.h"

@interface DeepLinkAppDelegate : UnityAppController
@property (nonatomic, copy) NSString* lastURL;
- (void) deepLinkIsAlive;
- (char *) deepLinkURL;
@end

#endif /* DeepLinkAppDelegate_h */