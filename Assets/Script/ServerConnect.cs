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

    private static ServerConnect instance;

    public static ServerConnect Instance
    {
        get
        {
            // 인스턴스가 없으면 생성
            if (instance == null)
            {
                instance = FindObjectOfType<ServerConnect>();

                // 인스턴스가 씬에 없다면 새로 생성
                if (instance == null)
                {
                    GameObject go = new GameObject("ServerConnect");
                    instance = go.AddComponent<ServerConnect>();
                }
            }

            return instance;
        }
    }

    // 생성자와 초기화
    private void Awake()
    {
        // 싱글톤 인스턴스가 다른 인스턴스와 충돌하면 현재 객체를 파괴
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            // 씬을 전환해도 싱글톤을 유지하려면 DontDestroyOnLoad 사용
            DontDestroyOnLoad(gameObject);
        }
    }

    // 송신 및 수신 큐 정의
    private static Queue<byte[]> sendQueue = new Queue<byte[]>();
    private static Queue<byte[]> recvQueue = new Queue<byte[]>();

    private string _userId;
    public string UserId { get { return _userId; } 
        set { 
            if(value.Length > IdLength)
            {
                Debug.Log("아이디가 너무 길다.(20자)");
            }
            else
            {
                _userId = value;
            }
                 } }
    public int IdLength = 20;

    //네트워크 관련 변수
    private TcpClient socketConnection;
    private NetworkStream stream;

    //패킷 관련 변수
    public List<Packet> packetData = new List<Packet>();
    private LoginPacket PK_login = new LoginPacket();

    public ServerUtil.Header.ConnectionState currentState;

    [SerializeField]
    private Text message;
    
    void Start()
    {
        ConnectToTcpServer();
        packetData.Add(PK_login);

        UserId = "Unconnected User";
        currentState = ServerUtil.Header.ConnectionState.INIT;

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

    private void SendMessage(byte[] message)
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
                stream.Write(message, 0, message.Length);
                Debug.Log("Client sent message: " + Encoding.ASCII.GetString(message));
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

        if (sendQueue.Count > 0)
        {
            SendMessage(sendQueue.Dequeue());
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

    public void EnqueueSendData(byte[] data)
    {
        sendQueue.Enqueue(data);
    }

    //private byte[] Serialaze(string message)
    //{
    //    // 1. enum 값을 int로 변환
    //    PK_Data data = PK_Data.MESSAGE;
    //    int dataValue = (int)data;
    //    int messageLength = message.Length;

    //    // 2. int 값을 바이트 배열로 변환
    //    byte[] byteData = BitConverter.GetBytes(dataValue);       // 4바이트
    //    byte[] byteMessageLength = BitConverter.GetBytes(messageLength);  // 4바이트
    //    byte[] byteMessage = Encoding.ASCII.GetBytes(message);    // 메시지 자체를 바이트 배열로 변환

    //    //둘 중 하나만 있어도 될 것 같음
    //    // 3. 최종 배열을 결합
    //    byte[] result = new byte[byteData.Length + byteMessageLength.Length + byteMessage.Length];

    //    // 각각의 배열을 복사
    //    Buffer.BlockCopy(byteData, 0, result, 0, byteData.Length);                      // PK_Data
    //    Buffer.BlockCopy(byteMessageLength, 0, result, byteData.Length, byteMessageLength.Length); // 메시지 길이
    //    Buffer.BlockCopy(byteMessage, 0, result, byteData.Length + byteMessageLength.Length, byteMessage.Length);  // 실제 메시지

    //    return result;  // 바이트 배열 반환
    //    //둘 중 하나만 있어도 될 것 같음
    //}

    private byte[] Deserialaze(byte[] buffer)
    {
        int currentHeader = (int)buffer[0]; //리틀 엔디안으로 저장되면 값을 3으로 바꿔야함. 추후 4바이트를 읽어서 int로 변환

        byte[] message = packetData[currentHeader].DeserialazingApply(buffer); //값들을 처리한 후 메세지(디버그)를 남겨준다.
        this.message.text = Encoding.UTF8.GetString(message);

        return message;
    }

}
