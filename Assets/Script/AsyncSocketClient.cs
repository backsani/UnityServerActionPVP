using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class AsyncSocketClient
{
    public void ConnectToServer(Socket clientSocket, IPEndPoint endPoint)
    {
        

        SocketAsyncEventArgs connectEventArgs = new SocketAsyncEventArgs();

        connectEventArgs.RemoteEndPoint = endPoint;
        connectEventArgs.Completed += OnConnectCompleted;

        if(!clientSocket.ConnectAsync(connectEventArgs))
        {
            OnConnectCompleted(this, connectEventArgs);
        }
    }

    private void OnConnectCompleted(object sender, SocketAsyncEventArgs e) 
    {
        if (e.SocketError == SocketError.Success)
        {
            Debug.Log("���� ���� ����!");
        }
        else
        {
            Debug.LogError($"���� ���� ����: {e.SocketError}");
        }
    }
}
