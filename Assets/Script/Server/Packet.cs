using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ServerUtil.Header
{
    public enum HeaderType
    {
        ACCEPT,
        NEWTIME
    };

    public enum ConnectionState
    {
        INIT,
        LOGIN,
        SIGNUP,
        LOGIN_SUCCESS,
        SIGNUP_SUCCESS,
        LOGIN_FAIL,
        SIGNUP_FAIL
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


    //��Ŷ�� ��ӹ޴� �ڽĿ��� 
    public abstract byte[] DeserialazingApply(byte[] data);

    protected byte[] PackingHeader(ServerUtil.Header.HeaderType headerType, int bufferSize)
    {
        byte[] byteUserId = new byte[ServerConnect.Instance.IdLength];

        byte[] data = Encoding.ASCII.GetBytes(ServerConnect.Instance.UserId);

        Array.Copy(data, 0, byteUserId, 0, data.Length);

        if (data.Length < byteUserId.Length)
        {
            for (int i = data.Length; i < byteUserId.Length; i++)
            {
                byteUserId[i] = 0x20;  // ���� ���� (' ')�� ASCII ��
            }
        }

        byte[] byteheader = BitConverter.GetBytes((int)headerType);
        int Length = 4 + byteheader.Length + byteUserId.Length;

        byte[] byteLength = BitConverter.GetBytes(Length + bufferSize);

        byte[] result = new byte[Length];

        Buffer.BlockCopy(byteLength, 0, result, 0, byteLength.Length);                      // PK_Data
        Buffer.BlockCopy(byteheader, 0, result, byteLength.Length, byteheader.Length); // �޽��� ����
        Buffer.BlockCopy(byteUserId, 0, result, byteLength.Length + byteheader.Length, byteUserId.Length);  // ���� �޽���

        return result;
    }
}


