#pragma once
#include "DeviceResources.h"
namespace Coocoo3DGraphics
{
	public ref class ComputeShader sealed
	{
	public:
		property bool Ready;
		property Platform::Object^ LoadTask;
		property Platform::String^ Path;
		static ComputeShader^ CompileLoad(DeviceResources^ deviceResources, const Platform::Array<byte>^ sourceCode);
		void CompileReload(DeviceResources^ deviceResources, const Platform::Array<byte>^ sourceCode);
		static ComputeShader^ Load(DeviceResources^ deviceResources, const Platform::Array<byte>^ data);
		void Reload(DeviceResources^ deviceResources, const Platform::Array<byte>^ data);
	internal:
		Microsoft::WRL::ComPtr<ID3D11ComputeShader> m_computeShader;
	};
}
