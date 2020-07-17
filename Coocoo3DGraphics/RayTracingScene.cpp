#include "pch.h"
#include "RayTracingScene.h"
#include "DirectXHelper.h"
#include "RayTracing/DirectXRaytracingHelper.h"
using namespace Coocoo3DGraphics;
using namespace DX;

void RayTracingScene::SubobjectHitGroup(CD3DX12_HIT_GROUP_SUBOBJECT* hitGroupSubobject, LPCWSTR hitGroupName, LPCWSTR anyHitShaderName, LPCWSTR closestHitShaderName)
{
	hitGroupSubobject->SetAnyHitShaderImport(anyHitShaderName);
	hitGroupSubobject->SetClosestHitShaderImport(closestHitShaderName);
	hitGroupSubobject->SetHitGroupExport(hitGroupName);
	hitGroupSubobject->SetHitGroupType(D3D12_HIT_GROUP_TYPE_TRIANGLES);
}

void RayTracingScene::ReloadPipelineStatesStep0()
{
	raytracingPipeline = { D3D12_STATE_OBJECT_TYPE_RAYTRACING_PIPELINE };
}

void RayTracingScene::ReloadPipelineStatesStep1(const Platform::Array<byte>^ data, const Platform::Array<Platform::String^>^ exportNames)
{
	D3D12_SHADER_BYTECODE libdxil = CD3DX12_SHADER_BYTECODE(data->begin(), data->Length);
	auto lib = raytracingPipeline.CreateSubobject<CD3DX12_DXIL_LIBRARY_SUBOBJECT>();
	lib->SetDXILLibrary(&libdxil);
	for (int i = 0; i < exportNames->Length; i++)
	{
		lib->DefineExport(exportNames[i]->Begin());
	}
}

void RayTracingScene::ReloadPipelineStatesStep2(DeviceResources^ deviceResources, const Platform::Array<HitGroupDesc^>^ hitGroups, UINT payloadSize, UINT attributeSize, UINT maxRecursionDepth)
{

	{
		D3D12_STATIC_SAMPLER_DESC staticSamplerDesc = {};
		staticSamplerDesc.AddressU = D3D12_TEXTURE_ADDRESS_MODE_CLAMP;
		staticSamplerDesc.AddressV = D3D12_TEXTURE_ADDRESS_MODE_CLAMP;
		staticSamplerDesc.AddressW = D3D12_TEXTURE_ADDRESS_MODE_CLAMP;
		staticSamplerDesc.BorderColor = D3D12_STATIC_BORDER_COLOR_OPAQUE_BLACK;
		staticSamplerDesc.ComparisonFunc = D3D12_COMPARISON_FUNC_NEVER;
		staticSamplerDesc.Filter = D3D12_FILTER_MIN_MAG_MIP_LINEAR;
		staticSamplerDesc.MipLODBias = 0;
		staticSamplerDesc.MaxAnisotropy = 0;
		staticSamplerDesc.MinLOD = 0;
		staticSamplerDesc.MaxLOD = D3D12_FLOAT32_MAX;
		staticSamplerDesc.ShaderVisibility = D3D12_SHADER_VISIBILITY_ALL;
		staticSamplerDesc.RegisterSpace = 0;

		CD3DX12_DESCRIPTOR_RANGE UAVDescriptor;
		UAVDescriptor.Init(D3D12_DESCRIPTOR_RANGE_TYPE_UAV, 1, 0);
		CD3DX12_ROOT_PARAMETER rootParameters[3];
		rootParameters[0].InitAsDescriptorTable(1, &UAVDescriptor);
		rootParameters[1].InitAsShaderResourceView(0);
		rootParameters[2].InitAsConstantBufferView(0);
		CD3DX12_ROOT_SIGNATURE_DESC globalRootSignatureDesc(ARRAYSIZE(rootParameters), rootParameters, 1, &staticSamplerDesc);

		auto device = deviceResources->GetD3DDevice();
		Microsoft::WRL::ComPtr<ID3DBlob> blob;
		Microsoft::WRL::ComPtr<ID3DBlob> error;

		DX::ThrowIfFailed(D3D12SerializeRootSignature(&globalRootSignatureDesc, D3D_ROOT_SIGNATURE_VERSION_1, &blob, &error));
		DX::ThrowIfFailed(device->CreateRootSignature(1, blob->GetBufferPointer(), blob->GetBufferSize(), IID_PPV_ARGS(&m_rootSignatures[0])));

	}

	{

		CD3DX12_ROOT_PARAMETER rootParameters[8];
		CD3DX12_DESCRIPTOR_RANGE range[6];
		for (int i = 0; i < 6; i++)
		{
			range[i].Init(D3D12_DESCRIPTOR_RANGE_TYPE_SRV, 1, i + 2, 1);
		}
		rootParameters[0].InitAsConstantBufferView(3);
		rootParameters[1].InitAsShaderResourceView(1, 1);
		rootParameters[2].InitAsDescriptorTable(1, &range[0]);
		rootParameters[3].InitAsDescriptorTable(1, &range[1]);
		rootParameters[4].InitAsDescriptorTable(1, &range[2]);
		rootParameters[5].InitAsDescriptorTable(1, &range[3]);
		rootParameters[6].InitAsDescriptorTable(1, &range[4]);
		rootParameters[7].InitAsDescriptorTable(1, &range[5]);


		CD3DX12_ROOT_SIGNATURE_DESC localRootSignatureDesc(ARRAYSIZE(rootParameters), rootParameters);
		localRootSignatureDesc.Flags = D3D12_ROOT_SIGNATURE_FLAG_LOCAL_ROOT_SIGNATURE;
		auto device = deviceResources->GetD3DDevice();
		Microsoft::WRL::ComPtr<ID3DBlob> blob;
		Microsoft::WRL::ComPtr<ID3DBlob> error;

		DX::ThrowIfFailed(D3D12SerializeRootSignature(&localRootSignatureDesc, D3D_ROOT_SIGNATURE_VERSION_1, &blob, &error));
		DX::ThrowIfFailed(device->CreateRootSignature(1, blob->GetBufferPointer(), blob->GetBufferSize(), IID_PPV_ARGS(&m_rootSignatures[1])));

	}


	for (int i = 0; i < hitGroups->Length; i++)
	{
		SubobjectHitGroup(raytracingPipeline.CreateSubobject<CD3DX12_HIT_GROUP_SUBOBJECT>(),
			hitGroups[i]->HitGroupName->Begin(),
			hitGroups[i]->AnyHitName->Begin() != hitGroups[i]->AnyHitName->End() ? hitGroups[i]->AnyHitName->Begin() : nullptr,
			hitGroups[i]->ClosestHitName->Begin() != hitGroups[i]->ClosestHitName->End() ? hitGroups[i]->ClosestHitName->Begin() : nullptr);
	}


	auto localRootSignature = raytracingPipeline.CreateSubobject<CD3DX12_LOCAL_ROOT_SIGNATURE_SUBOBJECT>();
	localRootSignature->SetRootSignature(m_rootSignatures[1].Get());
	// Shader association
	auto rootSignatureAssociation = raytracingPipeline.CreateSubobject<CD3DX12_SUBOBJECT_TO_EXPORTS_ASSOCIATION_SUBOBJECT>();
	rootSignatureAssociation->SetSubobjectToAssociate(*localRootSignature);
	for (int i = 0; i < hitGroups->Length; i++)
	{
		rootSignatureAssociation->AddExport(hitGroups[i]->HitGroupName->Begin());
	}

	raytracingPipeline.CreateSubobject<CD3DX12_GLOBAL_ROOT_SIGNATURE_SUBOBJECT>()->SetRootSignature(m_rootSignatures[0].Get());

	auto shaderConfig = raytracingPipeline.CreateSubobject<CD3DX12_RAYTRACING_SHADER_CONFIG_SUBOBJECT>();
	shaderConfig->Config(payloadSize, attributeSize);

	auto pipelineConfig = raytracingPipeline.CreateSubobject<CD3DX12_RAYTRACING_PIPELINE_CONFIG_SUBOBJECT>();
	pipelineConfig->Config(maxRecursionDepth);

	auto device = deviceResources->GetD3DDevice5();
	DX::ThrowIfFailed(device->CreateStateObject(raytracingPipeline, IID_PPV_ARGS(&m_dxrStateObject)));
}

void RayTracingScene::ReloadAllocScratchAndInstance(DeviceResources^ deviceResources, UINT scratchSize, UINT instanceCount)
{
	asLastUpdateIndex = 0;
	auto device = deviceResources->GetD3DDevice();
	CD3DX12_HEAP_PROPERTIES defaultHeapProperties(D3D12_HEAP_TYPE_DEFAULT);
	CD3DX12_HEAP_PROPERTIES uploadHeapProperties(D3D12_HEAP_TYPE_UPLOAD);

	for (int i = 0; i < c_frameCount; i++)
	{
		DX::ThrowIfFailed(device->CreateCommittedResource(
			&defaultHeapProperties,
			D3D12_HEAP_FLAG_NONE,
			&CD3DX12_RESOURCE_DESC::Buffer(scratchSize, D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS),
			D3D12_RESOURCE_STATE_UNORDERED_ACCESS,
			nullptr,
			IID_PPV_ARGS(&m_scratchResource[i])));
		NAME_D3D12_OBJECT(m_scratchResource[i]);

		DX::ThrowIfFailed(device->CreateCommittedResource(
			&uploadHeapProperties,
			D3D12_HEAP_FLAG_NONE,
			&CD3DX12_RESOURCE_DESC::Buffer(sizeof(D3D12_RAYTRACING_INSTANCE_DESC) * instanceCount),
			D3D12_RESOURCE_STATE_GENERIC_READ,
			nullptr,
			IID_PPV_ARGS(&instanceDescs[i])));
		NAME_D3D12_OBJECT(instanceDescs[i]);
	}
	if (pArgumentCache)
		free(pArgumentCache);
	pArgumentCache = malloc(instanceCount * c_argumentCacheStride);
	ZeroMemory(pArgumentCache, instanceCount * c_argumentCacheStride);
}

void RayTracingScene::NextASIndex(int meshCount)
{
	asLastUpdateIndex = (asLastUpdateIndex + 1) % c_frameCount;
	m_bottomLevelASs[asLastUpdateIndex].clear();
	m_bottomLevelASs[asLastUpdateIndex].reserve(meshCount);
	m_instanceDescs.clear();
}

void RayTracingScene::NextSTIndex()
{
	stLastUpdateIndex = (stLastUpdateIndex + 1) % c_frameCount;
}

void RayTracingScene::BuildShaderTableStep1(DeviceResources^ deviceResources, const Platform::Array<Platform::String^>^ raygenShaderNames, const Platform::Array<Platform::String^>^ missShaderNames, int argumentSize)
{
	auto device = deviceResources->GetD3DDevice5();

	Microsoft::WRL::ComPtr<ID3D12StateObjectProperties> stateObjectProperties;
	DX::ThrowIfFailed(m_dxrStateObject.As(&stateObjectProperties));

	UINT shaderIdentifierSize = D3D12_SHADER_IDENTIFIER_SIZE_IN_BYTES;

	// Ray gen shader table
	{
		UINT numShaderRecords = raygenShaderNames->Length;
		UINT shaderRecordSize = shaderIdentifierSize + argumentSize;
		ShaderTable rayGenShaderTable(device, numShaderRecords, shaderRecordSize, L"RayGenShaderTable");
		for (int i = 0; i < numShaderRecords; i++)
		{
			rayGenShaderTable.push_back(ShaderRecord(stateObjectProperties->GetShaderIdentifier(raygenShaderNames[i]->Begin()), shaderIdentifierSize));
		}
		m_rayGenShaderTable[stLastUpdateIndex] = rayGenShaderTable.GetResource();
	}

	// Miss shader table
	{
		UINT numShaderRecords = missShaderNames->Length;
		UINT shaderRecordSize = shaderIdentifierSize + argumentSize;
		ShaderTable missShaderTable(device, numShaderRecords, shaderRecordSize, L"MissShaderTable");
		for (int i = 0; i < numShaderRecords; i++)
		{
			missShaderTable.push_back(ShaderRecord(stateObjectProperties->GetShaderIdentifier(missShaderNames[i]->Begin()), shaderIdentifierSize));
		}
		m_missShaderTable[stLastUpdateIndex] = missShaderTable.GetResource();
		m_missShaderTableStrideInBytes = missShaderTable.GetShaderRecordSize();
	}
}

void RayTracingScene::BuildShaderTableStep2(DeviceResources^ deviceResources, const Platform::Array <Platform::String^>^ hitGroupNames, int argumentSize, int instances)
{

	auto device = deviceResources->GetD3DDevice5();
	UINT shaderIdentifierSize = D3D12_SHADER_IDENTIFIER_SIZE_IN_BYTES;
	Microsoft::WRL::ComPtr<ID3D12StateObjectProperties> stateObjectProperties;
	DX::ThrowIfFailed(m_dxrStateObject.As(&stateObjectProperties));
	// Hit group shader table
	{
		UINT numShaderRecords = instances * hitGroupNames->Length;
		UINT shaderRecordSize = shaderIdentifierSize + argumentSize;
		ShaderTable hitGroupShaderTable(device, numShaderRecords, shaderRecordSize, L"HitGroupShaderTable");
		for (int i = 0; i < instances; i++)
		{
			for (int j = 0; j < hitGroupNames->Length; j++)
			{
				hitGroupShaderTable.push_back(ShaderRecord(stateObjectProperties->GetShaderIdentifier(hitGroupNames[j]->Begin()), shaderIdentifierSize, (byte*)pArgumentCache + (i * c_argumentCacheStride), argumentSize));
			}
		}
		m_hitGroupShaderTable[stLastUpdateIndex] = hitGroupShaderTable.GetResource();
		m_hitGroupShaderTableStrideInBytes = hitGroupShaderTable.GetShaderRecordSize();
	}
}

RayTracingScene::~RayTracingScene()
{
	if (pArgumentCache)
		free(pArgumentCache);
	pArgumentCache = nullptr;
}
