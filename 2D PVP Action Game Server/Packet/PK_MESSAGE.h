#pragma once
#include "Packet.h"

class PK_MESSAGE : public Packet
{

public:
	PK_MESSAGE();
	int Serialaze(char* buffer);
	void DeSerialaze(char* buffer);
};