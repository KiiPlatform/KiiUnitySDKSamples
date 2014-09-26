using System.Text;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ExportAssetBundles
{
    private const string APP_ID = "de54ae03";
    private const string APP_KEY = "e91aa484541206147046aee4a16c463c";
    private const string CLIENT_ID = "6063bfb75a5639be7251846897c17072";
    private const string CLIENT_SECRET = "b72bd0f2c754dbede01fafc5abd0f06385402d22530d9ad718c25af2c302647d";
    private const string SITE = "JP"; // US or JP or CN or SG
    private const string ASSET_BUNDLE_VERSION = "v1.0.0";
    private static Encoding enc = Encoding.GetEncoding("UTF-8");

    [MenuItem("Assets/Build AssetBundle From Selection")]
    static void ExportResource ()
    {
        string path = EditorUtility.SaveFilePanel ("Save Resource", "", "New Resource", "unity3d");
        if (path.Length != 0) {
            Object[] selection = Selection.GetFiltered (typeof(Object), SelectionMode.DeepAssets);
            // require iOS Pro, Android Pro Lisence
            // for Android
            BuildPipeline.BuildAssetBundle(Selection.activeObject,
                                            selection, path + ".android.unity3d",
                                            BuildAssetBundleOptions.CollectDependencies |
                                            BuildAssetBundleOptions.CompleteAssets,
                                            BuildTarget.Android);
            
            // for iPhone
            BuildPipeline.BuildAssetBundle(Selection.activeObject,
                                            selection, path + ".iphone.unity3d",
                                            BuildAssetBundleOptions.CollectDependencies |
                                            BuildAssetBundleOptions.CompleteAssets,
                                            BuildTarget.iPhone);
            
            // for WebPlayer
            BuildPipeline.BuildAssetBundle(Selection.activeObject,
                                            selection, path + ".unity3d",
                                            BuildAssetBundleOptions.CollectDependencies |
                                            BuildAssetBundleOptions.CompleteAssets,
                                            BuildTarget.WebPlayer);
            Dictionary<string, string> bundles = new Dictionary<string, string>();
            bundles.Add("Android", path + ".android.unity3d");
            bundles.Add("iOS", path + ".iphone.unity3d");
            bundles.Add("WebPlayer", path + ".unity3d");
            UploadAssetBundles(bundles);
            Selection.objects = selection;
        }
    }
    private static string UploadAssetBundles(Dictionary<string, string> bundles)
    {
        string token = GetAdminToken();
        JSONObject assetBundleInfo = new JSONObject();
        assetBundleInfo.AddField("ver", ASSET_BUNDLE_VERSION);
        foreach (string platform in bundles.Keys)
        {
            string url = SaveObjectAndPublishObjectBody(token, platform, bundles[platform]);
            assetBundleInfo.AddField(platform, url);
        }
        return SaveObject(token, assetBundleInfo);
    }
    private static string GetAdminToken()
    {
        JSONObject request = new JSONObject();
        request.AddField("client_id", CLIENT_ID);
        request.AddField("client_secret", CLIENT_SECRET);
        string response = SendRequestSync ("/oauth2/token", "POST", "application/json", null, null, request.ToString());
        JSONObject json = new JSONObject(response);
        return json.GetField("access_token").str;
    }
    private static string SaveObjectAndPublishObjectBody(string token, string platform, string filepath)
    {
        // Save the object
        JSONObject request = new JSONObject();
        request.AddField("platform", platform);
        string objectID = SaveObject(token, request);

        // Uplaod the AssetBundle
        FileStream stream = null;
        byte[] body = null;
        try
        {
            stream = new FileStream(filepath, FileMode.Open);
            BinaryReader reader = new BinaryReader(stream);
            body = reader.ReadBytes((int)reader.BaseStream.Length);
        }
        finally
        {
            if (stream != null)
            {
                stream.Close();
            }
        }
        SendRequestSync ("/apps/" + APP_ID + "/Asset/objects/" + objectID + "/body", "PUT", "application/vnd.unity", null, token, body);

        // Publish the AssetBundle
        string response = SendRequestSync ("/apps/" + APP_ID + "/Asset/objects/" + objectID + "/body/publish", "POST", "application/vnd.kii.objectbodypublicationrequest+json", "application/vnd.kii.objectbodypublicationresponse+json", token, (byte[])null);
        JSONObject json = new JSONObject(response);
        return json.GetField("url").str;
    }
    private static string SaveObject(string token, JSONObject body)
    {
        // Save the object
        string response = SendRequestSync ("/apps/" + APP_ID + "/Asset/objects", "POST", "application/vnd.unity+json", null, token, body.ToString());
        JSONObject json = new JSONObject(response);
        return json.GetField("objectID").str;
    }
    private static string SendRequestSync(string path, string method, string contentType, string accept, string token, string body)
    {
        return SendRequestSync(path, method, contentType, accept, token, GetBytes(body));
    }
    private static string SendRequestSync(string path, string method, string contentType, string accept, string token, byte[] body)
    {
        string url = GetApiPath () + path;
        Dictionary<string, string> headers = new Dictionary<string, string> ();
        headers.Add ("x-kii-appid", APP_ID);
        headers.Add ("x-kii-appkey", APP_KEY);
        headers.Add ("X-HTTP-Method-Override", method);
        if (contentType != null)
        {
            headers.Add ("content-type", contentType);
        }
        if (accept != null)
        {
            headers.Add ("Accept", accept);
        }
        if (token != null)
        {
            headers.Add("Authorization", "Bearer " + token);
        }
        LogCurl(method, url, headers, GetString(body));
        WWW www = new WWW (url, body, headers);
        while (!www.isDone)
        {
        }
        if (!string.IsNullOrEmpty(www.error))
        {
            Debug.Log("ERROR:" + www.error);
            throw new System.Exception(www.error);
        }
        return GetString(www.bytes);
    }
    private static void LogCurl(string method, string url, Dictionary<string, string> headers, string body)
    {
        StringBuilder curl = new StringBuilder("curl -v -X POST");
        foreach (string name in headers.Keys)
        {
            curl.Append(" -H \"" + name + ":" + headers[name] + "\"");
        }
        curl.Append(" " + url + " ");
        if (!string.IsNullOrEmpty(body))
        {
            curl.Append(" -d '" + body + "'");
        }
        Debug.Log(curl);
    }
    private static string GetApiPath()
    {
        switch (SITE)
        {
        case "US":
            return "https://api.kii.com/api";
        case "JP":
            return "https://api-jp.kii.com/api";
        case "CN":
            return "https://api-cn2.kii.com/api";
        case "SG":
            return "https://api-sg.kii.com/api";
        }
        return null;
    }
    private static byte[] GetBytes(string s)
    {
        if (s == null){
            return null;
        }
        return enc.GetBytes (s);
    }
    private static string GetString(byte[] b)
    {
        if (b == null)
        {
            return null;
        }
        return enc.GetString (b);
    }
}
