#pragma once
#include "pch.h"
#include "Packet.h"

class PacketMaker
{
protected:
	PacketHeader packetHeader;

public:
	virtual int Serialzed(char* buffer, int size) = 0;
	virtual void Deserialzed(char* buffer) = 0;

protected:
	int UnpackingHeader(char* buffer);
	int PackingHeader(char* buffer);
};