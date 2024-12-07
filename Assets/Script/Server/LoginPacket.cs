using ServerUtil.Header;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

public class LoginPacket : Packet
{
    

    public override byte[] DeserialazingApply(byte[] data)
    {
        throw new System.NotImplementedException();
    }

    public override byte[] DeSerialzed(byte[] buffer)
    {
        char[] dataValue = new char[20];
        _packetHeader.headerType = ServerUtil.Header.HeaderType.ACCEPT;
        _packetHeader.userId = Encoding.ASCII.GetBytes(dataValue);

        throw new System.NotImplementedException();
    }

    public override byte[] Serialzed(string buffer)
    {
        byte[] byteState = BitConverter.GetBytes((int)ServerConnect.Instance.currentState);

        byte[] header = PackingHeader(ServerUtil.Header.HeaderType.ACCEPT, buffer.Length + byteState.Length);

        byte[] data = Encoding.ASCII.GetBytes(buffer);

        byte[] result = new byte[header.Length + data.Length + byteState.Length];

        Buffer.BlockCopy(header, 0, result, 0, header.Length);                      // PK_Data

        Buffer.BlockCopy(byteState, 0, result, header.Length, byteState.Length);

        Buffer.BlockCopy(data, 0, result, header.Length + byteState.Length, data.Length); // 메시지 길이

        return result;
    }

}
