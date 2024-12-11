#pragma once
#include "pch.h"


enum class IOOperation
{
	RECV,
	SEND
};

struct mOverlappedEx
{
	WSAOVERLAPPED _wsaOverlapped;
	WSABUF _wsaBuf;
	IOOperation _operation;
};

class ClientInfo
{
	SOCKET _socketClient;
	char			mUserId[20];

public:
	char			mRecvBuf[MAX_SOCKBUF]; //������ ����
	char			mSendBuf[MAX_SOCKBUF]; //������ ����

	mOverlappedEx _recvOverlappedEx;
	mOverlappedEx _sendOverlappedEx;

	ClientInfo()
	{
		ZeroMemory(&_recvOverlappedEx, sizeof(mOverlappedEx));
		ZeroMemory(&_sendOverlappedEx, sizeof(mOverlappedEx));
		memset(mRecvBuf, 0, sizeof(mRecvBuf));
		memset(mSendBuf, 0, sizeof(mSendBuf));
		memcpy(this->mUserId, mUserId, sizeof(mUserId));
		_socketClient = INVALID_SOCKET;
	}

	void SetSocket(SOCKET socket) { _socketClient = socket; }
	void SetUserId(const char* userId) { memcpy(mUserId, userId, sizeof(userId)); }

	SOCKET GetSocket() { return _socketClient; }
	char* GetUserId() { return mUserId; }
};