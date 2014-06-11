using UnityEngine;
using System.Collections;
using KiiCorp.Cloud.Unity;

public class DemoKiiInitializeBehaviour : KiiInitializeBehaviour {
	public override void Awake()
	{
		this.AppID = "e8e2563e";
		this.AppKey = "a084a2fe9957f78cc6aeae264000eb12";
		this.Site = KiiCorp.Cloud.Storage.Kii.Site.US;
		base.Awake();
	}
}
