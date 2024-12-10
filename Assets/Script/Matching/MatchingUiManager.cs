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
        Debug.Log("TryMatchButton");
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
