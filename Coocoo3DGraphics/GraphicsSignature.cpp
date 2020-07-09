#include "pch.h"
#include "GraphicsSignature.h"
#include "DirectXHelper.h"

using namespace Coocoo3DGraphics;

void GraphicsSignature::ReloadMMD(DeviceResources^ deviceResources)
{

	CD3DX12_ROOT_PARAMETER1 parameter[10];
	CD3DX12_DESCRIPTOR_RANGE1 range[3];
	range[0].Init(D3D12_DESCRIPTOR_RANGE_TYPE_SRV, 1, 0);
	range[1].Init(D3D12_DESCRIPTOR_RANGE_TYPE_SRV, 1, 1);
	range[2].Init(D3D12_DESCRIPTOR_RANGE_TYPE_SRV, 1, 2);
	parameter[0].InitAsConstantBufferView(0, 0, D3D12_ROOT_DESCRIPTOR_FLAG_DATA_STATIC, D3D12_SHADER_VISIBILITY_VERTEX);
	parameter[1].InitAsConstantBufferView(1, 0, D3D12_ROOT_DESCRIPTOR_FLAG_DATA_STATIC_WHILE_SET_AT_EXECUTE);
	parameter[2].InitAsConstantBufferView(2, 0, D3D12_ROOT_DESCRIPTOR_FLAG_DATA_STATIC_WHILE_SET_AT_EXECUTE);
	parameter[3].InitAsConstantBufferView(3, 0, D3D12_ROOT_DESCRIPTOR_FLAG_DATA_STATIC_WHILE_SET_AT_EXECUTE);
	parameter[4].InitAsDescriptorTable(1, &range[0]);
	parameter[5].InitAsDescriptorTable(1, &range[1]);
	parameter[6].InitAsDescriptorTable(1, &range[2]);

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
	staticSamplerDescs[2].AddressU = D3D12_TEXTURE_ADDRESS_MODE_CLAMP;
	staticSamplerDescs[2].AddressV = D3D12_TEXTURE_ADDRESS_MODE_CLAMP;
	staticSamplerDescs[2].AddressW = D3D12_TEXTURE_ADDRESS_MODE_CLAMP;
	staticSamplerDescs[2].ShaderRegister = 2;
	//staticSamplerDescs[2].ComparisonFunc = D3D12_COMPARISON_FUNC_LESS;


	CD3DX12_VERSIONED_ROOT_SIGNATURE_DESC rootSignatureDesc;
	rootSignatureDesc.Init_1_1(7, parameter, 3, staticSamplerDescs, D3D12_ROOT_SIGNATURE_FLAG_ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT);

	Microsoft::WRL::ComPtr<ID3DBlob> signature;
	Microsoft::WRL::ComPtr<ID3DBlob> error;
	DX::ThrowIfFailed(D3DX12SerializeVersionedRootSignature(&rootSignatureDesc, D3D_ROOT_SIGNATURE_VERSION_1, &signature, &error));
	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->CreateRootSignature(0, signature->GetBufferPointer(), signature->GetBufferSize(), IID_PPV_ARGS(&m_rootSignatures[0])));
}

void GraphicsSignature::Reload(DeviceResources^ deviceResources, const Platform::Array<GraphicSignatureDesc>^ Descs)
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
	rootSignatureDesc.Init_1_1(descCount, parameters, 3, staticSamplerDescs, D3D12_ROOT_SIGNATURE_FLAG_ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT);

	Microsoft::WRL::ComPtr<ID3DBlob> signature;
	Microsoft::WRL::ComPtr<ID3DBlob> error;
	DX::ThrowIfFailed(D3DX12SerializeVersionedRootSignature(&rootSignatureDesc, D3D_ROOT_SIGNATURE_VERSION_1, &signature, &error));
	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->CreateRootSignature(0, signature->GetBufferPointer(), signature->GetBufferSize(), IID_PPV_ARGS(&m_rootSignatures[0])));

	free(mem1);
}

void GraphicsSignature::Unload()
{
	for (int i = 0; i < _countof(m_rootSignatures); i++)
	{
		m_rootSignatures[i].Reset();
	}
}
