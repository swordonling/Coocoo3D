#pragma once
#include "DeviceResources.h"
#include "PixelShader.h"
#include "VertexShader.h"
#include "MMDMesh.h"
#include "Material.h"
#include "Texture2D.h"
#include "RenderTexture2D.h"
#include "ConstantBuffer.h"
#include "GraphicsSignature.h"
namespace Coocoo3DGraphics
{
	public enum struct D3D12ResourceStates
	{
		_COMMON = 0,
		_VERTEX_AND_CONSTANT_BUFFER = 0x1,
		_INDEX_BUFFER = 0x2,
		_RENDER_TARGET = 0x4,
		_UNORDERED_ACCESS = 0x8,
		_DEPTH_WRITE = 0x10,
		_DEPTH_READ = 0x20,
		_NON_PIXEL_SHADER_RESOURCE = 0x40,
		_PIXEL_SHADER_RESOURCE = 0x80,
		_STREAM_OUT = 0x100,
		_INDIRECT_ARGUMENT = 0x200,
		_COPY_DEST = 0x400,
		_COPY_SOURCE = 0x800,
		_RESOLVE_DEST = 0x1000,
		_RESOLVE_SOURCE = 0x2000,
		_RAYTRACING_ACCELERATION_STRUCTURE = 0x400000,
		_GENERIC_READ = (((((0x1 | 0x2) | 0x40) | 0x80) | 0x200) | 0x800),
		_PRESENT = 0,
		_PREDICATION = 0x200,
		_VIDEO_DECODE_READ = 0x10000,
		_VIDEO_DECODE_WRITE = 0x20000,
		_VIDEO_PROCESS_READ = 0x40000,
		_VIDEO_PROCESS_WRITE = 0x80000,
		_VIDEO_ENCODE_READ = 0x200000,
		_VIDEO_ENCODE_WRITE = 0x800000
	};
	//��D3D��C# �ӿ�
	//Ϊ�˼�C++����ı�д��
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
		void SetRootSignature(GraphicsSignature^ rootSignature);
		void ResourceBarrierScreen(D3D12ResourceStates before, D3D12ResourceStates after);
		void ClearDepthStencil();
		static void BeginAlloctor(DeviceResources^ deviceResources);
		void BeginCommand();
		void EndCommand();
		void Execute();
	internal:
		DeviceResources^ m_deviceResources;
		Microsoft::WRL::ComPtr<ID3D12GraphicsCommandList>	m_commandList;
	};
}