// ------------------------------------------------------------------------------
//  <autogenerated>
//      This code was generated by a tool.
//      Mono Runtime Version: 4.0.30319.1
// 
//      Changes to this file may cause incorrect behavior and will be lost if 
//      the code is regenerated.
//  </autogenerated>
// ------------------------------------------------------------------------------

using KiiCorp.Cloud.Storage;

using UnityEngine;
using System;
using System.Collections.Generic;

public class BucketPage : BasePage, IPage
{
	private KiiBucket bucket;

	// query
	private Rect queryButtonRect = new Rect(0, 128, 320, 64);

	// Create object
	private Rect createButtonRect = new Rect(0, 192, 320, 64);

	// Delete bucket
	private Rect deleteButtonRect = new Rect(320, 192, 320, 64);

	private bool buttonEnable = true;
	private IList<KiiObject> objectList = new List<KiiObject>();

	public BucketPage (MainCamera camera, KiiBucket bucket) : base(camera)
	{
		this.bucket = bucket;
	}

	#region IPage implementation
	public void OnGUI ()
	{
		GUI.Label(messageRect, message);

		GUI.enabled = buttonEnable;
		bool backClicked = GUI.Button(backButtonRect, "<");
		bool queryClicked = GUI.Button(queryButtonRect, "Query");
		bool createClicked = GUI.Button(createButtonRect, "Create");
		bool deleteClicked = GUI.Button(deleteButtonRect, "Delete");
		for (int i = 0 ; i < objectList.Count ; ++i)
		{
			Uri uri = objectList[i].Uri;
			if (uri == null)
			{
				continue;
			}
			if (GUI.Button(new Rect(0, i * 64 + 256, 640, 64), uri.ToString()))
			{
				// object page
				ShowObjectPage(objectList[i]);
				return;
			}
		}

		GUI.enabled = true;

		if (backClicked)
		{
			PerformBack();
			return;
		}
		if (queryClicked)
		{
			PerformQuery();
			return;
		}
		if (createClicked)
		{
			PerformCreate();
			return;
		}
		if (deleteClicked)
		{
			PerformDelete();
			return;
		}
	}
	
	void PerformQuery ()
	{
		message = "Query...";
		ButtonEnabled = false;

		KiiQuery query = new KiiQuery();
		bucket.Query(query, (KiiQueryResult<KiiObject> list, Exception e) =>
		{
			ButtonEnabled = true;
			if (e != null)
			{
				message = "Failed to query " + e.ToString();
				return;
			}
			objectList = list;
			message = "Query succeeded";
		});
	}

	void PerformCreate ()
	{
		message = "Creating object...";
		ButtonEnabled = false;

		bucket.NewKiiObject().Save((KiiObject obj, Exception e) =>
		{
			buttonEnable = true;
			if (e != null)
			{
				message = "Failed to create object " + e.ToString();
				return;
			}
			objectList.Add(obj);
			message = "Create object succeeded";
		});
	}

	void PerformDelete ()
	{
		message = "Deleting bucket...";
		ButtonEnabled = false;

		bucket.Delete((KiiBucket deletedBucket, Exception e) =>
		{
			buttonEnable = true;
			if (e != null)
			{
				message = "Failed to delete bucket " + e.ToString();
				return;
			}
			PerformBack();
		});

	}

	void ShowObjectPage (KiiObject obj)
	{
		camera.PushPage(new ObjectPage(camera, obj));
	}

	#endregion

}

