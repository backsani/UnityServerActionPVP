using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MatchingUiManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _userName;

    // Start is called before the first frame update
    void Start()
    {
        _userName.text = ServerConnect.Instance.UserId;
    }

    public void TryMatchButton()
    {
        ServerConnect.Instance.EnqueueSendData(ServerConnect.Instance.packetData[(int)ServerUtil.Header.HeaderType.ACCEPT].Serialzed(ServerUtil.Header.ConnectionState.MATCH_REQUEST.ToString()));
    }

    public void TryInfoButton()
    {
        Debug.Log("TryInfoButton");
    }

    public void TryExitButton()
    {
        ServerConnect.Instance.DisconnectServer();
    }
}
