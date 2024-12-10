using ServerUtil.Header;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PacketProcessor
{
    //��Ŷ ���� ����
    //public List<Packet> packetData = new List<Packet>(10);
    private LoginPacket PK_login = new LoginPacket();


    private readonly Dictionary<HeaderType, Packet> packetData = new Dictionary<HeaderType, Packet>();

    Buffer_Converter bufferCon = new Buffer_Converter();

    //HeaderType : ServerUtil.Header.HeaderType
    private readonly Dictionary<HeaderType, Action<byte[], Packet>> handlers = new Dictionary<HeaderType, Action<byte[], Packet>>();
    

    public PacketProcessor()
    {
        packetData[HeaderType.ACCEPT] = PK_login;

        handlers[HeaderType.ACCEPT] = HandleLoginProcesse;
    }

    public void ProcessBuffer(byte[] buffer)
    {
        Debug.Log("ProcessBuffer ����");
        HeaderType header = (HeaderType)bufferCon.GetHeaderType(buffer);

        if (handlers.TryGetValue(header, out var handler))
        {
            handler.Invoke(buffer, packetData[header]);
        }
        else
        {
            Debug.LogWarning($"�� �� ���� �޽��� Ÿ��: {header}");
        }
    }

    public void HandleLoginProcesse(byte[] buffer, Packet packet)
    {
        Debug.Log("HandleLoginProcesse ����");
        byte[] result = new byte[256];
        result = packet.DeSerialzed(buffer);
        Debug.Log(Encoding.UTF8.GetString(result));

        if(ServerConnect.Instance.currentState == ConnectionState.LOGIN_SUCCESS)
        {
            SceneManager.LoadScene("MatchingScene");
            Debug.Log("�α��� ����");
        }
    }
}
