#pragma once
#include "DeviceResources.h"
namespace Coocoo3DGraphics
{
	using namespace Windows::Storage::Streams;
	public ref class PixelShader sealed
	{
	public:
		static PixelShader^ CompileLoad(const Platform::Array<byte>^ sourceCode);
		void CompileReload(const Platform::Array<byte>^ sourceCode);
		bool CompileReload1(const Platform::Array<byte>^ sourceCode, Platform::String^ entryPoint);
		static PixelShader^ Load(const Platform::Array<byte>^ data);
		void Reload(const Platform::Array<byte>^ data);
		virtual ~PixelShader();
	internal:
		Microsoft::WRL::ComPtr<ID3DBlob> byteCode;
		//Microsoft::WRL::ComPtr<ID3D11PixelShader> m_pixelShader;
	};
}

