#pragma once
#include "DeviceResources.h"
namespace Coocoo3DGraphics
{
	public ref class VertexShader sealed
	{
	public:
		property bool Ready;
		property Platform::Object^ LoadTask;
		property Platform::String^ Path;
		static VertexShader^ CompileLoad(DeviceResources^ deviceResources, const Platform::Array<byte>^ sourceCode);
		void CompileReload(DeviceResources^ deviceResources, const Platform::Array<byte>^ sourceCode);
		static VertexShader^ Load(DeviceResources^ deviceResources, const Platform::Array<byte>^ data);
		void Reload(DeviceResources^ deviceResources, const Platform::Array<byte>^ data);
		virtual ~VertexShader();
	internal:
		Microsoft::WRL::ComPtr<ID3DBlob> byteCode;
	};
}
