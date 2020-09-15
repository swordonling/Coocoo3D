#pragma once
#include "ShaderMacro.h"
namespace Coocoo3DGraphics
{
	using namespace Windows::Storage::Streams;
	public ref class PixelShader sealed
	{
	public:
		bool CompileReload1(const Platform::Array<byte>^ sourceCode, Platform::String^ entryPoint, ShaderMacro macro);
		static PixelShader^ Load(const Platform::Array<byte>^ data);
		void Reload(const Platform::Array<byte>^ data);
		virtual ~PixelShader();
	internal:
		Microsoft::WRL::ComPtr<ID3DBlob> byteCode;
	};
}

