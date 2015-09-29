#import <UIKit/UIKit.h>
#define UIColorFromRGB(rgbValue) [UIColor colorWithRed:((float)((rgbValue & 0xFF0000) >> 16))/255.0 green:((float)((rgbValue & 0xFF00) >> 8))/255.0 blue:((float)(rgbValue & 0xFF))/255.0 alpha:1.0]

extern UIViewController *UnityGetGLViewController();
extern "C" void UnitySendMessage(const char *, const char *, const char *);

@interface KiiIOSSocialNetworkConnector : NSObject<UIWebViewDelegate>{
    UIActivityIndicatorView *loading;
}
@property (nonatomic, retain) UIWebView *webView;
@property (nonatomic, retain) NSString *gameObjectName;
@property (nonatomic, retain) NSString *endPointUrl;
@property (nonatomic, assign) BOOL isFinished;
@property (nonatomic, retain) UINavigationController* webNav;
@end

@implementation KiiIOSSocialNetworkConnector

- (instancetype)initWithGameObjectName:(NSString *)gameObjectName
                           endPointUrl:(NSString *)endPointUrl
{
    self = [super init];
    if (self != nil) {
        self.isFinished = NO;
        static dispatch_once_t onceToken;
        dispatch_once(&onceToken, ^{
            [[UINavigationBar appearance] setBarTintColor:UIColorFromRGB(0x067AB5)];
        });
        
        // Initialize fields.
        _webView = [[UIWebView alloc] init];
        _webView.delegate = self;
        UIViewController* webVC= [[UIViewController alloc] init];
        
        webVC.view.backgroundColor = [UIColor whiteColor];
        
        webVC.view = self.webView;
        
        _webNav=[[UINavigationController alloc] initWithRootViewController:webVC];
        
        UIBarButtonItem* cancelButton = [[UIBarButtonItem alloc] initWithBarButtonSystemItem:UIBarButtonSystemItemCancel target:self action:@selector(cancel:)];
        cancelButton.tintColor= [UIColor whiteColor];
        webVC.navigationItem.leftBarButtonItem=cancelButton;
        
        CGRect frame = CGRectMake(0.0, 0.0, 25.0, 25.0);
        loading = [[UIActivityIndicatorView alloc] initWithFrame:frame];
        [loading sizeToFit];
        loading.autoresizingMask = (UIViewAutoresizingFlexibleLeftMargin | UIViewAutoresizingFlexibleRightMargin | UIViewAutoresizingFlexibleTopMargin | UIViewAutoresizingFlexibleBottomMargin);
        [loading startAnimating];
        UIBarButtonItem *statusInd = [[UIBarButtonItem alloc] initWithCustomView:loading];
        statusInd.style = UIBarButtonItemStylePlain;
        webVC.navigationItem.rightBarButtonItem = statusInd;
        
        [self.webView setScalesPageToFit:YES];
        self.gameObjectName = gameObjectName;
        self.endPointUrl = endPointUrl;
        
    }
    return self;
}

- (void)dealloc
{
    self.endPointUrl = nil;
    self.gameObjectName = nil;
    [self.webView stopLoading];
    self.webView.delegate = nil;
    self.webView = nil;
    loading = nil;
    self.webNav = nil;
    [super dealloc];
}

- (BOOL)webView:(UIWebView *)webView
shouldStartLoadWithRequest:(NSURLRequest *)request
 navigationType:(UIWebViewNavigationType)navigationType
{
    NSString *url = [[request URL] absoluteString];
    if ([url hasPrefix:self.endPointUrl] == NO) {
        // go to next page.
        return YES;
    }
    [self sendMessageToCSharpLayerWithDictionary:@{
                                                   @"type" : @"finished",
                                                   @"value" : @{ @"url" : url }}];
    return NO;
}

- (void)webView:(UIWebView *)webView didFailLoadWithError:(NSError *)error
{
    [loading stopAnimating];
    if([[error domain] isEqual:NSURLErrorDomain] != NO) {
        switch ([error code]) {
            case NSURLErrorTimedOut:
            case NSURLErrorCannotFindHost:
            case NSURLErrorCannotConnectToHost:
            case NSURLErrorNetworkConnectionLost:
            case NSURLErrorNotConnectedToInternet:
                // Expected errors.
                break;
            default:
                NSLog(@"Unexpected error: %@", [error description]);
                break;
        }
    }
    [self sendMessageToCSharpLayerWithDictionary:@{@"type" : @"retry" }];
}
-(void)webViewDidFinishLoad:(UIWebView *)webView{
    [loading stopAnimating];
}
-(void)webViewDidStartLoad:(UIWebView *)webView{
    [loading startAnimating];
}
- (void)cancel:(id)sender
{
    [self.webView stopLoading];
    UIWindow* window =[[[UIApplication sharedApplication] delegate] window];
    [window.rootViewController dismissViewControllerAnimated:YES completion:^{
        
    }];
    [self sendMessageToCSharpLayerWithDictionary:@{@"type" : @"canceled" }];
}

- (void)sendMessageToCSharpLayerWithDictionary:(NSDictionary *)dictonary
{
    @synchronized (self) {
        if (self.isFinished != NO) {
            return;
        }
        self.isFinished = YES;
    }
    
    NSError *error = nil;
    NSData *data = [NSJSONSerialization
                    dataWithJSONObject:dictonary
                    options:NSJSONWritingPrettyPrinted
                    error:&error];
    NSString *message = error == nil ?
    [[[NSString alloc] initWithData:data
                           encoding:NSUTF8StringEncoding] autorelease] :
    [NSString stringWithFormat:@"{ \"type\" : \"error\", \"value\" : { \"message\" : \"%@\"} }", [error description]];
    
    UnitySendMessage([self.gameObjectName UTF8String],
                     "OnSocialAuthenticationFinished", [message UTF8String]);
}


@end

extern "C" {
    void *_KiiIOSSocialNetworkConnector_StartAuthentication(
                                                            const char* gameObjectName,
                                                            const char* accessUrl,
                                                            const char* endPointUrl,
                                                            float left,
                                                            float right,
                                                            float top,
                                                            float bottom);
    void _KiiIOSSocialNetworkConnector_Destroy(void *instance);
}

void *_KiiIOSSocialNetworkConnector_StartAuthentication(
                                                        const char* gameObjectName,
                                                        const char* accessUrl,
                                                        const char* endPointUrl,
                                                        float x,
                                                        float y,
                                                        float width,
                                                        float height)
{
    // Create KiiIOSSocialNetworkConnector.
    KiiIOSSocialNetworkConnector *retval =
    [[KiiIOSSocialNetworkConnector alloc]
     initWithGameObjectName:[NSString stringWithUTF8String:gameObjectName]
     endPointUrl:[NSString stringWithUTF8String:endPointUrl]];
    
    
    
    [retval.webView
     loadRequest:
     [NSURLRequest requestWithURL:
      [NSURL URLWithString:
       [NSString stringWithUTF8String:accessUrl]]]];
    UIWindow* window =[[[UIApplication sharedApplication] delegate] window];
    [window.rootViewController presentViewController:retval.webNav animated:YES completion:^{
        
    }];
    
    return retval;
}

void _KiiIOSSocialNetworkConnector_Destroy(void *instance)
{
    KiiIOSSocialNetworkConnector *connector =
    (KiiIOSSocialNetworkConnector *)instance;
    if (connector.webNav.isBeingDismissed) {
        [connector release];
    }else{
        UIWindow* window =[[[UIApplication sharedApplication] delegate] window];
        [window.rootViewController dismissViewControllerAnimated:YES completion:^{
            [connector release];
        }];
    }
}
