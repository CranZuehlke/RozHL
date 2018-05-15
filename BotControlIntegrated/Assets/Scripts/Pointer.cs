using System.Collections;
using System.Collections.Generic;
using HoloToolkit.Unity.SpatialMapping;
using UnityEngine;
using Zuehlke.HoloLens;

public class Pointer : MonoBehaviour
{
    public MyoInterface Myo;

    public GameObject Cursor;
    private MorphArrow _morphArrow;
    public static Pointer Instance { get; private set; }

    private MyoConnection.Pose _lastPose = MyoConnection.Pose.Unknown;

    void Awake()
    {
        Instance = this;
        _morphArrow = Cursor.GetComponentInChildren<MorphArrow>();
    }

    private void OnMyoPose(MyoConnection.Pose newpose)
    {
        if (newpose == MyoConnection.Pose.Fist && Myo.GetComponent<MyoSync>().InSync)
        {
            PerformTap();
        }
    }

    void Update()
    {
        if (!_morphArrow.ArrowState)
        {
            RaycastHit hitInfo;
            if (Physics.Raycast(new Ray(Myo.gameObject.transform.position, Myo.gameObject.transform.forward), out hitInfo))
            {
                Cursor.transform.position = hitInfo.point;
            }
        }
        if (Myo.Pose != _lastPose)
        {
            OnMyoPose(Myo.Pose);
            _lastPose = Myo.Pose;
        }
    }

    private void PerformTap()
    {
        RaycastHit hitInfo;
        if (Physics.Raycast(new Ray(Myo.gameObject.transform.position, Myo.gameObject.transform.forward), out hitInfo))
        {
            _morphArrow.ArrowState = true;
        }
    }
}