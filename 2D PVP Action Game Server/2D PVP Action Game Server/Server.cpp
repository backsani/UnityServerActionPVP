#include "Server.h"

/*
패킷 추가 시 서버 생성자에서 패킷 벡터에 추가시켜줘야한다.
*/
Server::Server() {
	this->packet.push_back(std::make_unique<PK_MESSAGE>());
	// 윈속 초기화
	if (WSAStartup(MAKEWORD(2, 2), &wsa) != 0)
		return;
}

Server::~Server()
{
	// closesocket()
	closesocket(listen_sock);

	// 윈속 종료
	WSACleanup();
}

VOID Server::setReady() {
	INT retval;

	// socket()
	listen_sock = socket(AF_INET, SOCK_STREAM, 0);
	if (listen_sock == INVALID_SOCKET)
		errDisplay(_T("socket()"));

	//bind()
	ZeroMemory(&ServerAddress, sizeof(SOCKADDR_IN));
	ServerAddress.sin_family = AF_INET;
	ServerAddress.sin_addr.S_un.S_addr = htonl(INADDR_ANY);
	ServerAddress.sin_port = htons(PORT);
	retval = bind(listen_sock, (SOCKADDR*)&ServerAddress, sizeof(ServerAddress));
	if (retval == SOCKET_ERROR)
		//error_Quit(_T("Bind()"));
		return;

	// listen()
	retval = listen(listen_sock, SOMAXCONN);
	if (retval == SOCKET_ERROR) errQuit(_T("listen()"));
}

VOID Server::Connect() {
	SOCKET clientSocket;
	SOCKADDR_IN clientAddress;
	INT AddressLen;
	AddressLen = sizeof(clientAddress);


	while (1)
	{
		//Accept()
		clientSocket = accept(listen_sock, (SOCKADDR*)&clientAddress, &AddressLen);
		if (clientSocket == INVALID_SOCKET)
		{
			//errDisplay(_T("Accept"));
			continue;
		}
		
		printf("접속 성공\n");

		this->client_socket.emplace_back(clientSocket);
		std::thread t(&Server::ProcessClient, this, clientSocket);
		t.detach();

	}
}


//const Socket& sock
VOID Server::ProcessClient(const SOCKET client_sock)
{

	char buf[BUFSIZE + 1];
	int retval;
	SOCKADDR_IN clientaddr;
	char clientPort[10];
	int addrlen;
	
	//PK_POS packetPos;
	Buffer_Converter bufferCon;

	addrlen = sizeof(sockaddr);
	
	PK_Data currentHeader;

	while (1) {
		// 데이터 받기
		retval = recv(client_sock, buf, BUFSIZE, 0);
		if (retval == SOCKET_ERROR) {
			errDisplay(_T("recv()"));
			break;
		}
		else if (retval == 0)
			continue;

		

		printf("%s", buf);

		currentHeader = bufferCon.GetHeader(buf);

		packet[currentHeader]->DeSerialaze(buf);
		strcpy(buf, packet[currentHeader]->GetBuffer());
		printf("%s", buf);
		/*if (bufferCon.GetHeader(buf) == DataHeader::Exit) {
			packet.Deserialization(buf, sizeof(buf));
			break;
		}

		if (bufferCon.GetHeader(buf) == DataHeader::Message || bufferCon.GetHeader(buf) == DataHeader::Position) {
			packet.Deserialization(buf, sizeof(buf));

			if (DebugMode == true) {
				printf("클라이언트 Id : %s   ", packet.GetId());
				printf("클라이언트 Message : %s\n", packet.GetBuffer());
				printf("클라이언트 Header : %d   ", packet.GetHeader());
				printf("클라이언트 Size : %d\n\n", packet.GetSize());
			}

			retval = packet.Serialize(buf, packet.GetHeader());
		}

		else if (bufferCon.GetHeader(buf) == DataHeader::Req_con) {
			packet.Deserialization(buf, sizeof(buf));

			if (DebugMode == true) {
				printf("클라이언트 ID : %s   ", packet.GetId());
				printf("클라이언트 Message : %s\n", packet.GetBuffer());
				printf("클라이언트 Header : %d   ", packet.GetHeader());
				printf("클라이언트 Size : %d\n", packet.GetSize());
			}
			client_socket.emplace_back(client_sock);

			

			retval = send(client_sock, buf, retval, 0);
			if (retval == SOCKET_ERROR) {
				errDisplay(_T("send()"));
			}

			packet.SetHeader(DataHeader::User_Con);
			packet.SetBuffer(packet.GetId());
			retval = packet.Serialize(buf, packet.GetHeader());

		}
		else printf("\n헤더가 옳바르지 않습니다.\n");*/
		
		int bufLength = packet[currentHeader]->Serialaze(buf);

		retval = send(client_sock, buf, bufLength, 0);
		if (retval == SOCKET_ERROR) {
			errDisplay(_T("recv()"));
			break;
		}
		else if (retval == 0)
			continue;

		//BroadcastPacket(buf, client_sock, retval);

	}

	/*packet.SetHeader(DataHeader::Exit);
	retval = packet.Serialize(buf, packet.GetHeader());*/

	BroadcastPacket(buf, client_sock, retval);

	// 요소를 찾아서 제거
	//auto it = std::find(client_socket.begin(), client_socket.end(), client_sock);
	//if (it != client_socket.end()) {
	//	client_socket.erase(it); // 찾은 요소를 제거
	//	printf("[TCP 서버] %s 종료: IP 주소=%s, 포트 번호=%d\n", packet.GetId(),
	//		inet_ntoa(clientaddr.sin_addr), ntohs(clientaddr.sin_port));
	//}
	//else {
	//	printf("제거에 실패했습니다.\n");
	//}

	// closesocket()
	closesocket(client_sock);

	return;
}

VOID Server::BroadcastPacket(char* buffer, const SOCKET& sock, int length) {
	int retval;
	for (SOCKET client : client_socket) {
		if (client != sock) {
			retval = send(client, buffer, length, 0);
		}
	}
}


VOID Server::errQuit(const TCHAR* msg) {
	LPVOID lpMsgBuf;
	FormatMessage(
		FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM,
		NULL, WSAGetLastError(),
		MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
		(LPTSTR)&lpMsgBuf, 0, NULL);
	MessageBox(NULL, (LPCTSTR)lpMsgBuf, (LPCWSTR)msg, MB_ICONERROR);
	LocalFree(lpMsgBuf);
	exit(1);
}

VOID Server::errDisplay(const TCHAR* msg) {
	LPVOID lpMsgBuf;
	FormatMessage(
		FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM,
		NULL, WSAGetLastError(),
		MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
		(LPTSTR)&lpMsgBuf, 0, NULL);
	printf("[%s] %s", (LPCWSTR)msg, (char*)lpMsgBuf);
	LocalFree(lpMsgBuf);
}