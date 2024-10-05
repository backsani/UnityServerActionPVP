#pragma once
#pragma comment(lib,"ws2_32")
#define _CRT_SECURE_NO_WARNINGS
#include <WinSock2.h>
#include <vector>
#include <thread>
#include <tchar.h>

#include "PacketSDK.h"

/*
패킷 추가시 생성자에서 패킷 버퍼에 추가시켜줘야한다. 또한 PacketSDK에서 해당 헤더를 선언해줘야한다.
*/

#define PORT 9000
#define BUFSIZE 256

class Server
{
	WSADATA wsa;
	SOCKET listen_sock;
	SOCKADDR_IN ServerAddress;

	std::vector<SOCKET> client_socket;
	std::vector<std::unique_ptr<Packet>> packet;
public:
	Server();

	VOID errQuit(const TCHAR* msg);
	VOID errDisplay(const TCHAR* msg);
	VOID setReady();
	VOID Connect();
	//bool AccectClient(Packet& packet);
	VOID ProcessClient(const SOCKET sock);
	VOID BroadcastPacket(char* buffer, const SOCKET& sock, int length);

	~Server();

};

