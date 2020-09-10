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
		static VertexShader^ CompileLoad(const Platform::Array<byte>^ sourceCode);
		void CompileReload(const Platform::Array<byte>^ sourceCode);
		bool CompileReload1(const Platform::Array<byte>^ sourceCode, Platform::String^ entryPoint);
		static VertexShader^ Load(const Platform::Array<byte>^ data);
		void Reload(const Platform::Array<byte>^ data);
		virtual ~VertexShader();
	internal:
		Microsoft::WRL::ComPtr<ID3DBlob> byteCode;
	};
}
