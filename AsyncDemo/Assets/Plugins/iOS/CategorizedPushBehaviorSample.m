//
//  CustomPush.m
//  Unity-iPhone
//
//  Created by Syah Riza on 10/23/14.
//
//

#import "CustomPushBehavior.h"
@interface CategorizedPushBehaviorSample : NSObject<CustomPushBehavior>

@end

@implementation CategorizedPushBehaviorSample
-(void) registerRemoteNotification{
    UIApplication *application = [UIApplication sharedApplication];
    // Register APNS
    // If you use Xcode5, you can only use the same code as the else block.
    if ([application respondsToSelector:@selector(registerUserNotificationSettings:)]) {
        // iOS8 : you can define categories and action below
        /* define notification actions */
        
        UIMutableUserNotificationAction *acceptAction = [[UIMutableUserNotificationAction alloc] init];
        
        acceptAction.identifier = @"ACCEPT_IDENTIFIER";
        
        acceptAction.title = @"Accept";
        
        acceptAction.destructive = NO;
        
        UIMutableUserNotificationAction *declineAction = [[UIMutableUserNotificationAction alloc] init];
        declineAction.identifier = @"DECLINE_IDENTIFIER";
        declineAction.title = @"Decline";
        declineAction.destructive = YES;
        //declineAction.activationMode = UIUserNotificationActivationModeBackground;
        declineAction.authenticationRequired = NO;
        
        
        /*Define Categories*/
        
        UIMutableUserNotificationCategory *inviteCategory =
        [[UIMutableUserNotificationCategory alloc] init];
        
        inviteCategory.identifier = @"INVITE_CATEGORY";
        
        [inviteCategory setActions:@[acceptAction, declineAction]
                        forContext:UIUserNotificationActionContextDefault];
        [inviteCategory setActions:@[acceptAction, declineAction]
                        forContext:UIUserNotificationActionContextMinimal];
        
        NSSet *categories= [NSSet setWithObject:inviteCategory];
        
        UIUserNotificationSettings* notificationSettings =
        [UIUserNotificationSettings settingsForTypes:UIUserNotificationTypeBadge |
         UIUserNotificationTypeSound |
         UIUserNotificationTypeAlert
                                          categories:categories];
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
