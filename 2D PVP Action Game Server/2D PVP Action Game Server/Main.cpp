#include <iostream>
#include "PacketSDK.h"
#include "Server.h"

const UINT16 MAX_CLIENT = 100;		//�� �����Ҽ� �ִ� Ŭ���̾�Ʈ ��

int main()
{
	Server ioCompletionPort;

	//������ �ʱ�ȭ
	ioCompletionPort.InitSocket();

	//���ϰ� ���� �ּҸ� �����ϰ� ��� ��Ų��.
	ioCompletionPort.BindSocket();

	ioCompletionPort.StartServer(MAX_CLIENT);

	printf("�ƹ� Ű�� ���� ������ ����մϴ�\n");
	while (true)
	{
		std::string inputCmd;
		std::getline(std::cin, inputCmd);

		if (inputCmd == "quit")
		{
			break;
		}
	}

	ioCompletionPort.DestroyThread();
	return 0;
}