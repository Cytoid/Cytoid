#import <Foundation/Foundation.h>

typedef void (*CytoidHostMessageHandler)(const char *json);

static CytoidHostMessageHandler gCytoidHostMessageHandler = NULL;

static NSString *const kCytoidHostNativeOutboundNotification = @"CytoidHostNativeOutboundMessage";
static NSString *const kCytoidHostNativeOutboundJsonKey = @"json";

static void DeliverOutboundMessage(NSString *json)
{
    if (json.length == 0)
    {
        return;
    }

    dispatch_async(dispatch_get_main_queue(), ^{
        [[NSNotificationCenter defaultCenter]
            postNotificationName:kCytoidHostNativeOutboundNotification
                          object:nil
                        userInfo:@{kCytoidHostNativeOutboundJsonKey : json}];
    });
}

extern "C" {

void CytoidHostNative_SetMessageHandler(CytoidHostMessageHandler handler)
{
    gCytoidHostMessageHandler = handler;
}

void CytoidHostNative_SendMessage(const char *json)
{
    if (json == NULL)
    {
        return;
    }

    NSString *message = [NSString stringWithUTF8String:json];
    if (message.length == 0)
    {
        return;
    }

    DeliverOutboundMessage(message);

    if (gCytoidHostMessageHandler != NULL)
    {
        gCytoidHostMessageHandler(json);
    }
}

}
