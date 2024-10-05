using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//namespace Server.Packet
//{
    public abstract class Packet
    {

        //패킷을 상속받는 자식에서 
        public abstract byte[] DeserialazingApply(byte[] data);

    }
//}
