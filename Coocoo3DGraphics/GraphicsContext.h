#pragma once
#include "DeviceResources.h"
#include "PixelShader.h"
#include "VertexShader.h"
#include "MMDMesh.h"
#include "Material.h"
#include "Texture2D.h"
#include "GraphicsBuffer.h"
#include "ConstantBuffer.h"
#include "BlendState.h"
#include "ComputeShader.h"
namespace Coocoo3DGraphics
{
	//是D3D的C# 接口
	//为了简化C++代码的编写。
	public ref class GraphicsContext sealed
	{
	public:
		static GraphicsContext^ Load(DeviceResources^ deviceResources);
		void Reload(DeviceResources^ deviceResources);
		void SetMaterial(Material^ material);
		void SetPObject(PObject^ pobject);
		void SetPObjectDepthOnly(PObject^ pobject);
		void SetCullMode(CullMode cullMode);
		void SetComputeShader(ComputeShader^ computeShader);
		void UpdateResource(ConstantBuffer^ buffer, const Platform::Array<byte>^ data);
		void UpdateResource(ConstantBuffer^ buffer, const Platform::Array<byte>^ data,int dataOffset);
		void UpdateResource(GraphicsBuffer^ buffer, const Platform::Array<byte>^data);
		void UpdateVertices(MMDMesh^ mesh, const Platform::Array<byte>^ verticeData);
		void UpdateVertices2(MMDMesh^ mesh, const Platform::Array<byte>^ verticeData);
		void UpdateVertices2(MMDMesh^ mesh, const Platform::Array<Windows::Foundation::Numerics::float3>^ verticeData);
		void CSSetSRV(GraphicsBuffer^ buffer, int slot);
		void CSSetSRV(Texture2D^ texture, int slot);
		void VSSetSRV(Texture2D^ texture, int slot);
		void PSSetSRV(Texture2D^ texture, int slot);
		void CSSetUAV(Texture2D^ texture, int slot);
		void CSSetConstantBuffer(ConstantBuffer^ buffer, int slot);
		void VSSetConstantBuffer(ConstantBuffer^ buffer, int slot);
		void GSSetConstantBuffer(ConstantBuffer^ buffer, int slot);
		void PSSetConstantBuffer(ConstantBuffer^ buffer, int slot);
		void Dispathch(int x, int y, int z);
		void Draw(int indexCount, int startIndexLocation);
		void DrawIndexed(int indexCount, int startIndexLocation, int baseVertexLocation);
		void SetMesh(MMDMesh^ mesh);
		void SetRenderTargetScreenAndClear(Windows::Foundation::Numerics::float4 color);
		void SetAndClearDSV(Texture2D^ texture);
		void SetBlendState(BlendState^ blendState);
		void ClearDepthStencil();
	internal:
		DeviceResources^ m_deviceResources;
	};
}