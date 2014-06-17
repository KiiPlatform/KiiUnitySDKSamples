using UnityEngine;
using System.Collections;
using KiiCorp.Cloud.Unity;

public class DemoKiiInitializeBehaviour : KiiInitializeBehaviour {
	public override void Awake()
	{
		this.AppID = "f39c2d34";
		this.AppKey = "2e98ef0bb78a58da92f9ac0709dc99ed";
		this.Site = KiiCorp.Cloud.Storage.Kii.Site.JP;
		base.Awake();
	}
}
