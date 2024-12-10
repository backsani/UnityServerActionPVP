using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
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

    private Socket clientSocket;
    private IPEndPoint socketAdress;
    private AsyncSocketClient asyncSocketClient;

    //패킷 관련 변수
    public List<Packet> packetData = new List<Packet>();
    private LoginPacket PK_login = new LoginPacket();
    public PacketProcessor packetProcessor = new PacketProcessor();

    public ServerUtil.Header.ConnectionState currentState;

    Buffer_Converter bufferCon;

    [SerializeField]
    private Text message;
    
    void Start()
    {
        asyncSocketClient = new AsyncSocketClient();

        ConnectToTcpServer();
        packetData.Add(PK_login);

        UserId = "Unconnected User";
        currentState = ServerUtil.Header.ConnectionState.INIT;

        StartReceive();
    }

    private void ConnectToTcpServer()
    {
        clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        //socketConnection = new TcpClient("127.0.0.1", 9000);
        socketAdress = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9000);
        asyncSocketClient.ConnectToServer(clientSocket, socketAdress);
        Debug.Log("Connected to server");
        
        
    }

    void StartSend(byte[] message)
    {
        SocketAsyncEventArgs sendEventArgs = new SocketAsyncEventArgs();

        sendEventArgs.Completed += OnsendCompleted;

        sendEventArgs.SetBuffer(message, 0, message.Length);

        if(!clientSocket.SendAsync(sendEventArgs))
        {
            OnsendCompleted(this, sendEventArgs);
        }
    }

    private void OnsendCompleted(object sender, SocketAsyncEventArgs e) 
    {
        // 전송 작업 성공 여부 확인
        if (e.SocketError == SocketError.Success)
        {
            Debug.Log("메시지 전송 완료!");

        }
        else
        {
            // 전송 실패 시 에러 메시지 출력
            Debug.LogError($"메시지 전송 실패: {e.SocketError}");
        }
    }

    private void StartReceive()
    {
        SocketAsyncEventArgs receiveEventArgs = new SocketAsyncEventArgs();

        receiveEventArgs.Completed += OnReceiveCompleted;

        byte[] buffer = new byte[1024];
        receiveEventArgs.SetBuffer(buffer, 0, buffer.Length);

        if (!clientSocket.ReceiveAsync(receiveEventArgs))
        {
            OnReceiveCompleted(this, receiveEventArgs);
        }
    }

    private void OnReceiveCompleted(object sender, SocketAsyncEventArgs e)
    {
        if(e.SocketError == SocketError.Success && e.BytesTransferred > 0)
        {
            EnqueueRecvData(e.Buffer);

            Debug.Log("클라이언트 데이터 수신");

            StartReceive();
        }
        else if (e.SocketError == SocketError.ConnectionReset || e.BytesTransferred == 0)
        {
            Debug.LogWarning("서버와의 연결이 종료되었습니다.");
            DisconnectServer(); // 연결 종료
        }
        else
        {
            Debug.LogError($"수신 중 오류 발생: {e.SocketError}");
        }
    }


    void Update()
    {

        if (sendQueue.Count > 0)
        {
            StartSend(sendQueue.Dequeue());
        }
        if (recvQueue.Count > 0)
        {
            packetProcessor.ProcessBuffer(recvQueue.Dequeue());
        }
            
    }

    /*-----------------------
        Queue(Send,Recv)
    ------------------------*/

    public void EnqueueSendData(byte[] data)
    {
        sendQueue.Enqueue(data);
    }

    public void EnqueueRecvData(byte[] data)
    {
        recvQueue.Enqueue(data);
    }

    /*-----------------------
        Disconnect Server
    ------------------------*/

    public void DisconnectServer()
    {
        try
        {
            if (clientSocket.Connected)
            {
                clientSocket.Shutdown(SocketShutdown.Both);
            }

            clientSocket.Close();
            Debug.Log("서버와 연결 종료.");
        }

        catch(Exception ex) 
        {
            Debug.LogError("서버와 연결 종료 중 오류" + ex.Message);
        }
    }

    private void OnApplicationQuit()
    {
        if (clientSocket != null)
        {
            clientSocket.Close();
        }
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

    //private byte[] Deserialaze(byte[] buffer)
    //{
    //    int currentHeader = (int)buffer[0]; //리틀 엔디안으로 저장되면 값을 3으로 바꿔야함. 추후 4바이트를 읽어서 int로 변환

    //    byte[] message = packetData[currentHeader].DeserialazingApply(buffer); //값들을 처리한 후 메세지(디버그)를 남겨준다.
    //    this.message.text = Encoding.UTF8.GetString(message);

    //    return message;
    //}

}
