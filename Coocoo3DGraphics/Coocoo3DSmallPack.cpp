#include "pch.h"
#include "Coocoo3DSmallPack.h"
using namespace Coocoo3DGraphics;

Coocoo3DSmallPack::~Coocoo3DSmallPack()
{
	if (pDataUnManaged)
		free(pDataUnManaged);
}
