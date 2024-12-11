using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LoginManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField _userId;


    public void TryLogin()
    {
        ServerConnect.Instance.UserId = _userId.text;
        ServerConnect.Instance.currentState = ServerUtil.Header.ConnectionState.LOGIN;

        ServerConnect.Instance.EnqueueSendData( ServerConnect.Instance.packetData[(int)ServerUtil.Header.HeaderType.ACCEPT].Serialzed(_userId.text));
    }
}
