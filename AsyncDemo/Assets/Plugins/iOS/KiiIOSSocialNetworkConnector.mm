#import <UIKit/UIKit.h>

extern UIViewController *UnityGetGLViewController();
extern "C" void UnitySendMessage(const char *, const char *, const char *);

@interface KiiIOSSocialNetworkConnector : NSObject<UIWebViewDelegate>
@property (nonatomic, retain) UIWebView *webView;
@property (nonatomic, retain) UIButton *cancelButton;
@property (nonatomic, retain) NSString *gameObjectName;
@property (nonatomic, retain) NSString *endPointUrl;
@property (nonatomic, assign) BOOL isFinished;
@end

@implementation KiiIOSSocialNetworkConnector

- (instancetype)initWithGameObjectName:(NSString *)gameObjectName
                           endPointUrl:(NSString *)endPointUrl
{
    self = [super init];
    if (self != nil) {
        self.isFinished = NO;

        // Initialize fields.
        self.webView = [[UIWebView alloc] init];
        self.webView.delegate = self;
        self.cancelButton = [UIButton buttonWithType:UIButtonTypeRoundedRect];
        [self.cancelButton setTitle:@"Cancel" forState:UIControlStateNormal];
        [self.cancelButton addTarget:self
                              action:@selector(onTapButton:)
                    forControlEvents:UIControlEventTouchUpInside];
        self.gameObjectName = gameObjectName;
        self.endPointUrl = endPointUrl;

        // add view to parent view..
        UIView *parent = UnityGetGLViewController().view;
        [parent addSubview:self.webView];
        [parent addSubview:self.cancelButton];
    }
    return self;
}

- (void)dealloc
{
    self.endPointUrl = nil;
    self.gameObjectName = nil;
    [self.cancelButton removeFromSuperview];
    [self.cancelButton removeTarget:self
                             action:@selector(onTapButton:)
                   forControlEvents:UIControlEventTouchUpInside];
    self.cancelButton = nil;
    [self.webView stopLoading];
    [self.webView removeFromSuperview];
    self.webView.delegate = nil;
    self.webView = nil;
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

- (void)onTapButton:(id)sender
{
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

static CGRect createSuitableRectangle(
        float left,
        float right,
        float top,
        float bottom);

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

    // change offset to rectangle.
    CGRect rect = createSuitableRectangle(x, y, width, height);

    // Set rectangles.
    float webViewHeight = rect.size.height * 0.8;
    retval.webView.frame = CGRectMake(
                rect.origin.x,
                rect.origin.y,
                rect.size.width,
                webViewHeight);
    retval.cancelButton.frame = CGRectMake(
                rect.origin.x,
                rect.origin.y + webViewHeight,
                rect.size.width,
                rect.size.height - webViewHeight);

    [retval.webView
        loadRequest:
            [NSURLRequest requestWithURL:
                    [NSURL URLWithString:
                             [NSString stringWithUTF8String:accessUrl]]]];
    return retval;
}

void _KiiIOSSocialNetworkConnector_Destroy(void *instance)
{
    KiiIOSSocialNetworkConnector *connector =
        (KiiIOSSocialNetworkConnector *)instance;
    [connector release];
}

static CGRect createSuitableRectangle(
        float x,
        float y,
        float width,
        float height)
{
    float scale = [UIScreen instancesRespondToSelector:@selector(scale)] ?
            [UIScreen mainScreen].scale : 1.0f;
    return CGRectMake(x / scale, y / scale, width / scale, height / scale);
}
