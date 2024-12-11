using ServerUtil.Header;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

namespace ServerUtil.Header
{
    public enum HeaderType
    {
        ACCEPT,
        MATCH,
        INGAME,
        NULL = 99
    };

    public enum ConnectionState
    {
        INIT,
        LOGIN,
        SIGNUP,
        LOGIN_SUCCESS,
        SIGNUP_SUCCESS,
        LOGIN_FAIL,
        SIGNUP_FAIL,
        MATCH_REQUEST,
        MATCH_FIND,
        MATCH_ACCEPT,
        MATCH_REFUSE
    };
}


public class PacketHeader
{
    public int Length;
    public ServerUtil.Header.HeaderType headerType;
    public byte[] userId = new byte[20];
};


public abstract class Packet
{
    protected PacketHeader _packetHeader;

    public abstract byte[] Serialzed(string buffer);
    public abstract byte[] DeSerialzed(byte[] buffer);

    public Packet()
    {
        _packetHeader = new PacketHeader();
        _packetHeader.Length = 0;
        _packetHeader.headerType = HeaderType.NULL;
        Debug.Log("매개변수를 받는 Packet 생성자 호출");
    }

    //패킷을 상속받는 자식에서 

    protected byte[] PackingHeader(ServerUtil.Header.HeaderType headerType, int bufferSize)
    {
        byte[] byteUserId = new byte[ServerConnect.Instance.IdLength];

        byte[] data = Encoding.ASCII.GetBytes(ServerConnect.Instance.UserId);

        Array.Copy(data, 0, byteUserId, 0, data.Length);

        if (data.Length < byteUserId.Length)
        {
            for (int i = data.Length; i < byteUserId.Length; i++)
            {
                byteUserId[i] = 0x20;  // 공백 문자 (' ')의 ASCII 값
            }
        }

        byte[] byteheader = BitConverter.GetBytes((int)headerType);
        int Length = 4 + byteheader.Length + byteUserId.Length;

        byte[] byteLength = BitConverter.GetBytes(Length + bufferSize);

        byte[] result = new byte[Length];

        Buffer.BlockCopy(byteLength, 0, result, 0, byteLength.Length);                      // PK_Data
        Buffer.BlockCopy(byteheader, 0, result, byteLength.Length, byteheader.Length); // 메시지 길이
        Buffer.BlockCopy(byteUserId, 0, result, byteLength.Length + byteheader.Length, byteUserId.Length);  // 실제 메시지

        return result;
    }

    protected int UnpackingHeader(byte[] data)
    {
        byte[] dataLength = new byte[sizeof(int)];
        byte[] dataHeaderType = new byte[sizeof(int)];

        Buffer.BlockCopy(data, 0, dataLength, 0, dataLength.Length);
        Buffer.BlockCopy(data, dataLength.Length, dataHeaderType, 0, dataHeaderType.Length);

        _packetHeader.Length = BitConverter.ToInt32(dataLength, 0);
        _packetHeader.headerType = (ServerUtil.Header.HeaderType)BitConverter.ToInt32(dataHeaderType, 0);
        //아이디 일단 생략

        
        return dataLength.Length + dataHeaderType.Length + ServerConnect.Instance.IdLength;
    }
}


