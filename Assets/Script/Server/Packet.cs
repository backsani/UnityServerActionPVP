using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//namespace Server.Packet
//{
    public abstract class Packet
    {

        //��Ŷ�� ��ӹ޴� �ڽĿ��� 
        public abstract byte[] DeserialazingApply(byte[] data);

    }
//}
