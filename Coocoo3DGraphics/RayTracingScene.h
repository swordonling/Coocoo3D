#pragma once
#include "DeviceResources.h"
#include "GraphicsSignature.h"
#include "MMDMesh.h"
namespace Coocoo3DGraphics
{
	public ref class RayTracingScene sealed
	{
	public:
		void ReloadPipelineStatesStep0();
		void ReloadPipelineStatesStep1(const Platform::Array<byte>^ data, const Platform::Array<Platform::String^>^ exportNames);
		void ReloadPipelineStatesStep2(DeviceResources^ deviceResources, const Platform::Array<Platform::String^>^ hitGroupNames, const Platform::Array<Platform::String^>^ closestHitNames);
		void ReloadPipelineStatesStep3(UINT payloadSize, UINT attributeSize, UINT maxRecursionDepth);
		void ReloadPipelineStatesStep4(DeviceResources^ deviceResources);
		void ReloadAllocScratchAndInstance(DeviceResources^ deviceResources, UINT scratchSize, UINT instanceCount);
		void NextASIndex(int meshCount);
		void NextSTIndex();
		void BuildShaderTableStep1(DeviceResources^ deviceResources, const Platform::Array<Platform::String^>^ raygenShaderNames, const Platform::Array<Platform::String^>^ missShaderNames, int argumentSize);
		void BuildShaderTableStep2(DeviceResources^ deviceResources, Platform::String^ hitGroupName, int argumentSize,int instances);
		virtual ~RayTracingScene();
	internal:
		static const int c_argumentCacheStride = 128;

		Microsoft::WRL::ComPtr<ID3D12RootSignature> m_rootSignatures[10];

		Microsoft::WRL::ComPtr<ID3D12StateObject> m_dxrStateObject;
		int stLastUpdateIndex = 0;
		Microsoft::WRL::ComPtr<ID3D12Resource> m_missShaderTable[c_frameCount];
		Microsoft::WRL::ComPtr<ID3D12Resource> m_hitGroupShaderTable[c_frameCount];
		Microsoft::WRL::ComPtr<ID3D12Resource> m_rayGenShaderTable[c_frameCount];

		UINT m_hitGroupShaderTableStrideInBytes;
		UINT m_missShaderTableStrideInBytes;


		Microsoft::WRL::ComPtr<ID3D12Resource> m_topLevelAccelerationStructure[c_frameCount];
		UINT m_topLevelAccelerationStructureSize[c_frameCount] = {};
		std::vector<Microsoft::WRL::ComPtr<ID3D12Resource>>m_bottomLevelASs[c_frameCount];

		Microsoft::WRL::ComPtr<ID3D12Resource> instanceDescs[c_frameCount];
		Microsoft::WRL::ComPtr<ID3D12Resource> m_scratchResource[c_frameCount];
		int asLastUpdateIndex = 0;

		std::vector <D3D12_RAYTRACING_INSTANCE_DESC> m_instanceDescs;

		CD3DX12_STATE_OBJECT_DESC raytracingPipeline;
		void* pArgumentCache = nullptr;
	private:
		void SubobjectHitGroup(CD3DX12_HIT_GROUP_SUBOBJECT* hitGroupSubobject, LPCWSTR hitGroupName, LPCWSTR anyHitShaderName, LPCWSTR closestHitShaderName);
	};
}
