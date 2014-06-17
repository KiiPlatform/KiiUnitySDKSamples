using UnityEngine;
using System.Collections;
using KiiCorp.Cloud.Unity;

public class DemoKiiInitializeBehaviour : KiiInitializeBehaviour {
	public override void Awake()
	{
		this.AppID = "a665e5b1";
		this.AppKey = "9d91a139e85c649955111dcd1dd85246";
		this.Site = KiiCorp.Cloud.Storage.Kii.Site.JP;
		base.Awake();
	}
}
