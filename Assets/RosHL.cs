using System.Collections;
using System.Collections.Generic;
using HoloToolkit.Unity.InputModule;
using UnityEngine;
using Controller;
using UnityEngine.VR.WSA.Input;
using System;

public class RosHL : MonoBehaviour
{

#if WINDOWS_UWP
    private MQTTController _mqttController;
#endif

    void Start()
    {
#if WINDOWS_UWP
        MainController.Instance.Start();
        _mqttController = MainController.Instance.MQTTController;
#endif
        GestureRecognizer gestureRecognizer = new GestureRecognizer();
        gestureRecognizer.TappedEvent += gestureRecognizer_TapEvent;
        gestureRecognizer.StartCapturingGestures();

    }

    private void gestureRecognizer_TapEvent(InteractionSourceKind source, int tapCount, Ray headRay)
    {
#if WINDOWS_UWP
        _mqttController.SetVelocity(1, 1); 
#endif
    }

    // Update is called once per frame
    void Update()
    {

    }
}
