using UnityEngine;
using KiiCorp.Cloud.Storage;
using KiiCorp.Cloud.Unity;
using System;
using System.Collections;

public class Push2App : MonoBehaviour
{
    private string payload = "";
    private string message = "--- Logs will show here ---";
    private KiiPushPlugin kiiPushPlugin = null;
    private static string USER_NAME = "unitypushdemo" + Environment.TickCount;
    private const string PASSWORD = "password";
    private static string BUCKET_NAME = "userbucket";
    private KiiPushPlugin.KiiPushMessageReceivedCallback receivedCallback;

    void Awake ()
    {
    }

    // Use this for initialization
    void Start ()
    {
        Debug.Log ("#####Main.Start");
        this.kiiPushPlugin = GameObject.Find ("KiiPushPlugin").GetComponent<KiiPushPlugin> ();

        this.receivedCallback = (ReceivedMessage message) => {
            switch (message.PushMessageType)
            {
            case ReceivedMessage.MessageType.PUSH_TO_APP:
                Debug.Log ("#####PUSH_TO_APP Message");
                this.OnPushNotificationsReceived (message);
                break;
            case ReceivedMessage.MessageType.PUSH_TO_USER:
                Debug.Log ("#####PUSH_TO_USER Message");
                this.OnPushNotificationsReceived (message);
                break;
            case ReceivedMessage.MessageType.DIRECT_PUSH:
                Debug.Log ("#####DIRECT_PUSH Message");
                this.OnPushNotificationsReceived (message);
                break;
            }
        };
        this.kiiPushPlugin.OnPushMessageReceived += this.receivedCallback;

        if (KiiUser.CurrentUser != null)
        {
            Invoke ("registerPush", 0);
            return;
        }

        KiiUser.LogIn (USER_NAME, PASSWORD, (KiiUser u1, Exception e1) => {
            if (e1 != null)
            {
                KiiUser newUser = KiiUser.BuilderWithName (USER_NAME).Build ();
                Debug.Log ("#####Register");
                newUser.Register (PASSWORD, (KiiUser u2, Exception e2) => {
                    Debug.Log ("#####callback Register");
                    if (e2 != null)
                    {
                        Debug.Log ("#####failed to Register");
                        this.ShowException ("Failed to register user.", e2);
                        return;
                    }
                    else
                    {
                        Invoke ("registerPush", 0);
                    }
                });
            }
            else
            {
                Invoke ("registerPush", 0);
            }
        });
    }

    void registerPush ()
    {		
        #if UNITY_IPHONE
        KiiPushInstallation.DeviceType deviceType = KiiPushInstallation.DeviceType.IOS;
        #elif UNITY_ANDROID
        KiiPushInstallation.DeviceType deviceType = KiiPushInstallation.DeviceType.ANDROID;
        #else
        KiiPushInstallation.DeviceType deviceType = KiiPushInstallation.DeviceType.ANDROID;
        #endif

        if (this.kiiPushPlugin == null)
        {
            Debug.Log ("#####failed to find KiiPushPlugin");
            return;
        }
        this.kiiPushPlugin.RegisterPush ((string pushToken, Exception e0) => {
            if (e0 != null)
            {
                Debug.Log ("#####failed to RegisterPush");
                this.message = "#####failed to RegisterPush : " + pushToken;
                return;
            }

            Debug.Log ("#####RegistrationId=" + pushToken);
            this.message = "Token : " + pushToken + "\n";
            Debug.Log ("#####Install");
            KiiUser.PushInstallation (true).Install (pushToken, deviceType, (Exception e3) => {
                if (e3 != null)
                {
                    Debug.Log ("#####failed to Install");
                    this.ShowException ("Failed to install PushNotification -- pushToken=" + pushToken, e3);
                    return;
                }
                KiiBucket bucket = KiiUser.CurrentUser.Bucket (BUCKET_NAME);
                // bucket.Acl(BucketAction.CREATE_OBJECTS_IN_BUCKET).Subject(KiiAnyAuthenticatedUser.Get()).Save(ACLOperation.GRANT, (KiiACLEntry<KiiBucket, BucketAction> entry, Exception e5)=>{
                //     if (e5 != null)
                //     {
                //         Debug.Log ("#####Failed to grant acl to the bucket");
                //         this.ShowException("Failed to grant acl to the bucket", e5);
                //         return;
                //     }
                Debug.Log ("#####Subscribe");
                KiiUser.CurrentUser.PushSubscription.Subscribe (bucket, (KiiSubscribable subscribable, Exception e6) => {
                    Debug.Log ("#####callback Subscribe");
                    if (e6 != null)
                    {
                        if (e6 is ConflictException)
                        {
                            this.message += "Bucket is already subscribed" + "\n";
                            this.message += "Push is ready";
                            Debug.Log ("#####all setup success!!!!!!");
                            return;
                        }
                        Debug.Log ("#####failed to Subscribe");
                        this.ShowException ("Failed to subscribe bucket", e6);
                        return;
                    }
                    else
                    {
                        this.message += "Push is ready";
                        Debug.Log ("#####all setup success!!!!!!");
                    }
                });
                // });
            });
        });
    }

    void OnGUI ()
    {
        ScalableGUI gui = new ScalableGUI ();
        gui.Label (5, 5, 310, 20, "Push2App scene");
        if (gui.Button (200, 5, 120, 35, "-> Push2User"))
        {
            this.kiiPushPlugin.OnPushMessageReceived -= this.receivedCallback;
            Application.LoadLevel ("push2user");
        }

        this.payload = gui.TextField (0, 45, 320, 50, this.payload);
        if (gui.Button (0, 100, 160, 50, "Create Object"))
        {
            KiiBucket bucket = KiiUser.CurrentUser.Bucket (BUCKET_NAME);
            KiiObject obj = bucket.NewKiiObject ();
            obj ["payload"] = this.payload;
            obj.Save ((KiiObject o, Exception e) => {
                if (e != null)
                {
                    Debug.Log ("#####" + e.Message);
                    Debug.Log ("#####" + e.StackTrace);
                    this.ShowException ("Failed to save object", e);
                    return;
                }
                this.message = "#####creating object is successful!!";
            });
        }
        if (gui.Button (160, 100, 160, 50, "Clear Log"))
        {
            this.message = "--- Logs will show here ---";
        }
        if (gui.Button (0, 150, 160, 50, "Register Push"))
        {
            Invoke ("registerPush", 0);
        }
        if (gui.Button (160, 150, 160, 50, "Unregister Push"))
        {
            this.kiiPushPlugin.UnregisterPush ((Exception e) => {
                if (e != null)
                {
                    Debug.Log ("#####" + e.Message);
                    Debug.Log ("#####" + e.StackTrace);
                    this.ShowException ("#####Unregister push is failed!!", e);
                    return;
                }
                this.message = "#####Unregister push is successful!!";
            });
        }
        if (gui.Button (0, 200, 160, 50, "Subscribe bucket"))
        {
            KiiUser user = KiiUser.CurrentUser;
            KiiBucket bucket = user.Bucket (BUCKET_NAME);
            KiiPushSubscription subscription = user.PushSubscription;
            subscription.Subscribe (bucket, (KiiSubscribable target, Exception e) => {
                if (e != null)
                {
                    Debug.Log ("#####" + e.Message);
                    Debug.Log ("#####" + e.StackTrace);
                    this.ShowException ("#####Subscribe is failed!!", e);
                    return;
                }
                this.message = "#####Subscribe is successful!!";
            });
        }
        if (gui.Button (160, 200, 160, 50, "Unsubscribe bucket"))
        {
            KiiUser user = KiiUser.CurrentUser;
            KiiBucket bucket = user.Bucket (BUCKET_NAME);
            KiiPushSubscription subscription = user.PushSubscription;
            subscription.Unsubscribe (bucket, (KiiSubscribable target, Exception e) => {
                if (e != null)
                {
                    Debug.Log ("#####" + e.Message);
                    Debug.Log ("#####" + e.StackTrace);
                    this.ShowException ("#####Unsubscribe is failed!!", e);
                    return;
                }
                this.message = "#####Unsubscribe is successful!!";
            });
        }
        if (gui.Button (0, 250, 160, 50, "Check subscription"))
        {
            KiiUser user = KiiUser.CurrentUser;
            KiiBucket bucket = user.Bucket (BUCKET_NAME);
            KiiPushSubscription subscription = user.PushSubscription;
            subscription.IsSubscribed (bucket, (KiiSubscribable target, bool isSubscribed, Exception e) => {
                if (e != null)
                {
                    Debug.Log ("#####" + e.Message);
                    Debug.Log ("#####" + e.StackTrace);
                    this.ShowException ("#####Check subscription is failed!!", e);
                    return;
                }
                this.message = "#####Subscription status : " + isSubscribed;
            });
        }
        gui.TextArea (5, 310, 310, 170, this.message, 10);
    }

    private void ShowException (string msg, Exception e)
    {
        Debug.Log ("#####" + e.Message);
        Debug.Log ("#####" + e.StackTrace);
        this.message = "#####ERROR: " + msg + "   type=" + e.GetType () + "\n";
        if (e.InnerException != null)
        {
            this.message += "#####InnerExcepton=" + e.InnerException.GetType () + "\n";
            this.message += "#####InnerExcepton.Message=" + e.InnerException.Message + "\n";
            this.message += "#####InnerExcepton.Stacktrace=" + e.InnerException.StackTrace + "\n";
        }
        this.message += "#####" + e.Message + "\n" + "#####" + e.StackTrace;
    }

    void Update ()
    {
    }

    public void OnPushNotificationsReceived (ReceivedMessage message)
    {
        this.message = "#####PushNotification Received" + "\n";
        this.message += "#####Type=" + message.PushMessageType + "\n";
        this.message += "#####Sender=" + message.Sender + "\n";
        this.message += "#####Scope=" + message.ObjectScope + "\n";
        this.message += "#####msg=" + message.GetString ("msg") + "\n";

        PushToAppMessage msg = (PushToAppMessage)message;
        msg.KiiObject.Refresh ((KiiObject obj, Exception e) => {
            this.message += "#####payload=" + obj.GetString ("payload");
        });
    }
}
