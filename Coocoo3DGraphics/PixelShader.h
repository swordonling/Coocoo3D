#pragma once
#include "DeviceResources.h"
namespace Coocoo3DGraphics
{
	public ref class PixelShader sealed
	{
	public:
		static PixelShader^ CompileLoad(DeviceResources^ deviceResources, const Platform::Array<byte>^ sourceCode);
		void CompileReload(DeviceResources^ deviceResources, const Platform::Array<byte>^ sourceCode);
		static PixelShader^ Load(DeviceResources^ deviceResources, const Platform::Array<byte>^ data);
		void Reload(DeviceResources^ deviceResources, const Platform::Array<byte>^ data);
		virtual ~PixelShader();
	internal:
		Microsoft::WRL::ComPtr<ID3DBlob> byteCode;
		//Microsoft::WRL::ComPtr<ID3D11PixelShader> m_pixelShader;
	};
}

