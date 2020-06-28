#pragma once
namespace Coocoo3DGraphics
{
	public ref class Coocoo3DSmallPack sealed
	{
	public:
		property Platform::Object^ property1;
		virtual ~Coocoo3DSmallPack();
	internal:
		void* pDataUnManaged = nullptr;
		int value1 = 0;
		int value2 = 0;
		int width = 0;
		int height = 0;
	};
}