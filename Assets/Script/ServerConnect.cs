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
            // ������ ����
            stream = socketConnection.GetStream();

            if (stream.CanWrite)
            {
                byte[] clientMessageAsByteArray = Serialaze(message);  // ������ ����ȭ ȣ��
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
            // ������ ����
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
        // 1. enum ���� int�� ��ȯ
        PK_Data data = PK_Data.MESSAGE;
        int dataValue = (int)data;
        int messageLength = message.Length;

        // 2. int ���� ����Ʈ �迭�� ��ȯ
        byte[] byteData = BitConverter.GetBytes(dataValue);       // 4����Ʈ
        byte[] byteMessageLength = BitConverter.GetBytes(messageLength);  // 4����Ʈ
        byte[] byteMessage = Encoding.ASCII.GetBytes(message);    // �޽��� ��ü�� ����Ʈ �迭�� ��ȯ

        //�� �� �ϳ��� �־ �� �� ����
        // 3. ���� �迭�� ����
        byte[] result = new byte[byteData.Length + byteMessageLength.Length + byteMessage.Length];

        // ������ �迭�� ����
        Buffer.BlockCopy(byteData, 0, result, 0, byteData.Length);                      // PK_Data
        Buffer.BlockCopy(byteMessageLength, 0, result, byteData.Length, byteMessageLength.Length); // �޽��� ����
        Buffer.BlockCopy(byteMessage, 0, result, byteData.Length + byteMessageLength.Length, byteMessage.Length);  // ���� �޽���

        return result;  // ����Ʈ �迭 ��ȯ
        //�� �� �ϳ��� �־ �� �� ����
    }

    private byte[] Deserialaze(byte[] buffer)
    {
        int currentHeader = (int)buffer[0]; //��Ʋ ��������� ����Ǹ� ���� 3���� �ٲ����. ���� 4����Ʈ�� �о int�� ��ȯ

        byte[] message = packetData[currentHeader].DeserialazingApply(buffer); //������ ó���� �� �޼���(�����)�� �����ش�.
        this.message.text = Encoding.UTF8.GetString(message);

        return message;
    }

}
