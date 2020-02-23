#ifdef UNITY_4_0 || UNITY_5_0
#import "iPhone_View.h"
#else
extern UIViewController* UnityGetGLViewController();
#endif

// Credit: https://github.com/ChrisMaire/unity-native-sharing

extern "C" void _NativeShare_Share( const char* files[], int filesCount, char* subject, const char* text ) 
{
	NSMutableArray *items = [NSMutableArray new];

	if( strlen( text ) > 0 )
		[items addObject:[NSString stringWithUTF8String:text]];

	// Credit: https://answers.unity.com/answers/862224/view.html
	for( int i = 0; i < filesCount; i++ ) 
	{
		NSString *filePath = [NSString stringWithUTF8String:files[i]];
		UIImage *image = [UIImage imageWithContentsOfFile:filePath];
		if( image != nil )
			[items addObject:image];
		else
			[items addObject:[NSURL fileURLWithPath:filePath]];
	}

	UIActivityViewController *activity = [[UIActivityViewController alloc] initWithActivityItems:items applicationActivities:nil];
	if( strlen( subject ) > 0 )
		[activity setValue:[NSString stringWithUTF8String:subject] forKey:@"subject"];

	UIViewController *rootViewController = UnityGetGLViewController();
	if( UI_USER_INTERFACE_IDIOM() == UIUserInterfaceIdiomPhone ) // iPhone
	{
		[rootViewController presentViewController:activity animated:YES completion:nil];
	}
	else // iPad
	{
		UIPopoverController *popup = [[UIPopoverController alloc] initWithContentViewController:activity];
		[popup presentPopoverFromRect:CGRectMake( rootViewController.view.frame.size.width / 2, rootViewController.view.frame.size.height / 2, 1, 1 ) inView:rootViewController.view permittedArrowDirections:0 animated:YES];
	}
}