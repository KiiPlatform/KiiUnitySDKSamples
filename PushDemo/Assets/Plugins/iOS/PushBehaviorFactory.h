//
//  PushRegisterFactory.h
//  KiiPushUnity
//
//  Copyright (c) 2014 Kii. All rights reserved.
#import "CustomPushBehavior.h"

@interface PushBehaviorFactory : NSObject
+ (id<CustomPushBehavior>) getPushBehavior;
@end