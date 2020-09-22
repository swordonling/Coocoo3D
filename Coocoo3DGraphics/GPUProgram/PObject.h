#pragma once
#include "DeviceResources.h"
#include "VertexShader.h"
#include "PixelShader.h"
#include "GeometryShader.h"
#include "GraphicsSignature.h"
#include "GraphicsObjectStatus.h"
namespace Coocoo3DGraphics
{
	public enum struct CullMode
	{
		back = 0,
		none = 1,
		front = 2,
	};
	public enum struct BlendState
	{
		none = 0,
		alpha = 1,
	};
	public enum struct PObjectType
	{
		mmd = 0,
		postProcess = 1,
		ui3d = 2,
	};
	public ref class PObject sealed
	{
	public:
		property GraphicsObjectStatus Status;
		property Platform::Object^ LoadTask;
		property Platform::String^ Path;
		void Reload(DeviceResources^ deviceResources, GraphicsSignature^ graphicsSignature, PObjectType type, BlendState blendState, VertexShader^ vertexShader, GeometryShader^ geometryShader, PixelShader^ pixelShader, DxgiFormat rtvFormat);
		void ReloadDepthOnly(DeviceResources^ deviceResources, GraphicsSignature^ graphicsSignature, VertexShader^ vsTransform, PixelShader^ psDepthAlphaClip, int depthOffset);
		bool ReloadSkinning(DeviceResources^ deviceResources, GraphicsSignature^ graphicsSignature, VertexShader^ vs, GeometryShader^ gs);
		bool ReloadDrawing(DeviceResources^ deviceResources, GraphicsSignature^ graphicsSignature, BlendState blendState, VertexShader^ vs, GeometryShader^ gs, PixelShader^ ps, DxgiFormat rtvFormat);
		void Reload(PObject^ pObject);
		void Unload();
	internal:
		VertexShader^ m_vertexShader;
		PixelShader^ m_pixelShader;
		GeometryShader^ m_geometryShader;
		static const UINT c_indexPipelineStateDepth = 6;
		static const UINT c_indexPipelineStateSkinning = 7;

		Microsoft::WRL::ComPtr<ID3D12PipelineState>			m_pipelineState[10];
	private:
	};
}