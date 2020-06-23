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
		static VertexShader^ LoadParticle(DeviceResources^ deviceResources, const Platform::Array<byte>^ data);
		void ReloadParticle(DeviceResources^ deviceResources, const Platform::Array<byte>^ data);
		static VertexShader^ LoadSkyBox(DeviceResources^ deviceResources, const Platform::Array<byte>^ data);
		void ReloadSkyBox(DeviceResources^ deviceResources, const Platform::Array<byte>^ data);
		static VertexShader^ LoadUI(DeviceResources^ deviceResources, const Platform::Array<byte>^ data);
		void ReloadUI(DeviceResources^ deviceResources, const Platform::Array<byte>^ data);
		virtual ~VertexShader();
	internal:
		Microsoft::WRL::ComPtr<ID3D11VertexShader> m_vertexShader;
		Microsoft::WRL::ComPtr<ID3D11InputLayout> m_inputLayout;
	};
}
