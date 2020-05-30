#import "DeepLinkAppDelegate.h"

extern "C" {
	void DeepLinkReceiverIsAlive();
	char * GetDeepLinkURL();
}

void DeepLinkReceiverIsAlive() {
	DeepLinkAppDelegate *appDelegate = (DeepLinkAppDelegate *)[UIApplication sharedApplication].delegate;
	[appDelegate deepLinkIsAlive];
}
char * GetDeepLinkURL() {
	DeepLinkAppDelegate *appDelegate = (DeepLinkAppDelegate *)[UIApplication sharedApplication].delegate;
	return [appDelegate deepLinkURL];
}