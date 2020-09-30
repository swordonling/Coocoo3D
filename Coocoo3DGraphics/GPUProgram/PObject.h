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
	public enum struct eInputLayout
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
		void Reload(DeviceResources^ deviceResources, GraphicsSignature^ graphicsSignature, eInputLayout type, BlendState blendState, VertexShader^ vertexShader, GeometryShader^ geometryShader, PixelShader^ pixelShader, DxgiFormat rtvFormat);
		//使用Upload上传GPU
		void ReloadDepthOnly(VertexShader^ vs, PixelShader^ ps, int depthOffset);
		//使用Upload上传GPU
		void ReloadSkinning(VertexShader^ vs, GeometryShader^ gs);
		//使用Upload上传GPU
		void ReloadDrawing(BlendState blendState, VertexShader^ vs, GeometryShader^ gs, PixelShader^ ps, DxgiFormat rtvFormat);
		bool Upload(DeviceResources^ deviceResources, GraphicsSignature^ graphicsSignature);
		void Unload();
	internal:
		VertexShader^ m_vertexShader;
		PixelShader^ m_pixelShader;
		GeometryShader^ m_geometryShader;
		static const UINT c_indexPipelineStateDepth = 6;
		static const UINT c_indexPipelineStateSkinning = 7;

		bool m_useStreamOutput;
		bool m_isDepthOnly;
		DXGI_FORMAT m_renderTargetFormat;
		BlendState m_blendState;
		int m_depthBias;
		Microsoft::WRL::ComPtr<ID3D12PipelineState>			m_pipelineState[10];

		inline void ClearState()
		{
			m_vertexShader = nullptr;
			m_pixelShader = nullptr;
			m_geometryShader = nullptr;
			m_renderTargetFormat = DXGI_FORMAT_UNKNOWN;
			m_useStreamOutput = false;
			m_blendState = BlendState::none;
			m_depthBias = 0;
		}
	};
}
