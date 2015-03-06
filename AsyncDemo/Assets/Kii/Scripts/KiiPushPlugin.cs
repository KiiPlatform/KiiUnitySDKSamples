using UnityEngine;
using KiiCorp.Cloud.Storage;
using System;
using System.Collections;

namespace KiiCorp.Cloud.Unity
{
	/// <summary>
	/// Unity plugin for push notification.
	/// </summary>
	/// <remarks>
	/// This class is not included in KiiCloudStorageSDK.dll
	/// You need to download unity plug-in.
	/// Usage.
	/// <list type="number">
	/// <item>
	/// <term>Create an empty GameObject and attach the KiiPushPlugin.cs to it.</term>
	/// </item>
	/// <item>
	/// <term>Replace 'com.example.your.application.package.name' with the value of your package name in the AndroidManifest.xml.</term>
	/// </item>
	/// <item>
	/// <term>Set the SenderID to KiiPushPlugin GameObject for GCM.</term>
	/// </item>
	/// <item>
	/// <term>Write the following code in your application.</term>
	/// </item>
	/// </list>
	/// <code>
	/// KiiPushPlugin kiiPushPlugin = GameObject.Find ("KiiPushPlugin").GetComponent&lt;KiiPushPlugin&gt; ();
	/// kiiPushPlugin.OnPushMessageReceived += (ReceivedMessage message) => {
	/// 	// This event handler is called when received the push message.
	/// 	switch (message.PushMessageType)
	/// 	{
	/// 		case ReceivedMessage.MessageType.PUSH_TO_APP:
	/// 			Debug.Log ("#####PUSH_TO_APP Message");
	/// 			break;
	/// 		case ReceivedMessage.MessageType.PUSH_TO_USER:
	/// 			Debug.Log ("#####PUSH_TO_USER Message");
	/// 			break;
	/// 		case ReceivedMessage.MessageType.DIRECT_PUSH:
	/// 			Debug.Log ("#####DIRECT_PUSH Message");
	/// 			break;
	/// 	}
	/// 	Debug.Log("Type=" + message.PushMessageType);
	/// 	Debug.Log("Sender=" + message.Sender);
	/// 	Debug.Log("Scope=" + message.ObjectScope);
	/// 	// You can get the value of custom field using GetXXXX method.
	/// 	Debug.Log("Payload=" + message.GetString("payload"));
	/// };
	/// </code>
	/// </remarks>
	public class KiiPushPlugin : MonoBehaviour {
		
		#if UNITY_IPHONE
		[System.Runtime.InteropServices.DllImport("__Internal")]
		extern static public void registerForRemoteNotifications();
		[System.Runtime.InteropServices.DllImport("__Internal")]
		extern static public void unregisterForRemoteNotifications();
		[System.Runtime.InteropServices.DllImport("__Internal")]
		extern static public void setListenerGameObject(string listenerName);
		[System.Runtime.InteropServices.DllImport("__Internal")]
		extern static public string getLastMessage();
		#elif UNITY_ANDROID
		private static AndroidJavaObject kiiPush = null;
		#endif

		/// <summary>
		/// Represents the method that will handle the event when registered the push notification.
		/// </summary>
		/// <param name="token">iOS : Device Token, Android : Registration ID.</param>
		/// <param name="e">If exception is null, execution is succeeded.</param>
		/// <remarks>
		/// This delegate is not included in KiiCloudStorageSDK.dll
		/// You need to download unity plug-in.
		/// </remarks>
		public delegate void KiiRegisterPushCallback(string token, Exception e);
		/// <summary>
		/// Represents the method that will handle the event when received the push message.
		/// </summary>
		/// <param name="message">The received push message.</param>
		/// <remarks>
		/// This delegate is not included in KiiCloudStorageSDK.dll
		/// You need to download unity plug-in.
		/// </remarks>
		public delegate void KiiPushMessageReceivedCallback(ReceivedMessage message);
		/// <summary>
		/// Represents the method that will handle the event when unregistered the push notification.
		/// </summary>
		/// <param name="e">If exception is null, execution is succeeded.</param>
		/// <remarks>
		/// This delegate is not included in KiiCloudStorageSDK.dll
		/// You need to download unity plug-in.
		/// </remarks>
		public delegate void KiiUnregisterPushCallback(Exception e);
		
		/// <summary>
		/// This setting is needed only on Android.
		/// </summary>
		/// <remarks></remarks>
		[SerializeField]
		public string SenderID;

		/// <summary>
		/// Occurs when push message received.
		/// This event doesn't occur while application is in the background.
		/// Application returns to foreground state, OnPushMessageReceived is called.
		/// This event called on UI thread.
		/// </summary>
		/// <remarks></remarks>
		public event KiiPushMessageReceivedCallback OnPushMessageReceived;

		/// <summary>
		/// Initializes a new instance of the <see cref="KiiCorp.Cloud.Storage.KiiPushPlugin"/> class.
		/// </summary>
		/// <remarks></remarks>
		public KiiPushPlugin()
		{
		}

		void Awake()
		{
			Debug.Log ("#####KiiPushReceiver.Awake");
			DontDestroyOnLoad(this);
			this.InitializeKiiPush ();
		}
		void Start ()
		{
			Debug.Log ("#####KiiPushReceiver.Start");
		}
		
		#if UNITY_IPHONE
		KiiRegisterPushCallback callback;
		private void InitializeKiiPush()
		{
			setListenerGameObject(this.gameObject.name);
		}
		public void RegisterPush(KiiRegisterPushCallback callback)
		{
			this.callback = callback;
			registerForRemoteNotifications();
		}
		void OnDidRegisterForRemoteNotificationsWithDeviceToken(string deviceToken)
		{
			Debug.Log ("#####KiiPush Device Token :" + deviceToken);
			this.callback (deviceToken, null);
		}
		void OnDidFailToRegisterForRemoteNotificationsWithError(string error)
		{
			this.callback (null, new Exception(error));
		}
		/// <summary>
		/// Unregisters the push.
		/// </summary>
		/// <param name="callback">Callback delegate. If exception is null, execution is succeeded.</param>
		/// <remarks></remarks>
		public void UnregisterPush(KiiUnregisterPushCallback callback)
		{
			unregisterForRemoteNotifications ();
			callback (null);
		}
		#elif UNITY_ANDROID
		KiiRegisterPushCallback registerCallback;
		KiiUnregisterPushCallback unregisterCallback;
		private void InitializeKiiPush()
		{
			try
			{
				if (kiiPush != null)
				{
					return;
				}
				using(var pluginClass = new AndroidJavaClass("com.kii.cloud.unity.KiiPushUnityPlugin"))
				kiiPush = pluginClass.CallStatic<AndroidJavaObject>("getInstance");
				
				kiiPush.Call("setListenerGameObjectName", this.gameObject.name);
				kiiPush.Call("setSenderId", this.SenderID);
			}
			catch (Exception e)
			{
				Debug.Log("#####failed to initialize KiiPushReceiver");
				Debug.Log(e.Message);
				Debug.Log(e.StackTrace);
			}
		}
		/// <summary>
		/// Registers the push.
		/// </summary>
		/// <param name="callback">Callback delegate. If exception is null, execution is succeeded.</param>
		/// <remarks></remarks>
		public void RegisterPush(KiiRegisterPushCallback callback)
		{
			try
			{
				this.registerCallback = callback;
				kiiPush.Call ("getRegistrationID");
			}
			catch (Exception e)
			{
				callback (null, e);
			}
		}
		void OnRegisterPushSucceeded(string registrationId)
		{
			if (this.registerCallback != null)
			{
				this.registerCallback (registrationId, null);
			}
		}
		void OnRegisterPushFailed(string errorString)
		{
			if (this.registerCallback != null)
			{
				this.registerCallback (null, new Exception(errorString));
			}
		}
		/// <summary>
		/// Unregisters the push.
		/// </summary>
		/// <param name="callback">Callback delegate. If exception is null, execution is succeeded.</param>
		/// <remarks></remarks>
		public void UnregisterPush(KiiUnregisterPushCallback callback)
		{
			try
			{
				this.unregisterCallback = callback;
				kiiPush.Call ("unregisterGCM");
			}
			catch (Exception e)
			{
				callback (e);
			}
		}
		public void OnUnregisterPushSucceeded()
		{
			if (this.unregisterCallback != null)
			{
				this.unregisterCallback (null);
			}
		}
		public void OnUnregisterPushFailed(string errorString)
		{
			if (this.unregisterCallback != null)
			{
				this.unregisterCallback (new Exception(errorString));
			}
		}
		#else
		private void InitializeKiiPush()
		{
		}
		/// <summary>
		/// Registers the push.
		/// </summary>
		/// <param name="callback">Callback delegate. If exception is null, execution is succeeded.</param>
		/// <remarks></remarks>
		public void RegisterPush(KiiRegisterPushCallback callback)
		{
		}
		/// <summary>
		/// Unregisters the push.
		/// </summary>
		/// <param name="callback">Callback delegate. If exception is null, execution is succeeded.</param>
		/// <remarks></remarks>
		public void UnregisterPush(KiiUnregisterPushCallback callback)
		{
		}
		#endif

		#if UNITY_IPHONE
		public string GetLastMessage()
		{
			return getLastMessage();
		}
		#elif UNITY_ANDROID
		public string GetLastMessage()
		{
			return kiiPush.Call<string>("getLastMessage");
		}
		#else
		public string GetLastMessage()
		{
			return null;
		}
		#endif
		
		/// <summary>
		/// This method is called by the unity native plugin when received push message.
		/// Don't call this method from unity application.
		/// </summary>
		/// <param name="payload">Payload.</param>
		/// <remarks></remarks>
		public void OnPushNotificationsReceived(string payload)
		{
			try
			{
				Debug.Log ("#####OnPushNotificationsReceived");
				Debug.Log ("#####payload=" + payload);
				if (this.OnPushMessageReceived != null)
				{
					ReceivedMessage message = ReceivedMessage.Parse (payload);
					this.OnPushMessageReceived (message);
				}
				else
				{
					Debug.Log("#####WARN:Event OnPushMessageReceived is not bound");
				}
			}
			catch (Exception e)
			{
				Debug.Log("#####ERROR:" + e.Message);
			}
		}
	}
}
