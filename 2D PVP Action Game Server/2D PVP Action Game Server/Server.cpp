#include "Server.h"

/*
패킷 추가 시 서버 생성자에서 패킷 벡터에 추가시켜줘야한다.
*/
Server::Server() {
	this->packet.push_back(std::make_unique<LoginPacketMaker>());
	// 윈속 초기화
	
}

Server::~Server()
{
	// closesocket()
	closesocket(listenSocket);

	// 윈속 종료
	WSACleanup();
}

bool Server::InitSocket()
{
	WSADATA wsaData;

	if (WSAStartup(MAKEWORD(2, 2), &wsaData))
	{
		printf("[에러] WSAStartup() 함수 실패 : %d\n", WSAGetLastError());
		return false;
	}

	listenSocket = WSASocket(AF_INET, SOCK_STREAM, IPPROTO_TCP, NULL, NULL, WSA_FLAG_OVERLAPPED);

	if (listenSocket == INVALID_SOCKET)
	{
		printf("[에러] WSASocket() 함수 실패 : %d\n", WSAGetLastError());
		return false;
	}

	printf("소켓 초기화 성공\n");
	return true;
}

bool Server::BindSocket()
{
	SOCKADDR_IN serverAddr;
	serverAddr.sin_family = AF_INET;
	serverAddr.sin_port = htons(PORT); //서버 포트를 설정한다.				
	serverAddr.sin_addr.s_addr = htonl(INADDR_ANY);

	//위에서 지정한 서버 주소 정보와 cIOCompletionPort 소켓을 연결한다.
	if (bind(listenSocket, (SOCKADDR*)&serverAddr, sizeof(SOCKADDR_IN)))
	{
		printf("[에러] bind()함수 실패 : %d\n", WSAGetLastError());
		return false;
	}

	if (listen(listenSocket, 5))
	{
		printf("[에러] listen()함수 실패 : %d\n", WSAGetLastError());
		return false;
	}

	printf("서버 등록 성공..\n");
	return true;
}

bool Server::StartServer(const UINT32 maxClientCount)
{
	Server::CreateClient(maxClientCount);

	mIOCPHandle = CreateIoCompletionPort(INVALID_HANDLE_VALUE, NULL, NULL, MAX_WORKERTHREAD);
	if (mIOCPHandle == NULL)
	{
		printf("[에러] CreateIoCompletionPort() 함수 실패: %d\n", GetLastError());
		return false;
	}

	if (!CreateWokerThread()) {
		printf("[에러] CreateWokerThread() 함수 실패: %d\n", GetLastError());
		return false;
	}

	if (!CreateAccepterThread()) {
		printf("[에러] CreateAccepterThread() 함수 실패: %d\n", GetLastError());
		return false;
	}

	printf("서버 시작\n");
	return true;
}

void Server::CreateClient(const UINT32 maxClientCount)
{
	for (UINT32 i = 0; i < maxClientCount; i++)
	{
		mClientInfos.emplace_back();
	}
}


bool Server::CreateWokerThread()
{
	unsigned int uiThreadId = 0;
	//WaingThread Queue에 대기 상태로 넣을 쓰레드들 생성 권장되는 개수 : (cpu개수 * 2) + 1 
	for (int i = 0; i < MAX_WORKERTHREAD; i++)
	{
		mWorkerThreads.emplace_back([this]() { WokerThread(); });
	}

	printf("WokerThread 시작..\n");
	return true;
}

bool Server::CreateAccepterThread()
{
	mAccepterThread = std::thread([this]() { AccepterThread(); });

	printf("AccepterThread 시작..\n");
	return true;
}

void Server::WokerThread()
{
	ClientInfo* pClientInfo = nullptr;
	BOOL bSuccess = TRUE;
	DWORD dwIoSize = 0;
	LPOVERLAPPED lpOverlapped = NULL;
	HeaderType currentHeader;
	Buffer_Converter bufferCon;
	int bufSize = 0;


	while (mIsWorkerRun)
	{
		bSuccess = GetQueuedCompletionStatus(mIOCPHandle,
			&dwIoSize,					// 실제로 전송된 바이트
			(PULONG_PTR)&pClientInfo,		// CompletionKey
			&lpOverlapped,				// Overlapped IO 객체
			INFINITE);					// 대기할 시간

		//사용자 쓰레드 종료 메세지 처리..
		if (TRUE == bSuccess && 0 == dwIoSize && NULL == lpOverlapped)
		{
			mIsWorkerRun = false;
			continue;
		}

		if (NULL == lpOverlapped)
		{
			continue;
		}

		//client가 접속을 끊었을때..			
		if (FALSE == bSuccess || (0 == dwIoSize && TRUE == bSuccess))
		{
			printf("socket(%d) 접속 끊김\n", (int)pClientInfo->_socketClient);
			CloseSocket(pClientInfo);
			continue;
		}


		auto pOverlappedEx = (mOverlappedEx*)lpOverlapped;

		currentHeader = bufferCon.GetHeader(pClientInfo->mRecvBuf);


		/*------------------
				공사중
		------------------*/


		//Overlapped I/O Recv작업 결과 뒤 처리
		if (IOOperation::RECV == pOverlappedEx->_operation)
		{
			packet[currentHeader]->Deserialzed(pClientInfo->mRecvBuf);

			switch (currentHeader)
			{
			case HeaderType::ACCEPT:
				{
					auto loginPacket = static_cast<LoginPacketMaker*>(packet[currentHeader].get());

					//ConnectionState를 보고 로그인인지 회원가입인지 처리

					memcpy(&pClientInfo->mUserId, loginPacket->GetUserId(), sizeof(loginPacket->GetUserId()));

					printf("[유저 접속] msg : %s\n", pClientInfo->mUserId);
					loginPacket->SetConnectionInfo(ConnectionState::LOGIN_SUCCESS);

					memcpy(&pClientInfo->mSendBuf, pClientInfo->mUserId, sizeof(pClientInfo->mUserId));
				}
				break;
			case HeaderType::NEWTIME:
				break;
			default:
				printf("[오류] not exists Packet\n");
				break;
			}

			/*strcpy(pClientInfo->mRecvBuf, packet[currentHeader]->GetBuffer());*/


			//클라이언트에 메세지를 에코한다.
			int bufLength = packet[currentHeader]->Serialzed(pClientInfo->mSendBuf, sizeof(pClientInfo->mSendBuf));
			//int bufLength = packet[currentHeader]->Serialaze(packet[currentHeader]->GetBuffer());

			SendMsg(pClientInfo, pClientInfo->mSendBuf, bufLength);

			BindRecv(pClientInfo);
		}
		//Overlapped I/O Send작업 결과 뒤 처리
		else if (IOOperation::SEND == pOverlappedEx->_operation)
		{
			printf("[송신] msg : %s\n", pClientInfo->mSendBuf);
		}
		//예외 상황
		else
		{
			printf("socket(%d)에서 예외상황\n", (int)pClientInfo->_socketClient);
		}

	}
}


void Server::AccepterThread()
{
	SOCKADDR_IN		stClientAddr;
	int nAddrLen = sizeof(SOCKADDR_IN);

	while (mIsAccepterRun)
	{
		//접속을 받을 구조체의 인덱스를 얻어온다.
		ClientInfo* pClientInfo = GetEmptyClientInfo();
		if (NULL == pClientInfo)
		{
			printf("[에러] Client Full\n");
			return;
		}

		//클라이언트 접속 요청이 들어올 때까지 기다린다.
		pClientInfo->_socketClient = accept(listenSocket, (SOCKADDR*)&stClientAddr, &nAddrLen);
		if (INVALID_SOCKET == pClientInfo->_socketClient)
		{
			continue;
		}

		//I/O Completion Port객체와 소켓을 연결시킨다.
		if (!BindIOCompletionPort(pClientInfo))
		{
			return;
		}

		//Recv Overlapped I/O작업을 요청해 놓는다.
		if (!BindRecv(pClientInfo))
		{
			return;
		}

		printf("클라이언트 접속 : SOCKET(%d)\n", (int)pClientInfo->_socketClient);

		//클라이언트 갯수 증가
		//++mClientCnt;
	}
}

ClientInfo* Server::GetEmptyClientInfo()
{
	for (auto& client : mClientInfos)
	{
		if (INVALID_SOCKET == client._socketClient)
		{
			return &client;
		}
	}

	return nullptr;
}

void Server::DestroyThread()
{
	mIsWorkerRun = false;
	CloseHandle(mIOCPHandle);

	for (auto& th : mWorkerThreads)
	{
		if (th.joinable())
		{
			th.join();
		}
	}

	mIsAccepterRun = false;
	closesocket(listenSocket);

	if (mAccepterThread.joinable())
	{
		mAccepterThread.join();
	}
}

bool Server::BindIOCompletionPort(ClientInfo* pClientInfo)
{
	//socket과 pClientInfo를 CompletionPort객체와 연결시킨다.
	auto hIOCP = CreateIoCompletionPort((HANDLE)pClientInfo->_socketClient
		, mIOCPHandle
		, (ULONG_PTR)(pClientInfo), 0);

	if (NULL == hIOCP || mIOCPHandle != hIOCP)
	{
		printf("[에러] CreateIoCompletionPort()함수 실패: %d\n", GetLastError());
		return false;
	}

	return true;
}


//WSARecv Overlapped I/O 작업을 시킨다.
bool Server::BindRecv(ClientInfo* pClientInfo)
{
	DWORD dwFlag = 0;
	DWORD dwRecvNumBytes = 0;

	//Overlapped I/O을 위해 각 정보를 셋팅해 준다.
	pClientInfo->_recvOverlappedEx._wsaBuf.len = MAX_SOCKBUF;
	pClientInfo->_recvOverlappedEx._wsaBuf.buf = pClientInfo->mRecvBuf;
	pClientInfo->_recvOverlappedEx._operation = IOOperation::RECV;

	int nRet = WSARecv(pClientInfo->_socketClient,
		&(pClientInfo->_recvOverlappedEx._wsaBuf),
		1,
		&dwRecvNumBytes,
		&dwFlag,
		(LPWSAOVERLAPPED) & (pClientInfo->_recvOverlappedEx),
		NULL);

	//socket_error이면 client socket이 끊어진걸로 처리한다.
	if (nRet == SOCKET_ERROR && (WSAGetLastError() != ERROR_IO_PENDING))
	{
		printf("[에러] WSARecv()함수 실패 : %d\n", WSAGetLastError());
		return false;
	}

	return true;
}

//WSASend Overlapped I/O작업을 시킨다.
bool Server::SendMsg(ClientInfo* pClientInfo, char* pMsg, int nLen)
{
	DWORD dwRecvNumBytes = 0;

	//전송될 메세지를 복사
	CopyMemory(pClientInfo->mSendBuf, pMsg, nLen);
	pClientInfo->mSendBuf[nLen] = '\0';


	//Overlapped I/O을 위해 각 정보를 셋팅해 준다.
	pClientInfo->_sendOverlappedEx._wsaBuf.len = nLen;
	pClientInfo->_sendOverlappedEx._wsaBuf.buf = pClientInfo->mSendBuf;
	pClientInfo->_sendOverlappedEx._operation = IOOperation::SEND;

	int nRet = WSASend(pClientInfo->_socketClient,
		&(pClientInfo->_sendOverlappedEx._wsaBuf),
		1,
		&dwRecvNumBytes,
		0,
		(LPWSAOVERLAPPED) & (pClientInfo->_sendOverlappedEx),
		NULL);

	//socket_error이면 client socket이 끊어진걸로 처리한다.
	if (nRet == SOCKET_ERROR && (WSAGetLastError() != ERROR_IO_PENDING))
	{
		printf("[에러] WSASend()함수 실패 : %d\n", WSAGetLastError());
		return false;
	}
	return true;
}



void Server::CloseSocket(ClientInfo* pClientInfo)
{
	bool bIsForce = false;
	struct linger stLinger = { 0, 0 };	// SO_DONTLINGER로 설정

	// bIsForce가 true이면 SO_LINGER, timeout = 0으로 설정하여 강제 종료 시킨다. 주의 : 데이터 손실이 있을수 있음 
	if (true == bIsForce)
	{
		stLinger.l_onoff = 1;
	}

	//socketClose소켓의 데이터 송수신을 모두 중단 시킨다.
	shutdown(pClientInfo->_socketClient, SD_BOTH);

	//소켓 옵션을 설정한다.
	setsockopt(pClientInfo->_socketClient, SOL_SOCKET, SO_LINGER, (char*)&stLinger, sizeof(stLinger));

	//소켓 연결을 종료 시킨다. 
	closesocket(pClientInfo->_socketClient);

	pClientInfo->_socketClient = INVALID_SOCKET;
}



/*-----------------------------------------------
				절		취		선
-----------------------------------------------*/

//VOID Server::Connect() {
//	SOCKET clientSocket;
//	SOCKADDR_IN clientAddress;
//	INT AddressLen;
//	AddressLen = sizeof(clientAddress);
//
//
//	while (1)
//	{
//		//Accept()
//		clientSocket = accept(listen_sock, (SOCKADDR*)&clientAddress, &AddressLen);
//		if (clientSocket == INVALID_SOCKET)
//		{
//			//errDisplay(_T("Accept"));
//			continue;
//		}
//		
//		printf("접속 성공\n");
//
//		this->client_socket.emplace_back(clientSocket);
//		std::thread t(&Server::ProcessClient, this, clientSocket);
//		t.detach();
//
//	}
//}
//
//
////const Socket& sock
//VOID Server::ProcessClient(const SOCKET client_sock)
//{
//
//	char buf[BUFSIZE + 1];
//	int retval;
//	SOCKADDR_IN clientaddr;
//	char clientPort[10];
//	int addrlen;
//	
//	//PK_POS packetPos;
//	Buffer_Converter bufferCon;
//
//	addrlen = sizeof(sockaddr);
//	
//	PK_Data currentHeader;
//
//	while (1) {
//		// 데이터 받기
//		retval = recv(client_sock, buf, BUFSIZE, 0);
//		if (retval == SOCKET_ERROR) {
//			errDisplay(_T("recv()"));
//			break;
//		}
//		else if (retval == 0)
//			continue;
//
//		
//
//		printf("%s", buf);
//
//		currentHeader = bufferCon.GetHeader(buf);
//
//		packet[currentHeader]->DeSerialaze(buf);
//		strcpy(buf, packet[currentHeader]->GetBuffer());
//		printf("%s", buf);
//		/*if (bufferCon.GetHeader(buf) == DataHeader::Exit) {
//			packet.Deserialization(buf, sizeof(buf));
//			break;
//		}
//
//		if (bufferCon.GetHeader(buf) == DataHeader::Message || bufferCon.GetHeader(buf) == DataHeader::Position) {
//			packet.Deserialization(buf, sizeof(buf));
//
//			if (DebugMode == true) {
//				printf("클라이언트 Id : %s   ", packet.GetId());
//				printf("클라이언트 Message : %s\n", packet.GetBuffer());
//				printf("클라이언트 Header : %d   ", packet.GetHeader());
//				printf("클라이언트 Size : %d\n\n", packet.GetSize());
//			}
//
//			retval = packet.Serialize(buf, packet.GetHeader());
//		}
//
//		else if (bufferCon.GetHeader(buf) == DataHeader::Req_con) {
//			packet.Deserialization(buf, sizeof(buf));
//
//			if (DebugMode == true) {
//				printf("클라이언트 ID : %s   ", packet.GetId());
//				printf("클라이언트 Message : %s\n", packet.GetBuffer());
//				printf("클라이언트 Header : %d   ", packet.GetHeader());
//				printf("클라이언트 Size : %d\n", packet.GetSize());
//			}
//			client_socket.emplace_back(client_sock);
//
//			
//
//			retval = send(client_sock, buf, retval, 0);
//			if (retval == SOCKET_ERROR) {
//				errDisplay(_T("send()"));
//			}
//
//			packet.SetHeader(DataHeader::User_Con);
//			packet.SetBuffer(packet.GetId());
//			retval = packet.Serialize(buf, packet.GetHeader());
//
//		}
//		else printf("\n헤더가 옳바르지 않습니다.\n");*/
//		
//		int bufLength = packet[currentHeader]->Serialaze(buf);
//
//		retval = send(client_sock, buf, bufLength, 0);
//		if (retval == SOCKET_ERROR) {
//			errDisplay(_T("recv()"));
//			break;
//		}
//		else if (retval == 0)
//			continue;
//
//		//BroadcastPacket(buf, client_sock, retval);
//
//	}
//
//	/*packet.SetHeader(DataHeader::Exit);
//	retval = packet.Serialize(buf, packet.GetHeader());*/
//
//	BroadcastPacket(buf, client_sock, retval);
//
//	// 요소를 찾아서 제거
//	//auto it = std::find(client_socket.begin(), client_socket.end(), client_sock);
//	//if (it != client_socket.end()) {
//	//	client_socket.erase(it); // 찾은 요소를 제거
//	//	printf("[TCP 서버] %s 종료: IP 주소=%s, 포트 번호=%d\n", packet.GetId(),
//	//		inet_ntoa(clientaddr.sin_addr), ntohs(clientaddr.sin_port));
//	//}
//	//else {
//	//	printf("제거에 실패했습니다.\n");
//	//}
//
//	// closesocket()
//	closesocket(client_sock);
//
//	return;
//}
//
//VOID Server::BroadcastPacket(char* buffer, const SOCKET& sock, int length) {
//	int retval;
//	for (SOCKET client : client_socket) {
//		if (client != sock) {
//			retval = send(client, buffer, length, 0);
//		}
//	}
//}
//
//
//VOID Server::errQuit(const TCHAR* msg) {
//	LPVOID lpMsgBuf;
//	FormatMessage(
//		FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM,
//		NULL, WSAGetLastError(),
//		MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
//		(LPTSTR)&lpMsgBuf, 0, NULL);
//	MessageBox(NULL, (LPCTSTR)lpMsgBuf, (LPCWSTR)msg, MB_ICONERROR);
//	LocalFree(lpMsgBuf);
//	exit(1);
//}
//
//VOID Server::errDisplay(const TCHAR* msg) {
//	LPVOID lpMsgBuf;
//	FormatMessage(
//		FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM,
//		NULL, WSAGetLastError(),
//		MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
//		(LPTSTR)&lpMsgBuf, 0, NULL);
//	printf("[%s] %s", (LPCWSTR)msg, (char*)lpMsgBuf);
//	LocalFree(lpMsgBuf);
//}