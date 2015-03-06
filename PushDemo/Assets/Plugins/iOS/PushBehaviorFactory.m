//
//  PushRegisterFactory.m
//  KiiPushUnity
//
//  Copyright (c) 2014 Kii. All rights reserved.

#import "PushBehaviorFactory.h"
#import "DefaultPushBehavior.h"

@implementation PushBehaviorFactory

+ (id<CustomPushBehavior>) getPushBehavior{

    id<CustomPushBehavior> pushBehavior = nil;

    //change class name if you want to customize push registration
    Class clazz = NSClassFromString(@"DefaultPushBehavior");
    if(clazz && [clazz conformsToProtocol:@protocol(CustomPushBehavior)]){
        pushBehavior = [[clazz alloc] init];
    }else{
        pushBehavior = [[DefaultPushBehavior alloc]init];
    }

    return pushBehavior;
}

@end