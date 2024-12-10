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
            Debug.Log("서버 연결 성공!");
        }
        else
        {
            Debug.LogError($"서버 연결 실패: {e.SocketError}");
        }
    }
}
