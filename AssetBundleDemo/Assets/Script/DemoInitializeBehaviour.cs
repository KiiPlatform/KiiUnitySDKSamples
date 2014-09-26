using UnityEngine;
using System;
using System.Collections;
using KiiCorp.Cloud.Unity;
using KiiCorp.Cloud.Storage;

public class DemoInitializeBehaviour : KiiInitializeBehaviour {
    public override void Awake()
    {
        AppID = "de54ae03";
        AppKey = "e91aa484541206147046aee4a16c463c";
        Site = Kii.Site.JP;
        base.Awake();
    }
    void Start()
    {
        Debug.Log("##### Start Demo");
        string token = PlayerPrefs.GetString("token");
        if (string.IsNullOrEmpty(token))
        {
            KiiUser.RegisterAsPseudoUser(null, (KiiUser user, Exception e) => {
                if (e != null)
                {
                    Debug.Log("##### Failed to register user");
                    return;
                }
                PlayerPrefs.SetString("token", KiiUser.AccessToken);
                ShowLogo();
            });
        }
        else
        {
            KiiUser.LoginWithToken(token, (KiiUser user, Exception e) => {
                if (e != null)
                {
                    Debug.Log("##### Failed to login");
                    return;
                }
                ShowLogo();
            });
        }
    }
    private void ShowLogo()
    {
        Debug.Log("##### ShowLogo");
        // Get the latest asset bundle.
        KiiQuery query = new KiiQuery();
        query.SortByDesc("_modified");
        query.Limit = 1;

        Kii.Bucket("AssetBundles").Query(query, (KiiQueryResult<KiiObject> result, Exception e) => {
            if (e != null)
            {
                Debug.Log("##### Failed to query. " + e.ToString());
                return;
            }
            if (result.Count == 0)
            {
                Debug.Log("##### Cannot find asset bundle.");
                return;
            }
            string platform = null;
            #if UNITY_IPHONE
            platform = "iOS";
            #elif UNITY_ANDROID
            platform = "Android";
            #elif UNITY_WEBPLAYER
            platform = "WebPlayer";
            #endif
            string assetUrl = result[0].GetString(platform);
            Debug.Log("##### asset url=" + assetUrl);
            StartCoroutine(DownloadLogo(assetUrl));
        });
    }
    private IEnumerator DownloadLogo(string url)
    {
        Debug.Log("##### DownloadLogo");
        using (WWW www = WWW.LoadFromCacheOrDownload (url, 1))
        {
            yield return www;
            if (www.error != null)
            {
                Debug.Log("##### Failed to download asset bundle");
            }
            else
            {
                AssetBundle bundle = www.assetBundle;
                Instantiate (bundle.mainAsset);
                bundle.Unload (false);
                Debug.Log("##### Success!!");
            }
        }
    }
}
