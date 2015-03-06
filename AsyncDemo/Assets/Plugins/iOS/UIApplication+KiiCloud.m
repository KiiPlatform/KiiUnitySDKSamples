//
//  UIApplication+KiiCloud.m
//  KiiPushUnity
//
//  Created by Syah Riza on 3/20/14.
//  Copyright (c) 2014 Kii. All rights reserved.
//
#import <objc/runtime.h>
#import "UIApplication+KiiCloud.h"
#import "CustomPushBehavior.h"
#import "PushBehaviorFactory.h"

void registerForRemoteNotifications()
{
    
    id<CustomPushBehavior> pushBehavior = [PushBehaviorFactory getPushBehavior];
    
    if (pushBehavior) {
        [pushBehavior registerRemoteNotification];
    }
}

void unregisterForRemoteNotifications()
{
    [[UIApplication sharedApplication] unregisterForRemoteNotifications];
}

char * listenerGameObject = 0;
void setListenerGameObject(char * listenerName)
{
    free(listenerGameObject);
    listenerGameObject = 0;
    unsigned long len = strlen(listenerName);
    listenerGameObject = malloc(len+1);
    strcpy(listenerGameObject, listenerName);
}

char* cStringCopy(const char* string)
{
    if (string == NULL)
        return NULL;
    char* res = (char*)malloc(strlen(string) + 1);
    strcpy(res, string);
    return res;
}
char* getLastMessage()
{
    NSUserDefaults *ud = [NSUserDefaults standardUserDefaults];
    NSString *message = [ud stringForKey:@"LAST_MESSAGE"];
    if (message)
    {
        [ud removeObjectForKey:@"LAST_MESSAGE"];
        [ud synchronize];
        return cStringCopy([message UTF8String]);;
    }
    else
    {
        return NULL;
    }
}

@implementation UIApplication (KiiCloud)
+(void)load
{
    KII_DEBUG_LOG(@"%s",__FUNCTION__);
    method_exchangeImplementations(class_getInstanceMethod(self, @selector(setDelegate:)), class_getInstanceMethod(self, @selector(setKiiDelegate:)));
    
    UIApplication *app = [UIApplication sharedApplication];
    KII_DEBUG_LOG(@"Initializing application: %@, %@", app, app.delegate);
}

BOOL kiiRunTimeDidFinishLaunching(id self, SEL _cmd, id application, id launchOptions)
{
    BOOL result = YES;
    
    if ([self respondsToSelector:@selector(application:kiiDidFinishLaunchingWithOptions:)])
    {
        result = (BOOL) [self application:application kiiDidFinishLaunchingWithOptions:launchOptions];
    }
    else
    {
        [self applicationDidFinishLaunching:application];
        result = YES;
    }
    
    return result;
}


void kiiRunTimeDidRegisterForRemoteNotificationsWithDeviceToken(id self, SEL _cmd, id application, id devToken)
{
    if ([self respondsToSelector:@selector(application:kiiDidRegisterForRemoteNotificationsWithDeviceToken:)])
    {
        [self application:application kiiDidRegisterForRemoteNotificationsWithDeviceToken:devToken];
    }
    const unsigned *tokenBytes = [devToken bytes];
    NSString *hexToken = [NSString stringWithFormat:@"%08x%08x%08x%08x%08x%08x%08x%08x",
                          ntohl(tokenBytes[0]), ntohl(tokenBytes[1]), ntohl(tokenBytes[2]),
                          ntohl(tokenBytes[3]), ntohl(tokenBytes[4]), ntohl(tokenBytes[5]),
                          ntohl(tokenBytes[6]), ntohl(tokenBytes[7])];
    KII_DEBUG_LOG(@"Register Succeeded :%@",hexToken);
    UnitySendMessage(listenerGameObject, "OnDidRegisterForRemoteNotificationsWithDeviceToken", [hexToken UTF8String]);
    
}

void kiiRunTimeDidFailToRegisterForRemoteNotificationsWithError(id self, SEL _cmd, id application, id error)
{
    if ([self respondsToSelector:@selector(application:kiiDidFailToRegisterForRemoteNotificationsWithError:)])
    {
        [self application:application kiiDidFailToRegisterForRemoteNotificationsWithError:error];
    }
    NSString *errorString = [error description];
    const char * str = [errorString UTF8String];
    UnitySendMessage(listenerGameObject, "OnDidFailToRegisterForRemoteNotificationsWithError", str);
    KII_DEBUG_LOG(@"Error registering for push notifications. Error: %@", error);
}

void kiiRunTimeDidReceiveRemoteNotification(id self, SEL _cmd, id application, id userInfo)
{
    KII_DEBUG_LOG(@"##### kiiRunTimeDidReceiveRemoteNotification");
    if ([self respondsToSelector:@selector(application:kiiDidReceiveRemoteNotification:)])
    {
        [self application:application kiiDidReceiveRemoteNotification:userInfo];
    }
    
    NSError *error;
    NSData *jsonData = [NSJSONSerialization dataWithJSONObject:userInfo
                                                       options:NSJSONWritingPrettyPrinted // Pass 0 if you don't care about the readability of the generated string
                                                         error:&error];
    NSString *jsonString = nil;
    if (! jsonData)
    {
        KII_DEBUG_LOG(@"Got an error: %@", error);
    }
    else
    {
        jsonString = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
        KII_DEBUG_LOG(@"jsonString= %@",jsonString);
    }
    
    if (jsonString)
    {
        NSUserDefaults *ud = [NSUserDefaults standardUserDefaults];
        [ud setObject:jsonString forKey:@"LAST_MESSAGE"];
        [ud synchronize];
        const char * str = [jsonString UTF8String];
        UnitySendMessage(listenerGameObject, "OnPushNotificationsReceived", str);
    }
    else
    {
        UnitySendMessage(listenerGameObject, "OnPushNotificationsReceived", nil);
    }
}

void kiiRunTimeDidReceiveRemoteNotificationInBackground(id self, SEL _cmd, id application, id userInfo, id handler)
{
    kiiRunTimeDidReceiveRemoteNotification(self, _cmd, application, userInfo);
    NSDictionary *payload = (NSDictionary *) userInfo;
    
    void (^completionHandler)(UIBackgroundFetchResult)  = (void (^)(UIBackgroundFetchResult)) handler;
    if ([payload[@"aps"][@"content-available"] intValue] == 1) {
        completionHandler(UIBackgroundFetchResultNewData);
        return;
    } else {
        completionHandler(UIBackgroundFetchResultNoData);
        return;
    }
    
}

void kiiRunTimeHandleActionWithIdentifier(id self, SEL _cmd, id application, id identifier, id userInfo, id handler)
{
    NSMutableDictionary *payload = [(NSDictionary *) userInfo mutableCopy];
    if (identifier) {
        payload[@"actionIdentifier"] = (NSString*) identifier;
    }
    
    kiiRunTimeDidReceiveRemoteNotification(self, _cmd, application, payload);
    void (^completionHandler)()  = (void (^)()) handler;
    completionHandler();
}

static void exchangeMethodImplementations(Class class, SEL oldMethod, SEL newMethod, IMP impl, const char * signature)
{
    Method method = nil;
    //Check whether method exists in the class
    method = class_getInstanceMethod(class, oldMethod);
    
    if (method)
    {
        //if method exists add a new method
        class_addMethod(class, newMethod, impl, signature);
        //and then exchange with original method implementation
        method_exchangeImplementations(class_getInstanceMethod(class, oldMethod), class_getInstanceMethod(class, newMethod));
    }
    else
    {
        //if method does not exist, simply add as orignal method
        class_addMethod(class, oldMethod, impl, signature);
    }
}


- (void) setKiiDelegate:(id<UIApplicationDelegate>)delegate
{
    
    static Class delegateClass = nil;
    
    if(delegateClass == [delegate class])
    {
        [self setKiiDelegate:delegate];
        return;
    }
    
    delegateClass = [delegate class];
    
    exchangeMethodImplementations(delegateClass, @selector(application:didFinishLaunchingWithOptions:),
                                  @selector(application:kiiDidFinishLaunchingWithOptions:), (IMP)kiiRunTimeDidFinishLaunching, "v@:::");
    
    exchangeMethodImplementations(delegateClass, @selector(application:didRegisterForRemoteNotificationsWithDeviceToken:),
                                  @selector(application:kiiDidRegisterForRemoteNotificationsWithDeviceToken:), (IMP)kiiRunTimeDidRegisterForRemoteNotificationsWithDeviceToken, "v@:::");
    
    exchangeMethodImplementations(delegateClass, @selector(application:didFailToRegisterForRemoteNotificationsWithError:),
                                  @selector(application:kiiDidFailToRegisterForRemoteNotificationsWithError:), (IMP)kiiRunTimeDidFailToRegisterForRemoteNotificationsWithError, "v@:::");
    
    exchangeMethodImplementations(delegateClass, @selector(application:didReceiveRemoteNotification:),
                                  @selector(application:kiiDidReceiveRemoteNotification:), (IMP)kiiRunTimeDidReceiveRemoteNotification, "v@:::");
    
    exchangeMethodImplementations(delegateClass, @selector(application:didReceiveRemoteNotification:fetchCompletionHandler:),
                                  @selector(application:kiiDidReceiveRemoteNotification:fetchCompletionHandler:), (IMP)kiiRunTimeDidReceiveRemoteNotificationInBackground, "v@::::");
    
    exchangeMethodImplementations(delegateClass, @selector(application:handleActionWithIdentifier:forRemoteNotification:completionHandler:),
                                  @selector(application:kiiHandleActionWithIdentifier:forRemoteNotification:completionHandler:), (IMP)kiiRunTimeHandleActionWithIdentifier, "v@:::::");
    
    [self setKiiDelegate:delegate];
}
@end
