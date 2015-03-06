//
//  CustomPush.m
//  Unity-iPhone
//
//  Created by Syah Riza on 10/23/14.
//
//

#import "DefaultPushBehavior.h"

@implementation DefaultPushBehavior
-(void) registerRemoteNotification{
    UIApplication *application = [UIApplication sharedApplication];
    // Register APNS
    // If you use Xcode5, you can only use the same code as the else block.
    if ([application respondsToSelector:@selector(registerUserNotificationSettings:)]) {
        // iOS8 : you can define categories and action below
        
        UIUserNotificationSettings* notificationSettings =
        [UIUserNotificationSettings settingsForTypes:UIUserNotificationTypeBadge |
         UIUserNotificationTypeSound |
         UIUserNotificationTypeAlert
                                          categories:nil];
        [application registerUserNotificationSettings:notificationSettings];
        [application registerForRemoteNotifications];
    } else {
        // iOS7 or earlier
        [application registerForRemoteNotificationTypes:(UIRemoteNotificationTypeBadge |
                                                         UIRemoteNotificationTypeSound |
                                                         UIRemoteNotificationTypeAlert)];
    }

}
@end
