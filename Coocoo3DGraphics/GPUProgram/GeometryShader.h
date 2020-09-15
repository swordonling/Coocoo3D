#pragma once
#include "ShaderMacro.h"
namespace Coocoo3DGraphics
{
	public ref class GeometryShader sealed
	{
	public:
		property bool Ready;
		property Platform::Object^ LoadTask;
		property Platform::String^ Path;
		static GeometryShader^ CompileLoad(const Platform::Array<byte>^ sourceCode);
		void CompileReload(const Platform::Array<byte>^ sourceCode);
		//bool CompileReload1(const Platform::Array<byte>^ sourceCode, Platform::String^ entryPoint);
		bool CompileReload1(const Platform::Array<byte>^ sourceCode, Platform::String^ entryPoint, ShaderMacro macro);
		static GeometryShader^ Load(const Platform::Array<byte>^ data);
		void Reload(const Platform::Array<byte>^ data);
	internal:
		//Microsoft::WRL::ComPtr<ID3D11GeometryShader> m_geometryShader;
		Microsoft::WRL::ComPtr<ID3DBlob> byteCode;
	};
}
