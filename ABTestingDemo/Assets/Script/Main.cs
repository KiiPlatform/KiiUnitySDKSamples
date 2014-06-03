using UnityEngine;
using System;
using System.Collections;
using JsonOrg;
using KiiCorp.Cloud.Storage;
using KiiCorp.Cloud.Analytics;
using KiiCorp.Cloud.ABTesting;

[ExecuteInEditMode()]
public class Main : MonoBehaviour {

	private string email = "";
	private string message = "";
	private string buttonCaption = null;
	private bool loggedin = false;
	private bool initializedExperiment = false;
	private KiiExperiment experiment;
	private Variation appliedVariation;

	void Start () {
//		Kii.Initialize ("085c7201", "84868757e531e11d088a5bf61589fa13", "http://qa21.internal.kii.com/api");
//		KiiAnalytics.Initialize ("085c7201", "84868757e531e11d088a5bf61589fa13", "http://qa21.internal.kii.com/api", GetDeviceID());
	}
	
	// Update is called once per frame
	void Update () {
	}

	void OnGUI() {
		if (!this.loggedin)
		{
			this.email = GUI.TextField (new Rect (10, 10, 800, 100), this.email);
			GUI.Label (new Rect (10, 220, 500, 1000), this.message);
			if (GUI.Button (new Rect (10, 115, 250, 100), "Create User"))
			{
				KiiUser user = KiiUser.BuilderWithEmail (this.email).WithName ("U" + Environment.TickCount).Build ();
				user.Register ("pa$$sword", (KiiUser registeredUser, Exception e)=>{
					if (e != null)
					{
						this.message = "ERROR: failed to register user " + e.GetType () + " " + e.Message + "\n" + e.StackTrace;
					}
					else
					{
						this.message = "SUCCESS";
						this.loggedin = true;
						this.message = "";
					}
				});
			}
		}
		else
		{
			GUI.Label (new Rect (10, 220, 500, 1000), message);
			if (!this.initializedExperiment)
			{
				this.initializedExperiment = true;
				Debug.Log("#####Call KiiExperiment.GetByID");
				KiiExperiment.GetByID ("8a835c5b-3508-41a9-9a7e-a348c513a426", (KiiExperiment experiment, Exception e)=>{
					Debug.Log("#####End KiiExperiment.GetByID");
					if (e != null)
					{
						Debug.Log("#####Error KiiExperiment.GetByID");
						this.message = "ERROR: KiiExperiment.GetByID failed!! " + e.GetType () + " " + e.Message + "\n" + e.StackTrace;
						return;
					}
					Debug.Log("#####Success KiiExperiment.GetByID");
					this.experiment = experiment;
					this.appliedVariation = this.experiment.GetAppliedVariation(this.experiment.Variations[0]);
					ConversionEvent viewCnvEvent = this.experiment.GetConversionEventByName("viewed");
					KiiEvent viewKiiEvent = this.appliedVariation.EventForConversion(viewCnvEvent);
					KiiAnalytics.Upload((Exception e1)=>{
						if (e1 != null)
						{
							this.message = "ERROR: KiiAnalytics.Upload('viewed') failed!! " + e1.GetType () + " " + e1.Message + "\n" + e1.StackTrace;
						}
						else
						{
							this.message = "Event 'viewed' is Uploaded!!";
						}
					}
					,viewKiiEvent);
					if (this.buttonCaption == null)
					{
						JsonObject json = this.appliedVariation.VariableSet;
						this.buttonCaption = json.GetString("caption");
					}
				});
			}
			if (GUI.Button (new Rect (10, 10, 250, 100), this.buttonCaption))
			{
				ConversionEvent cnvEvent = this.experiment.GetConversionEventByName("clicked");
				KiiEvent kiiEvent = this.appliedVariation.EventForConversion(cnvEvent);
				KiiAnalytics.Upload((Exception e2)=>{
					if (e2 != null)
					{
						this.message = "ERROR: KiiAnalytics.Upload(clicked) failed!! " + e2.GetType () + " " + e2.Message + "\n" + e2.StackTrace;
					}
					else
					{
						this.message = "Event 'clicked' is Uploaded!!";
					}
				}, kiiEvent);
			}
			if (GUI.Button (new Rect (265, 10, 250, 100), "Cancel"))
			{
				Application.Quit();
			}
		}
	}
}
