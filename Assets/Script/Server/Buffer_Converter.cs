using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class Buffer_Converter : MonoBehaviour
{
    public int GetHeaderType(byte[] buffer)
    {
        byte[] dataHeaderType = new byte[Marshal.SizeOf(typeof(ServerUtil.Header.HeaderType))];

        Buffer.BlockCopy(buffer, Marshal.SizeOf(typeof(int)), dataHeaderType, 0, dataHeaderType.Length);

        return Convert.ToInt32(dataHeaderType);
    }
}
