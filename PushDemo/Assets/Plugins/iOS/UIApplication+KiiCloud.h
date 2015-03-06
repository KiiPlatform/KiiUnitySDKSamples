//
//  UIApplication+KiiCloud.h
//  KiiPushUnity
//
//  Created by Syah Riza on 3/20/14.
//  Copyright (c) 2014 Kii. All rights reserved.
//

#import <Foundation/Foundation.h>

#ifdef KII_DEBUG
#define KII_DEBUG_LOG(message, ...) \
NSLog(message, ##__VA_ARGS__);
#else
#define KII_DEBUG_LOG(message, ...)
#endif

#import <UIKit/UIKit.h>

@interface UIApplication (SupressWarnings)
- (void)application:(UIApplication *)application kiiDidRegisterForRemoteNotificationsWithDeviceToken:(NSData *)devToken;
- (void)application:(UIApplication *)application kiiDidFailToRegisterForRemoteNotificationsWithError:(NSError *)err;
- (void)application:(UIApplication *)application kiiDidReceiveRemoteNotification:(NSDictionary *)userInfo;
- (void)application:(UIApplication *)application kiiDidReceiveRemoteNotification:(NSDictionary *)userInfo fetchCompletionHandler:(void (^)(UIBackgroundFetchResult result))handler;
- (BOOL)application:(UIApplication *)application kiiDidFinishLaunchingWithOptions:(NSDictionary *)launchOptions;
- (void)application:(UIApplication *)application kiiHandleActionWithIdentifier:(NSString *)identifier forRemoteNotification:(NSDictionary *)userInfo completionHandler:(void(^)())completionHandler;
@end
