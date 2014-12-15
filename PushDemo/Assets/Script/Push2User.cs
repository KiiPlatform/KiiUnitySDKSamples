using UnityEngine;
using KiiCorp.Cloud.Storage;
using KiiCorp.Cloud.Unity;
using System;
using System.Collections;

public class Push2User : MonoBehaviour
{
    private string payload = "";
    private string message = "--- Logs will show here ---";
    private KiiPushPlugin kiiPushPlugin = null;
    private static string USER_NAME = "unitypushdemo" + Environment.TickCount;
    private const string PASSWORD = "password";
    private static string TOPIC_NAME = "my_box";
    private KiiPushPlugin.KiiPushMessageReceivedCallback receivedCallback = null;
    private PushSetting pushSetting = null;
    private APNSSetting apnsSetting = null;
    private GCMSetting gcmSetting = null;
    private bool pushScreen = false;
    private bool apnsScreen = false;
    private bool gcmScreen = false;

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

        pushSetting = new PushSetting ();
        apnsSetting = new APNSSetting ();
        gcmSetting = new GCMSetting ();

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
                KiiTopic t = null;
                try
                {
                    Debug.Log ("#####Topic");
                    t = KiiUser.CurrentUser.Topic (TOPIC_NAME);
                }
                catch (Exception e)
                {
                    Debug.Log ("#####failed to create topic " + e.Message);
                }
                Debug.Log ("#####Save");
                t.Save ((KiiTopic topic, Exception e4) => {
                    Debug.Log ("#####callback Save");
                    if (e4 != null && !(e4 is ConflictException))
                    {
                        Debug.Log ("#####failed to Save");
                        this.ShowException ("Failed to save topic", e4);
                        return;
                    }
                    if (e4 != null && e4 is ConflictException)
                    {
                        this.message += "Topic is already created" + "\n";
                    }
                    Debug.Log ("#####Subscribe");
                    KiiUser.CurrentUser.PushSubscription.Subscribe (topic, (KiiSubscribable subscribable, Exception e5) => {
                        Debug.Log ("#####callback Subscribe");
                        if (e5 != null)
                        {
                            if (e5 is ConflictException)
                            {
                                this.message += "Topic is already subscribed" + "\n";
                                this.message += "Push is ready, KiiUser login name : " + USER_NAME;
                                Debug.Log ("#####all setup success!!!!!!");
                                return;
                            }
                            Debug.Log ("#####failed to Subscribe");
                            this.ShowException ("Failed to subscribe topic", e5);
                            return;
                        }
                        this.message += "Push is ready, KiiUser login name : " + USER_NAME;
                        Debug.Log ("#####all setup success!!!!!!");
                    });
                });
            });
        });
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

    void OnGUI ()
    {
        ScalableGUI gui = new ScalableGUI ();

        if (pushScreen == true && apnsScreen == false && gcmScreen == false)
        {
            if (gui.Button (0, 10, 160, 50, "<< Back to main"))
            {
                pushScreen = false;
            }
            gui.Label (5, 70, 310, 30, "Push settings screen", 18);
            if (gui.Toggle (0, 100, 160, 50, pushSetting.SendAppID, "SendAppID"))
            {
                pushSetting.SendAppID = !pushSetting.SendAppID;
            }
            if (gui.Toggle (160, 100, 160, 50, pushSetting.SendObjectScope, "SendObjectScope"))
            {
                pushSetting.SendObjectScope = !pushSetting.SendObjectScope;
            }
            if (gui.Toggle (0, 150, 160, 50, pushSetting.SendOrigin, "SendOrigin"))
            {
                pushSetting.SendOrigin = !pushSetting.SendOrigin;
            }
            if (gui.Toggle (160, 150, 160, 50, pushSetting.SendSender, "SendSender"))
            {
                pushSetting.SendSender = !pushSetting.SendSender;
            }
            if (gui.Toggle (0, 200, 160, 50, pushSetting.SendTopicId, "SendTopicId"))
            {
                pushSetting.SendTopicId = !pushSetting.SendTopicId;
            }
            if (gui.Toggle (160, 200, 160, 50, pushSetting.SendWhen, "SendWhen"))
            {
                pushSetting.SendWhen = !pushSetting.SendWhen;
            }
            if (gui.Toggle (0, 250, 160, 50, pushSetting.SendToProduction, "SendToProduction"))
            {
                pushSetting.SendToProduction = !pushSetting.SendToProduction;
            }
            if (gui.Toggle (160, 250, 160, 50, pushSetting.SendToDevelopment, "SendToDevelopment"))
            {
                pushSetting.SendToDevelopment = !pushSetting.SendToDevelopment;
            }
        }
        else if (pushScreen == false && apnsScreen == true && gcmScreen == false)
        {
            if (gui.Button (0, 10, 160, 50, "<< Back to main"))
            {
                apnsScreen = false;
            }
            gui.Label (5, 70, 310, 30, "APNs settings screen", 18);
            if (gui.Toggle (0, 100, 160, 50, (apnsSetting.ContentAvailable == 1) ? true : false, "content-available"))
            {
                if (apnsSetting.ContentAvailable == 0)
                {
                    apnsSetting.ContentAvailable = 1;
                }
                else
                {
                    apnsSetting.ContentAvailable = 0;
                }
            }
            gui.Label (5, 160, 310, 20, "APNs alert body", 10);
            apnsSetting.AlertBody = gui.TextField (0, 190, 320, 50, apnsSetting.AlertBody);
        }
        else if (pushScreen == false && apnsScreen == false && gcmScreen == true)
        {
            if (gui.Button (0, 10, 160, 50, "<< Back to main"))
            {
                gcmScreen = false;
            }
            gui.Label (5, 70, 310, 30, "GCM settings screen", 18);
        }
        else
        {
            gui.Label (5, 5, 310, 20, "Push2User scene");
            if (gui.Button (200, 5, 120, 35, "-> Push2App"))
            {
                this.kiiPushPlugin.OnPushMessageReceived -= this.receivedCallback;
                Application.LoadLevel ("push2app");
            }

            this.payload = gui.TextField (0, 45, 320, 50, this.payload);

            if (gui.Button (0, 100, 160, 40, "Send Message"))
            {
                sendMessage ();
            }
            if (gui.Button (160, 100, 160, 40, "Clear Log"))
            {
                this.message = "--- Logs will show here ---";
            }
            if (gui.Button (0, 140, 160, 40, "Register Push"))
            {
                Invoke ("registerPush", 0);
            }
            if (gui.Button (160, 140, 160, 40, "Unregister Push"))
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
            if (gui.Button (0, 180, 160, 40, "Subscribe topic"))
            {
                KiiUser user = KiiUser.CurrentUser;
                KiiTopic topic = user.Topic (TOPIC_NAME);
                KiiPushSubscription subscription = user.PushSubscription;
                subscription.Subscribe (topic, (KiiSubscribable target, Exception e) => {
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
            if (gui.Button (160, 180, 160, 40, "Unsubscribe topic"))
            {
                KiiUser user = KiiUser.CurrentUser;
                KiiTopic topic = user.Topic (TOPIC_NAME);
                KiiPushSubscription subscription = user.PushSubscription;
                subscription.Unsubscribe (topic, (KiiSubscribable target, Exception e) => {
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
            if (gui.Button (0, 220, 160, 40, "Check subscription"))
            {
                KiiUser user = KiiUser.CurrentUser;
                KiiTopic topic = user.Topic (TOPIC_NAME);
                KiiPushSubscription subscription = user.PushSubscription;
                subscription.IsSubscribed (topic, (KiiSubscribable target, bool isSubscribed, Exception e) => {
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


            if (gui.Button (160, 220, 160, 40, "Push settings >>"))
            {
                pushScreen = true;
            }
            if (gui.Toggle (0, 260, 160, 40, apnsSetting.Enable, "Enable APNs"))
            {
                apnsSetting.Enable = !apnsSetting.Enable;
            }
            GUI.enabled = apnsSetting.Enable;
            if (gui.Button (160, 260, 160, 40, "APNs settings >>"))
            {
                apnsScreen = true;
            }
            GUI.enabled = true;
            if (gui.Toggle (0, 300, 160, 40, gcmSetting.Enable, "Enable GCM"))
            {
                gcmSetting.Enable = !gcmSetting.Enable;
            }
            GUI.enabled = gcmSetting.Enable;
            if (gui.Button (160, 300, 160, 40, "GCM settings >>"))
            {
                gcmScreen = true;
            }
            GUI.enabled = true;
            gui.TextArea (5, 350, 310, 130, this.message, 10);
        }
    }

    public void OnPushNotificationsReceived (ReceivedMessage message)
    {
        this.message = "#####PushNotification Received" + "\n";
        this.message += "#####Type=" + message.PushMessageType + "\n";
        this.message += "#####Sender=" + message.Sender + "\n";
        this.message += "#####Scope=" + message.ObjectScope + "\n";
        this.message += "#####payload=" + message.GetString ("payload") + "\n";
        this.message += "#####msg=" + message.GetString ("msg") + "\n";
    }

    private void sendMessage ()
    {
        if (KiiUser.CurrentUser == null)
        {
            this.message = "#####KiiUser is not logged in!";
            return;
        }
        KiiPushMessageData data = new KiiPushMessageData ();
        data.Put ("payload", this.payload);
        data.Put ("msg", "unity!!");
        KiiPushMessage message = pushSetting.GetKiiPushMessage (data, apnsSetting, gcmSetting);
        KiiTopic topic = KiiUser.CurrentUser.Topic (TOPIC_NAME);
        topic.SendMessage (message, (KiiPushMessage target, Exception e) => {
            if (e != null)
            {
                Debug.Log ("#####" + e.Message);
                Debug.Log ("#####" + e.StackTrace);
                this.ShowException ("Failed to send message", e);
                return;
            }
            this.message = "#####sending message is successful!!";
        });
    }

    class PushSetting
    {
        public bool EnableAPNS { get; set; }

        public bool EnableGCM { get; set; }

        public bool SendAppID { get; set; }

        public bool SendObjectScope { get; set; }

        public bool SendOrigin { get; set; }

        public bool SendSender { get; set; }

        public bool SendToDevelopment { get; set; }

        public bool SendTopicId { get; set; }

        public bool SendToProduction { get; set; }

        public bool SendWhen { get; set; }

        public PushSetting ()
        {
            EnableAPNS = true;
            EnableGCM = true;
            SendAppID = true;
            SendObjectScope = true;
            SendOrigin = true;
            SendSender = true;
            SendToDevelopment = true;
            SendTopicId = true;
            SendToProduction = true;
            SendWhen = true;
        }

        public KiiPushMessage GetKiiPushMessage (KiiPushMessageData data, APNSSetting apns, GCMSetting gcm)
        {
            return KiiPushMessage.BuildWith (data)
                .EnableAPNS (EnableAPNS)
                .EnableGCM (EnableGCM)
                .SendAppID (SendAppID)
                .SendObjectScope (SendObjectScope)
                .SendOrigin (SendOrigin)
                .SendSender (SendSender)
                .SendToDevelopment (SendToDevelopment)
                .SendTopicId (SendTopicId)
                .SendToProduction (SendToProduction)
                .SendWhen (SendWhen)
                .WithAPNSMessage (apns.GetAPNSMessage ())
                .WithGCMMessage (gcm.GetGCMMessage ())
                .Build ();
        }
    }

    class APNSSetting
    {
        public bool Enable { get; set; }

        public int ContentAvailable { get; set; }

        public string AlertBody { get; set; }

        public APNSSetting ()
        {
            Enable = true;
            ContentAvailable = 0;
            AlertBody = "";
        }

        public APNSMessage GetAPNSMessage ()
        {
            if (Enable == true)
            {
                APNSMessage.Builder builder = APNSMessage.CreateBuilder ()
                    .Enable (Enable)
                    .WithContentAvailable (ContentAvailable);
                if (AlertBody != "" && AlertBody != null)
                {
                    builder = builder.WithAlertBody (AlertBody);
                }
                return builder.Build ();
            }
            else
            {
                return APNSMessage.CreateBuilder ()
                    .Enable (Enable)
                    .Build ();
            }
        }
    }

    class GCMSetting
    {
        public bool Enable { get; set; }

        public GCMSetting ()
        {
            Enable = true;
        }

        public GCMMessage GetGCMMessage ()
        {
            return GCMMessage.CreateBuilder ()
                .Enable (Enable)
                .Build ();
        }
    }
}
