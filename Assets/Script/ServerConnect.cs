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
            // �ν��Ͻ��� ������ ����
            if (instance == null)
            {
                instance = FindObjectOfType<ServerConnect>();

                // �ν��Ͻ��� ���� ���ٸ� ���� ����
                if (instance == null)
                {
                    GameObject go = new GameObject("ServerConnect");
                    instance = go.AddComponent<ServerConnect>();
                }
            }

            return instance;
        }
    }

    // �����ڿ� �ʱ�ȭ
    private void Awake()
    {
        // �̱��� �ν��Ͻ��� �ٸ� �ν��Ͻ��� �浹�ϸ� ���� ��ü�� �ı�
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            // ���� ��ȯ�ص� �̱����� �����Ϸ��� DontDestroyOnLoad ���
            DontDestroyOnLoad(gameObject);
        }
    }

    // �۽� �� ���� ť ����
    private static Queue<byte[]> sendQueue = new Queue<byte[]>();
    private static Queue<byte[]> recvQueue = new Queue<byte[]>();

    private string _userId;
    public string UserId { get { return _userId; } 
        set { 
            if(value.Length > IdLength)
            {
                Debug.Log("���̵� �ʹ� ���.(20��)");
            }
            else
            {
                _userId = value;
            }
                 } }
    public int IdLength = 20;

    //��Ʈ��ũ ���� ����
    private TcpClient socketConnection;
    private NetworkStream stream;

    private Socket clientSocket;
    private IPEndPoint socketAdress;
    private AsyncSocketClient asyncSocketClient;

    //��Ŷ ���� ����
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
        // ���� �۾� ���� ���� Ȯ��
        if (e.SocketError == SocketError.Success)
        {
            Debug.Log("�޽��� ���� �Ϸ�!");

        }
        else
        {
            // ���� ���� �� ���� �޽��� ���
            Debug.LogError($"�޽��� ���� ����: {e.SocketError}");
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

            Debug.Log("Ŭ���̾�Ʈ ������ ����");

            StartReceive();
        }
        else if (e.SocketError == SocketError.ConnectionReset || e.BytesTransferred == 0)
        {
            Debug.LogWarning("�������� ������ ����Ǿ����ϴ�.");
            DisconnectServer(); // ���� ����
        }
        else
        {
            Debug.LogError($"���� �� ���� �߻�: {e.SocketError}");
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
            Debug.Log("������ ���� ����.");
        }

        catch(Exception ex) 
        {
            Debug.LogError("������ ���� ���� �� ����" + ex.Message);
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
    //    // 1. enum ���� int�� ��ȯ
    //    PK_Data data = PK_Data.MESSAGE;
    //    int dataValue = (int)data;
    //    int messageLength = message.Length;

    //    // 2. int ���� ����Ʈ �迭�� ��ȯ
    //    byte[] byteData = BitConverter.GetBytes(dataValue);       // 4����Ʈ
    //    byte[] byteMessageLength = BitConverter.GetBytes(messageLength);  // 4����Ʈ
    //    byte[] byteMessage = Encoding.ASCII.GetBytes(message);    // �޽��� ��ü�� ����Ʈ �迭�� ��ȯ

    //    //�� �� �ϳ��� �־ �� �� ����
    //    // 3. ���� �迭�� ����
    //    byte[] result = new byte[byteData.Length + byteMessageLength.Length + byteMessage.Length];

    //    // ������ �迭�� ����
    //    Buffer.BlockCopy(byteData, 0, result, 0, byteData.Length);                      // PK_Data
    //    Buffer.BlockCopy(byteMessageLength, 0, result, byteData.Length, byteMessageLength.Length); // �޽��� ����
    //    Buffer.BlockCopy(byteMessage, 0, result, byteData.Length + byteMessageLength.Length, byteMessage.Length);  // ���� �޽���

    //    return result;  // ����Ʈ �迭 ��ȯ
    //    //�� �� �ϳ��� �־ �� �� ����
    //}

    //private byte[] Deserialaze(byte[] buffer)
    //{
    //    int currentHeader = (int)buffer[0]; //��Ʋ ��������� ����Ǹ� ���� 3���� �ٲ����. ���� 4����Ʈ�� �о int�� ��ȯ

    //    byte[] message = packetData[currentHeader].DeserialazingApply(buffer); //������ ó���� �� �޼���(�����)�� �����ش�.
    //    this.message.text = Encoding.UTF8.GetString(message);

    //    return message;
    //}

}
