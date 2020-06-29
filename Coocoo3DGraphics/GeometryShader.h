#pragma once
#include "DeviceResources.h"
namespace Coocoo3DGraphics
{
	public ref class GeometryShader sealed
	{
	public:
		property bool Ready;
		property Platform::Object^ LoadTask;
		property Platform::String^ Path;
		static GeometryShader^ CompileLoad(DeviceResources^ deviceResources, const Platform::Array<byte>^ sourceCode);
		void CompileReload(DeviceResources^ deviceResources, const Platform::Array<byte>^ sourceCode);
		static GeometryShader^ Load(DeviceResources^ deviceResources, const Platform::Array<byte>^ data);
		void Reload(DeviceResources^ deviceResources, const Platform::Array<byte>^ data);
	internal:
		//Microsoft::WRL::ComPtr<ID3D11GeometryShader> m_geometryShader;
		Microsoft::WRL::ComPtr<ID3DBlob> byteCode;
	};
}
