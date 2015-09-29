using UnityEngine;
using System;
using System.Collections.Generic;
using JsonOrg;
using KiiCorp.Cloud;
using KiiCorp.Cloud.Storage;
using System.Runtime.InteropServices;

namespace KiiCorp.Cloud.Storage.Connector
{

    /// <summary>
    /// This callback is used when login process is finished.
    /// </summary>
    /// <remarks>
    /// This callback is used when KiiSocialNetworkConnector.LogIn(Provider,
    /// KiiUserCallback) is finished.
    /// </remarks>
    /// <param name='user'>
    /// Social network to use to login KiiCloud.
    /// </param>
    /// <param name='provider'>
    /// Social network to use to login KiiCloud.
    /// </param>
    /// <param name='exception'>
    /// Social network to use to login KiiCloud.
    /// </param>
    public delegate void KiiSocialCallback(
            KiiUser user,
            Provider provider,
            Exception exception);

    /// <summary>
    /// Provides API that allows user to authenticate on KiiCloud through
    /// various social networks.
    /// </summary>
    /// <remarks>
    /// This class can be used after Kii.Initialize(string, string, Kii.Site).
    /// </remarks>
    public class KiiSocialNetworkConnector : MonoBehaviour
    {

        private Rect displayArea = new Rect(0, 0, Screen.width, Screen.height);
        private KiiSocialCallback callback = null;
        private Provider provider;

#if UNITY_IPHONE
        private IntPtr connector;

        [DllImport("__Internal")]
        private static extern IntPtr
            _KiiIOSSocialNetworkConnector_StartAuthentication(
                string gameObjectName,
                string accessUrl,
                string endPointUrl,
                float x,
                float y,
                float width,
                float height);
        [DllImport("__Internal")]
        private static extern void
            _KiiIOSSocialNetworkConnector_Destroy(IntPtr instance);
#endif

        /// <summary>
        /// Initializes a new instance of KiiSocialNetworkConnector.
        /// </summary>
        /// <remarks>
        /// This constructor is called by UnityGameEngine. Do not use it from
        /// your application.
        /// </remarks>
        public KiiSocialNetworkConnector()
        {
        }

        /// <summary>
        /// Display area of web page of social connector.
        /// </summary>
        /// <remarks>
        /// Web page of social connector is displayed as full screen, if
        /// applications do not set this field. DisplayArea is only effective
        /// for iOS environment. In Android environment, web page is shown as
        /// alert dialog.
        /// </remarks>
        /// <value>
        /// Display area.
        /// </value>
        public Rect DisplayArea
        {
            get
            {
                return this.displayArea;
            }
            set
            {
                this.displayArea = value;
            }
        }

        /// <summary>
        /// Login with the social network.
        /// </summary>
        /// <remarks>
        /// If there is already logged in user, perform logout and login with
        /// credentials given by user.
        /// </remarks>
        /// <param name='provider'>
        /// Social network to use to login KiiCloud.
        /// </param>
        /// <param name='callback'>
        /// callback notifies events asynchronously. must not be null.
        /// </param>
        /// <exception cref='ArgumentException'>
        /// Exception is thrown when one or more arguments are invalid.
        /// </exception>
        /// <exception cref='ArgumentNullException'>
        /// Exception is thrown when one or more arguments are null.
        /// </exception>
        /// <exception cref='NotSupportedException'>
        /// Exception is thrown when the specified provider is not supported.
        /// </exception>
        public void LogIn(
                Provider provider,
                KiiSocialCallback callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException("callback must not be null.");
            }
            if (!Enum.IsDefined(typeof(Provider), provider))
            {
                throw new ArgumentException("invalid provider");
            }
            if (provider == Provider.QQ)
            {
                throw new NotSupportedException("QQ is not supported.");
            }
            this.callback = callback;
            this.provider = provider;

#if UNITY_ANDROID
            AndroidJavaClass c = new AndroidJavaClass(
                    "com.kii.cloud.storage.social.webview.WebViewDialog");
            c.CallStatic("showDialog", this.gameObject.name,
                    GetStartPointUrl(), GetEndPointUrl());
#elif UNITY_IPHONE
            this.connector = _KiiIOSSocialNetworkConnector_StartAuthentication(
                    this.gameObject.name, GetStartPointUrl(), GetEndPointUrl(),
                    this.DisplayArea.x, this.DisplayArea.y,
                    this.DisplayArea.width, this.DisplayArea.height);
#endif
        }
        void OnDestroy() {
#if UNITY_IPHONE
            _KiiIOSSocialNetworkConnector_Destroy(this.connector);
            this.connector = IntPtr.Zero;
#endif
        }
        /// <summary>
        /// This method is called by native plugin.
        /// </summary>
        /// <param name="result">Authentication result as JSON format.</param>
        /// <remarks>Do not use it from your application.</remarks>
        private void OnSocialAuthenticationFinished(string result)
        {
            try
            {
                JsonObject json = new JsonObject(result);
                string type = json.GetString("type");
                // check type.
                switch (type)
                {
                    case "finished":
                        // success case. kii user was created.

                        // callback is not needed to check null. That is done
                        // in LogIn method.
                        KiiUser user = CreateCurrentKiiUser(ParseUrl(json.GetJsonObject(
                            "value").GetString("url")));
                        user.Refresh((KiiUser usr, Exception e) => {
                            this.callback(usr, this.provider, null);
                        });
                        return;
                    case "error":
                        throw new NativeInteractionException(
                            json.GetJsonObject("value").GetString("message"));
                    case "retry":
                        throw new ServerConnectionException(
                            "Server connection is failed. Please retry later");
                    case "canceled":
                        throw new UserCancelException("");
                    default:
                        // SDK programming error.
                        throw new SocialException("Unknown type = " + type);
                }

            }
            catch (SocialException e)
            {
                // callback is not needed to check null. That is done
                // in LogIn method.
                this.callback(null, this.provider, e);
            }
            catch (JsonException e)
            {
                // callback is not needed to check null. That is done
                // in LogIn method.
                this.callback(null, this.provider, new SocialException(
                            "fail to pase server response as json", e));
            }
            catch (Exception e)
            {
                // callback is not needed to check null. That is done
                // in LogIn method.
                this.callback(null, this.provider, new SocialException(
                            "unexpected error", e));
            }
        }

        private KiiUser CreateCurrentKiiUser(
            Dictionary<string, object> queryParameters)
        {
            string succeeded = (string)queryParameters["kii_succeeded"];
            if (succeeded == "false")
            {
                // server error. fail to login.
                SocialException exception = null;
                string errorCode = (string)queryParameters["kii_error_code"];
                if ("UNAUTHORIZED" == errorCode)
                {
                    exception = new OAuthException(
                        "authorization credentials are rejected by provider");
                }
                else
                {
                    exception = new SocialException(
                        "login failed with error: " + errorCode);
                }
                throw exception;
            }

            if (succeeded != "true")
            {
                // server error. invalid response.
                throw new SocialException("unknown kii_succeed: " + succeeded);
            }

            KiiUser retval = KiiUser.CreateByUri(
                new Uri(Utils.Path("kiicloud://", "users", GetNotEmptyString(
                                    queryParameters, "kii_user_id"))));
            Dictionary<string, object> dictionary =
                new Dictionary<string, object>() {
                    { KiiUser.SocialResultParams.PROVIDER_USER_ID,
                      GetNotEmptyString(queryParameters, "provider_user_id")},
                    { KiiUser.SocialResultParams.PROVIDER, this.provider },
                    { KiiUser.SocialResultParams.KII_NEW_USER,
                      queryParameters["kii_new_user"] }
            };

            // oauth_token and oauth_token_secret are optinal fields.
            if (queryParameters.ContainsKey("oauth_token"))
            {
                dictionary.Add(KiiUser.SocialResultParams.OAUTH_TOKEN,
                        queryParameters["oauth_token"]);
            }
            if (queryParameters.ContainsKey("oauth_token_secret"))
            {
                dictionary.Add(KiiUser.SocialResultParams.OAUTH_TOKEN_SECRET,
                        queryParameters["oauth_token_secret"]);
            }
            if (queryParameters.ContainsKey("kii_expires_in"))
            {
                dictionary.Add("kii_expires_in",
                        queryParameters["kii_expires_in"]);
            }
            if (queryParameters.ContainsKey("oauth_token_expires_in")) {
                dictionary.Add("oauth_token_expires_in",
                    queryParameters["oauth_token_expires_in"]);
            }
            _KiiInternalUtils.SetSocialAccessTokenDictionary(retval,
                    dictionary);

            _KiiInternalUtils.SetCurrentUser(retval);
            _KiiInternalUtils.UpdateAccessToken(GetNotEmptyString(
                        queryParameters, "kii_access_token"));

            return retval;
        }

        private static Dictionary<string, object> ParseUrl(string url)
        {
            Dictionary<string, object> retval =
                    new Dictionary<string, object>();
            int questionIndex = url.IndexOf('?');
            if (questionIndex < 0)
            {
                throw new SocialException("Fail to parse url.");
            }
            string queryString = url.Substring(questionIndex + 1);
            string[] queryParams = queryString.Split('&');
            foreach (string queryParam in queryParams)
            {
                string[] keyValue = queryParam.Split('=');
                if (keyValue.Length == 2)
                {
                    if (keyValue[0] == "kii_new_user")
                    {
                        retval.Add(keyValue[0], Convert.ToBoolean(keyValue[1]));
                    }
                    else
                    {
                        retval.Add(keyValue[0], keyValue[1]);
                    }
                }
            }
            return retval;
        }

        private string GetStartPointUrl() {
            UriBuilder builder = new UriBuilder(Utils.Path(Kii.KiiAppsBaseUrl,
                    "apps", Kii.AppId, "integration", "webauth", "connect"));
            builder.Query = "id=" + GetProviderName();
            return builder.Uri.ToString();
        }

        private string GetEndPointUrl() {
            return Utils.Path(Kii.KiiAppsBaseUrl, "apps", Kii.AppId,
                    "integration", "webauth", "result");
        }

        private string GetProviderName() {
            switch (this.provider) {
                case Provider.FACEBOOK:
                    return "facebook";
                case Provider.TWITTER:
                    return "twitter";
                case Provider.LINKEDIN:
                    return "linkedin";
                case Provider.YAHOO:
                    return "yahoo";
                case Provider.GOOGLEPLUS:
                    return "googleplus";
#pragma warning disable 0618
                case Provider.GOOGLE:
                    return "google";
#pragma warning restore 0618
                case Provider.DROPBOX:
                    return "dropbox";
                case Provider.BOX:
                    return "box";
                case Provider.RENREN:
                    return "renren";
                case Provider.SINA:
                    return "sina";
                case Provider.LIVE:
                    return "live";
                case Provider.KII:
                    return "kii";
                default:
                    throw new SystemException("unexpected error provider=" +
                            this.provider.ToString());
            }
        }

        private static string GetNotEmptyString(
                Dictionary<string, object> dictionary,
                string key)
        {
            string retval = (string)dictionary[key];
            if (string.IsNullOrEmpty(retval))
            {
                throw new SocialException("value for " + key +
                        " is null or empty");
            }
            return retval;
        }
    }
}
