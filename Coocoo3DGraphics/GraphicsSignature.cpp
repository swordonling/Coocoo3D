#include "pch.h"
#include "GraphicsSignature.h"
#include "DirectXHelper.h"

using namespace Coocoo3DGraphics;
#define SizeOfInUint32(obj) ((sizeof(obj) - 1) / sizeof(UINT32) + 1)

void GraphicsSignature::ReloadMMD(DeviceResources^ deviceResources)
{

	CD3DX12_ROOT_PARAMETER1 parameter[10];
	CD3DX12_DESCRIPTOR_RANGE1 range[6];
	range[0].Init(D3D12_DESCRIPTOR_RANGE_TYPE_SRV, 1, 0);
	range[1].Init(D3D12_DESCRIPTOR_RANGE_TYPE_SRV, 1, 1);
	range[2].Init(D3D12_DESCRIPTOR_RANGE_TYPE_SRV, 1, 2);
	range[3].Init(D3D12_DESCRIPTOR_RANGE_TYPE_SRV, 1, 3);
	range[4].Init(D3D12_DESCRIPTOR_RANGE_TYPE_SRV, 1, 4);
	range[5].Init(D3D12_DESCRIPTOR_RANGE_TYPE_SRV, 1, 5);
	parameter[0].InitAsConstantBufferView(0, 0, D3D12_ROOT_DESCRIPTOR_FLAG_DATA_STATIC_WHILE_SET_AT_EXECUTE);
	parameter[1].InitAsConstantBufferView(1, 0, D3D12_ROOT_DESCRIPTOR_FLAG_DATA_STATIC_WHILE_SET_AT_EXECUTE);
	parameter[2].InitAsConstantBufferView(2, 0, D3D12_ROOT_DESCRIPTOR_FLAG_DATA_STATIC_WHILE_SET_AT_EXECUTE);
	parameter[3].InitAsConstantBufferView(3, 0, D3D12_ROOT_DESCRIPTOR_FLAG_DATA_STATIC_WHILE_SET_AT_EXECUTE);
	parameter[4].InitAsDescriptorTable(1, &range[0]);
	parameter[5].InitAsDescriptorTable(1, &range[1]);
	parameter[6].InitAsDescriptorTable(1, &range[2]);
	parameter[7].InitAsDescriptorTable(1, &range[3]);
	parameter[8].InitAsDescriptorTable(1, &range[4]);
	parameter[9].InitAsDescriptorTable(1, &range[5]);

	D3D12_STATIC_SAMPLER_DESC staticSamplerDescs[3] = {};
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
	staticSamplerDesc.ShaderVisibility = D3D12_SHADER_VISIBILITY_PIXEL;
	staticSamplerDesc.RegisterSpace = 0;
	staticSamplerDescs[0] = staticSamplerDesc;
	staticSamplerDescs[1] = staticSamplerDesc;
	staticSamplerDescs[2] = staticSamplerDesc;
	staticSamplerDescs[0].ShaderRegister = 0;
	staticSamplerDescs[1].ShaderRegister = 1;
	staticSamplerDescs[2].ShaderRegister = 2;
	//staticSamplerDescs[2].ComparisonFunc = D3D12_COMPARISON_FUNC_LESS;


	CD3DX12_VERSIONED_ROOT_SIGNATURE_DESC rootSignatureDesc;
	rootSignatureDesc.Init_1_1(_countof(parameter), parameter, 3, staticSamplerDescs, D3D12_ROOT_SIGNATURE_FLAG_ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT | D3D12_ROOT_SIGNATURE_FLAG_ALLOW_STREAM_OUTPUT);

	Microsoft::WRL::ComPtr<ID3DBlob> signature;
	Microsoft::WRL::ComPtr<ID3DBlob> error;
	DX::ThrowIfFailed(D3DX12SerializeVersionedRootSignature(&rootSignatureDesc, D3D_ROOT_SIGNATURE_VERSION_1, &signature, &error));
	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->CreateRootSignature(0, signature->GetBufferPointer(), signature->GetBufferSize(), IID_PPV_ARGS(&m_rootSignature)));
}

void Sign1(DeviceResources^ deviceResources, const Platform::Array<GraphicSignatureDesc>^ Descs, Microsoft::WRL::ComPtr<ID3D12RootSignature>& m_sign, D3D12_ROOT_SIGNATURE_FLAGS flags)
{
	UINT descCount = Descs->Length;
	void* mem1 = malloc(sizeof(CD3DX12_ROOT_PARAMETER1) * descCount + sizeof(CD3DX12_DESCRIPTOR_RANGE1) * descCount);
	CD3DX12_ROOT_PARAMETER1* parameters = (CD3DX12_ROOT_PARAMETER1*)mem1;
	CD3DX12_DESCRIPTOR_RANGE1* ranges = (CD3DX12_DESCRIPTOR_RANGE1*)((byte*)mem1 + sizeof(CD3DX12_ROOT_PARAMETER1) * descCount);

	int cbvCount = 0;
	int srvCount = 0;
	int uavCount = 0;

	for (int i = 0; i < descCount; i++)
	{
		if (Descs[i] == GraphicSignatureDesc::CBV)
		{
			parameters[i].InitAsConstantBufferView(cbvCount);
			cbvCount++;
		}
		else if (Descs[i] == GraphicSignatureDesc::SRV)
		{
			parameters[i].InitAsShaderResourceView(srvCount);
			srvCount++;
		}
		else if (Descs[i] == GraphicSignatureDesc::UAV)
		{
			parameters[i].InitAsUnorderedAccessView(uavCount);
			uavCount++;
		}
		else if (Descs[i] == GraphicSignatureDesc::CBVTable)
		{
			ranges[i].Init(D3D12_DESCRIPTOR_RANGE_TYPE_CBV, 1, cbvCount);
			parameters[i].InitAsDescriptorTable(1, &ranges[i]);
			cbvCount++;
		}
		else if (Descs[i] == GraphicSignatureDesc::SRVTable)
		{
			ranges[i].Init(D3D12_DESCRIPTOR_RANGE_TYPE_SRV, 1, srvCount);
			parameters[i].InitAsDescriptorTable(1, &ranges[i]);
			srvCount++;
		}
		else if (Descs[i] == GraphicSignatureDesc::UAVTable)
		{
			ranges[i].Init(D3D12_DESCRIPTOR_RANGE_TYPE_UAV, 1, uavCount);
			parameters[i].InitAsDescriptorTable(1, &ranges[i]);
			uavCount++;
		}
	}

	D3D12_STATIC_SAMPLER_DESC staticSamplerDescs[3] = {};
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
	staticSamplerDescs[0] = staticSamplerDesc;
	staticSamplerDescs[1] = staticSamplerDesc;
	staticSamplerDescs[2] = staticSamplerDesc;
	staticSamplerDescs[0].ShaderRegister = 0;
	staticSamplerDescs[1].ShaderRegister = 1;
	staticSamplerDescs[2].ShaderRegister = 2;

	CD3DX12_VERSIONED_ROOT_SIGNATURE_DESC rootSignatureDesc;
	rootSignatureDesc.Init_1_1(descCount, parameters, 3, staticSamplerDescs, flags);

	Microsoft::WRL::ComPtr<ID3DBlob> signature;
	Microsoft::WRL::ComPtr<ID3DBlob> error;
	DX::ThrowIfFailed(D3DX12SerializeVersionedRootSignature(&rootSignatureDesc, D3D_ROOT_SIGNATURE_VERSION_1, &signature, &error));
	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->CreateRootSignature(0, signature->GetBufferPointer(), signature->GetBufferSize(), IID_PPV_ARGS(&m_sign)));

	free(mem1);
}

void GraphicsSignature::Reload(DeviceResources^ deviceResources, const Platform::Array<GraphicSignatureDesc>^ Descs)
{
	Sign1(deviceResources, Descs, m_rootSignature, D3D12_ROOT_SIGNATURE_FLAG_ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT | D3D12_ROOT_SIGNATURE_FLAG_ALLOW_STREAM_OUTPUT);
}

void GraphicsSignature::ReloadCompute(DeviceResources^ deviceResources, const Platform::Array<GraphicSignatureDesc>^ Descs)
{
	Sign1(deviceResources, Descs, m_rootSignature, D3D12_ROOT_SIGNATURE_FLAG_NONE);
}

void GraphicsSignature::Unload()
{
	m_rootSignature.Reset();
}
