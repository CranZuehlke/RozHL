﻿using System.Collections;
using System.Collections.Generic;
using Controller;
using UnityEngine;
using UnityEngine.UI;

public class BotMovement : MonoBehaviour
{

    public Transform TransCursor;
    public Transform TransBot;


    private bool isMoving;
    private bool isRotating;
    private const float destThreshold = 0.1f;

#if WINDOWS_UWP
    private MQTTController _mqttController;

    // Use this for initialization
    void Start()
    {
        MainController.Instance.Start();
        _mqttController = MainController.Instance.MQTTController;
    }
#endif

    // Update is called once per frame
    void Update()
    {

        if (UpdateBotBos.Instance.IsTracked)
        {
            if (!isRotating && AngleDiff() > 1 && AngleDiff() < -1)
            {
                InvokeRepeating("RotateBot", 0.5f, 0.25f);
                isRotating = true;
            }
            else if (!isRotating && !isMoving && Vector3.SqrMagnitude(CalcDir()) < destThreshold * destThreshold)
            {
                InvokeRepeating("MoveBot", 0.5f, 0.25f);
                isMoving = true;
            }
        }
    }

    private float AngleDiff()
    {
        var botForward = TransBot.forward;
        var direction = CalcDir();
        botForward.y = 0;
        direction.y = 0;
        return Vector3.SignedAngle(botForward, direction, Vector3.up);
    }

    private void RotateBot()
    {
#if WINDOWS_UWP
        if (AngleDiff() > 0)
        {
            _mqttController.SetVelocity(0, -1f);
        }
        else
        {
            _mqttController.SetVelocity(0, 1f);
        }
#endif

        if ((AngleDiff() < 1 && AngleDiff() > -1) || !UpdateBotBos.Instance.IsTracked)
        {
            CancelInvoke("RotateBot");
            isRotating = false;
        }
    }

    private void MoveBot()
    {
#if WINDOWS_UWP
        _mqttController.SetVelocity(1f, 0);
#endif

        if (Vector3.SqrMagnitude(CalcDir()) < destThreshold * destThreshold || !UpdateBotBos.Instance.IsTracked)
        {
            CancelInvoke("MoveBot");
            isMoving = false;
        }
    }

    private Vector3 CalcDir()
    {
        return TransCursor.position - TransBot.position;
    }

}
