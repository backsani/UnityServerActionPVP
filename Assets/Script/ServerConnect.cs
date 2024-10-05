using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

enum PK_Data
{
    MESSAGE,
    MOVE,
    ATTACK,
    STATE
}

public class ServerConnect : MonoBehaviour
{
    private TcpClient socketConnection;
    private NetworkStream stream;
    private List<Packet> packetData = new List<Packet>();
    PK_MESSAGE PK_message = new PK_MESSAGE();
    [SerializeField]
    private Text message;
    
    void Start()
    {
        ConnectToTcpServer();
        packetData.Add(PK_message);
        message.text = "NullNull";
        //packetHeader.Add(PK_Data.MOVE);
        //packetHeader.Add(PK_Data.ATTACK);
        //packetHeader.Add(PK_Data.STATE);
        
    }

    private void ConnectToTcpServer()
    {
        try
        {
            socketConnection = new TcpClient("127.0.0.1", 9000);
            Debug.Log("Connected to server");
        }
        catch (Exception e)
        {
            Debug.Log("On client connect exception " + e);
        }
    }

    private void SendMessage(string message)
    {
        if (socketConnection == null)
        {
            Debug.Log("Failed to connect socket");
            return;
        }

        try
        {
            // 데이터 전송
            stream = socketConnection.GetStream();

            if (stream.CanWrite)
            {
                byte[] clientMessageAsByteArray = Serialaze(message);  // 수정된 직렬화 호출
                stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);
                Debug.Log("Client sent message: " + message);
            }
        }
        catch (Exception e)
        {
            Debug.Log("On client send message exception " + e);
        }
    }

    private void ReceiveMessage()
    {
        if (socketConnection == null)
        {
            return;
        }

        try
        {
            // 데이터 수신
            stream = socketConnection.GetStream();
            if (stream.CanRead)
            {
                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                byte[] readBuffer = Deserialaze(buffer);
                string message = Encoding.ASCII.GetString(readBuffer, 0, readBuffer.Length);
                //string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                Debug.Log("Server message received: " + message);
            }
        }
        catch (Exception e)
        {
            Debug.Log("On client receive message exception " + e);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SendMessage("Hello from client");
            ReceiveMessage();
        }
    }

    private void OnApplicationQuit()
    {
        if (socketConnection != null)
        {
            stream.Close();
            socketConnection.Close();
        }
    }

    private byte[] Serialaze(string message)
    {
        // 1. enum 값을 int로 변환
        PK_Data data = PK_Data.MESSAGE;
        int dataValue = (int)data;
        int messageLength = message.Length;

        // 2. int 값을 바이트 배열로 변환
        byte[] byteData = BitConverter.GetBytes(dataValue);       // 4바이트
        byte[] byteMessageLength = BitConverter.GetBytes(messageLength);  // 4바이트
        byte[] byteMessage = Encoding.ASCII.GetBytes(message);    // 메시지 자체를 바이트 배열로 변환

        //둘 중 하나만 있어도 될 것 같음
        // 3. 최종 배열을 결합
        byte[] result = new byte[byteData.Length + byteMessageLength.Length + byteMessage.Length];

        // 각각의 배열을 복사
        Buffer.BlockCopy(byteData, 0, result, 0, byteData.Length);                      // PK_Data
        Buffer.BlockCopy(byteMessageLength, 0, result, byteData.Length, byteMessageLength.Length); // 메시지 길이
        Buffer.BlockCopy(byteMessage, 0, result, byteData.Length + byteMessageLength.Length, byteMessage.Length);  // 실제 메시지

        return result;  // 바이트 배열 반환
        //둘 중 하나만 있어도 될 것 같음
    }

    private byte[] Deserialaze(byte[] buffer)
    {
        int currentHeader = (int)buffer[0]; //리틀 엔디안으로 저장되면 값을 3으로 바꿔야함. 추후 4바이트를 읽어서 int로 변환

        byte[] message = packetData[currentHeader].DeserialazingApply(buffer); //값들을 처리한 후 메세지(디버그)를 남겨준다.
        this.message.text = Encoding.UTF8.GetString(message);

        return message;
    }

}
