using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//public class PK_MESSAGE : Packet
//{

//    public override byte[] DeserialazingApply(byte[] data)
//    {
//        byte[] recvBufferLength = new byte[4];
//        Buffer.BlockCopy(data, 4, recvBufferLength, 0, sizeof(int));
//        int bufferLength = BitConverter.ToInt32(recvBufferLength, 0);

//        byte[] recvBuffer = new byte[bufferLength];
//        Buffer.BlockCopy(data, 8, recvBuffer, 0, bufferLength);
//        return recvBuffer;
//    }
//}
