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
	SOCKET _socketClient;
	WSABUF _wsaBuf;
	IOOperation _operation;
};

struct ClientInfo
{
	SOCKET _socketClient;
	mOverlappedEx _recvOverlappedEx;
	mOverlappedEx _sendOverlappedEx;

	char			mUserId[20];
	char			mRecvBuf[MAX_SOCKBUF]; //데이터 버퍼
	char			mSendBuf[MAX_SOCKBUF]; //데이터 버퍼

	ClientInfo()
	{
		ZeroMemory(&_recvOverlappedEx, sizeof(mOverlappedEx));
		ZeroMemory(&_sendOverlappedEx, sizeof(mOverlappedEx));
		memset(mUserId, 0, sizeof(mUserId));
		_socketClient = INVALID_SOCKET;
	}
};