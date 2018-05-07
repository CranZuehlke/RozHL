using System.Collections;
using System.Collections.Generic;
using HoloToolkit.Unity.SpatialMapping;
using UnityEngine;
using UnityEngine.VR.WSA.Input;
using Zuehlke.HoloLens;

public class Pointer : MonoBehaviour
{
    public MyoConnection Myo;

    public GameObject Cursor;
    private MorphArrow _morphArrow;
    public static Pointer Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        _morphArrow = Cursor.GetComponentInChildren<MorphArrow>();
        Myo.OnPoseChanged += OnMyoPose;
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
        RaycastHit hitInfo;
        if (Physics.Raycast(Myo.gameObject.transform.position, Myo.gameObject.transform.forward, out hitInfo, 30.0f, 0))
        {
            Cursor.transform.position = hitInfo.point;
        }
    }

    private void PerformTap()
    {
        var headPosition = Camera.main.transform.position;
        var gazeDirection = Camera.main.transform.forward;

        RaycastHit hitInfo;
        if (Physics.Raycast(Myo.gameObject.transform.position, Myo.gameObject.transform.forward, out hitInfo, 30.0f, 0))
        {
            _morphArrow.ArrowState = true;
        }
    }
}