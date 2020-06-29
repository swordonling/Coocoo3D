#pragma once
#include "DeviceResources.h"
#include "PixelShader.h"
#include "VertexShader.h"
#include "MMDMesh.h"
#include "Material.h"
#include "Texture2D.h"
#include "RenderTexture2D.h"
#include "ConstantBuffer.h"
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
		void SetPObject(PObject^ pobject, CullMode cullMode, BlendState blendState);
		void SetPObjectDepthOnly(PObject^ pobject);
		void UpdateResource(ConstantBuffer^ buffer, const Platform::Array<byte>^ data, UINT sizeInByte);
		void UpdateResource(ConstantBuffer^ buffer, const Platform::Array<byte>^ data, UINT sizeInByte, int dataOffset);
		void UpdateVertices(MMDMesh^ mesh, const Platform::Array<byte>^ verticeData);
		void UpdateVertices2(MMDMesh^ mesh, const Platform::Array<byte>^ verticeData);
		void UpdateVertices2(MMDMesh^ mesh, const Platform::Array<Windows::Foundation::Numerics::float3>^ verticeData);
		void SetSRV(PObjectType type, Texture2D^ texture, int slot);
		void SetSRV_RT(PObjectType type, RenderTexture2D^ texture, int slot);
		void SetConstantBuffer(PObjectType type, ConstantBuffer^ buffer, int slot);
		void SetMMDRender1CBResources(ConstantBuffer^ boneData, ConstantBuffer^ entityData, ConstantBuffer^ presentData, ConstantBuffer^ materialData);
		void Draw(int indexCount, int startIndexLocation);
		void DrawIndexed(int indexCount, int startIndexLocation, int baseVertexLocation);
		void UploadMesh(MMDMesh^ mesh);
		void UploadTexture(Texture2D^ texture);
		void SetMesh(MMDMesh^ mesh);
		void SetRenderTargetScreenAndClear(Windows::Foundation::Numerics::float4 color);
		void SetAndClearDSV(RenderTexture2D^ texture);
		void ClearDepthStencil();
		void BeginCommand();
		void EndCommand();
		void Execute();
	internal:
		DeviceResources^ m_deviceResources;
	};
}