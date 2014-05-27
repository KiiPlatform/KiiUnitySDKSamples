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
	private KiiExperiment experiment;
	private Variation appliedVariation;

	void Start () {
		Kii.Initialize ("085c7201", "84868757e531e11d088a5bf61589fa13", "http://qa21.internal.kii.com/api");
		KiiAnalytics.Initialize ("085c7201", "84868757e531e11d088a5bf61589fa13", "http://qa21.internal.kii.com/api", GetDeviceID());
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
				try
				{
					KiiUser user = KiiUser.BuilderWithEmail (this.email).WithName ("U" + Environment.TickCount).Build ();
					user.Register ("pa$$sword");
					this.message = "SUCCESS";
					this.loggedin = true;
					this.message = "";
				}
				catch (KiiCorp.Cloud.Storage.NetworkException e)
				{
					this.message = "ERROR: " + e.GetType () + "\n" +
						"Status=" + e.Status + "\n" +
						"Data=" + e.Data.ToString() + "\n" +
						"InnerExcepton=" + e.InnerException.GetType() + "\n" +
						"InnerExcepton.Message=" + e.InnerException.Message + "\n" +
						"InnerExcepton.Stacktrace=" + e.InnerException.StackTrace + "\n" +
						"Source=" + e.Source + "\n" +
						e.Message + "\n" + e.StackTrace;
				}
				catch (Exception e)
				{
					this.message = "ERROR: " + e.GetType () + " " + e.Message + "\n" + e.StackTrace;
				}
			}
		}
		else
		{
			GUI.Label (new Rect (10, 220, 500, 1000), message);
			try
			{
				if (this.experiment == null)
				{
					this.experiment = KiiExperiment.GetByID ("8a835c5b-3508-41a9-9a7e-a348c513a426");
					this.appliedVariation = experiment.GetAppliedVariation(experiment.Variations[0]);
					ConversionEvent viewCnvEvent = this.experiment.GetConversionEventByName("viewed");
					KiiEvent viewKiiEvent = this.appliedVariation.EventForConversion(viewCnvEvent);
					KiiAnalytics.Upload(viewKiiEvent);
				}
				if (this.buttonCaption == null)
				{
					JsonObject json = this.appliedVariation.VariableSet;
					this.buttonCaption = json.GetString("caption");
				}

				if (GUI.Button (new Rect (10, 10, 250, 100), this.buttonCaption))
				{
					ConversionEvent cnvEvent = experiment.GetConversionEventByName("clicked");
					KiiEvent kiiEvent = this.appliedVariation.EventForConversion(cnvEvent);
					KiiAnalytics.Upload(kiiEvent);
					Application.Quit();
				}
				if (GUI.Button (new Rect (265, 10, 250, 100), "Cancel"))
				{
					Application.Quit();
				}
			}
			catch (Exception e)
			{
				this.message = "ERROR: " + e.GetType () + " " + e.Message + "\n" + e.StackTrace;
			}
		}
	}
	string GetDeviceID()
	{
		string deviceID = ReadDeviceIDFromStorage();
		if (deviceID == null)
		{
			deviceID = Guid.NewGuid().ToString();
			SaveDeviceID(deviceID);
		}
		return deviceID;
	}
	string ReadDeviceIDFromStorage()
	{
		string id = PlayerPrefs.GetString("deviceId", null);
		if (id == null || id.Length == 0)
		{
			id = System.Guid.NewGuid().ToString();
		}
		return id;
	}
	void SaveDeviceID(string id)
	{
		PlayerPrefs.SetString("deviceId", id);
		PlayerPrefs.Save();
	}
}
