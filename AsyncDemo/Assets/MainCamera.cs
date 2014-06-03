using KiiCorp.Cloud.Storage;

using UnityEngine;
using System.Collections;

public class MainCamera : MonoBehaviour {

//	private const string APP_ID = "e8e2563e";
//	private const string APP_KEY = "a084a2fe9957f78cc6aeae264000eb12";
//	private const Kii.Site APP_SITE = Kii.Site.US;

	private IPage currentPage;
	private Stack pageStack = new Stack();

	public GUISkin DemoGUISkin;

	// Use this for initialization
	void Start () {
		currentPage = new TitlePage(this);
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnGUI() {
		GUI.skin = DemoGUISkin;

		currentPage.OnGUI();
	}

	public void PushPage(IPage next)
	{
		pageStack.Push(currentPage);
		currentPage = next;
	}

	public void PopPage()
	{
		if (pageStack.Count == 0)
		{
			Debug.Log("Stack is empty!");
			return;
		}
		currentPage = (IPage)pageStack.Pop();
	}
}
