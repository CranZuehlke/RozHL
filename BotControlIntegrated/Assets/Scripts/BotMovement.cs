using System.Collections;
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
    private const float angleThreshold = 5;

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
            if (!isRotating && (AngleDiff() > angleThreshold || AngleDiff() < -angleThreshold))
            {
                InvokeRepeating("RotateBot", 0.5f, 0.25f);
                isRotating = true;
            }
            else if (!isRotating && !isMoving && Vector3.SqrMagnitude(CalcDir()) > destThreshold * destThreshold)
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
        var angleDiff = AngleDiff();
#if WINDOWS_UWP
        var angleVelocity = Mathf.Abs(angleDiff) > 90 ? 1f : Mathf.Abs(angleDiff) > 30 ? 0.6f : 0.3f;
        if (angleDiff > 0)
        {
            _mqttController.SetVelocity(0, -angleVelocity);
        }
        else
        {
            _mqttController.SetVelocity(0, angleVelocity);
        }
#endif

        if ((angleDiff < angleThreshold && angleDiff > -angleThreshold) || !UpdateBotBos.Instance.IsTracked)
        {
            CancelInvoke("RotateBot");
            isRotating = false;
        }
    }

    private void MoveBot()
    {
        var distance = Vector3.Magnitude(CalcDir());
        var velocity = distance > 1f ? 1f : distance > 0.5f ? 0.5f : 0.25f;
#if WINDOWS_UWP
        _mqttController.SetVelocity(velocity, 0);
#endif

        if (distance < destThreshold || !UpdateBotBos.Instance.IsTracked)
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
