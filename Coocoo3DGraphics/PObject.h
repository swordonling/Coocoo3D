#pragma once
#include "DeviceResources.h"
#include "VertexShader.h"
#include "PixelShader.h"
#include "GeometryShader.h"
#include "GraphicsSignature.h"
namespace Coocoo3DGraphics
{
	public enum struct CullMode
	{
		none = 0,
		front = 1,
		back = 2,
	};
	public enum struct BlendState
	{
		none = 0,
		alpha = 1,
	};
	public enum struct PObjectType
	{
		mmd = 0,
		mmdDepth = 1,
		ui3d = 2,
	};
	public ref class PObject sealed
	{
	public:
		property bool Ready;
		property Platform::Object^ LoadTask;
		property Platform::String^ Path;
		void Reload(DeviceResources^ deviceResources, GraphicsSignature ^ graphicsSignature, PObjectType type, VertexShader^ vertexShader, GeometryShader^ geometryShader, PixelShader^ pixelShader);
		void Reload(PObject^ pObject);
	internal:
		VertexShader^ m_vertexShader;
		PixelShader^ m_pixelShader;
		GeometryShader^ m_geometryShader;


		Microsoft::WRL::ComPtr<ID3D12PipelineState>			m_pipelineState[10];
	private:
	};
}
