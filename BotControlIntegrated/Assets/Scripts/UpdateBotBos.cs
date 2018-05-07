using HoloToolkit.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;

public class UpdateBotBos : Singleton<UpdateBotBos>, ITrackableEventHandler {

    public ImageTargetBehaviour ImageTarget;
    public bool IsTracked { get; private set; }

    void Start()
    {
        ImageTarget.RegisterTrackableEventHandler(this);
    }

    public void OnTrackableStateChanged(TrackableBehaviour.Status previousStatus, TrackableBehaviour.Status newStatus)
    {
        if(newStatus==TrackableBehaviour.Status.DETECTED|| newStatus == TrackableBehaviour.Status.TRACKED|| newStatus == TrackableBehaviour.Status.EXTENDED_TRACKED)
        {
            IsTracked = true;
        }
        else
        {
            IsTracked = false;
        }
    }

	// Update is called once per frame
	void Update () {
        if (IsTracked)
        {
            this.transform.position = ImageTarget.gameObject.transform.position;
            this.transform.rotation = ImageTarget.gameObject.transform.rotation;

        }
    }

}
