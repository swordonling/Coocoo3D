#include "pch.h"
#include "GraphicsSignature.h"
#include "DirectXHelper.h"

using namespace Coocoo3DGraphics;

void GraphicsSignature::ReloadMMD(DeviceResources ^ deviceResources)
{

	CD3DX12_ROOT_PARAMETER parameter[10];
	CD3DX12_DESCRIPTOR_RANGE range[2];
	range[0].Init(D3D12_DESCRIPTOR_RANGE_TYPE_SRV, 1, 0);
	range[1].Init(D3D12_DESCRIPTOR_RANGE_TYPE_SRV, 1, 1);
	parameter[0].InitAsConstantBufferView(0);
	parameter[1].InitAsConstantBufferView(1);
	parameter[2].InitAsConstantBufferView(2);
	parameter[3].InitAsConstantBufferView(3);
	parameter[4].InitAsDescriptorTable(1, &range[0]);
	parameter[5].InitAsDescriptorTable(1, &range[1]);

	D3D12_STATIC_SAMPLER_DESC staticSamplerDescs[3] = {};
	D3D12_STATIC_SAMPLER_DESC staticSamplerDesc = {};
	staticSamplerDesc.AddressU = D3D12_TEXTURE_ADDRESS_MODE_WRAP;
	staticSamplerDesc.AddressV = D3D12_TEXTURE_ADDRESS_MODE_WRAP;
	staticSamplerDesc.AddressW = D3D12_TEXTURE_ADDRESS_MODE_WRAP;
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
	staticSamplerDescs[2].ComparisonFunc = D3D12_COMPARISON_FUNC_LESS;


	CD3DX12_ROOT_SIGNATURE_DESC rootSignatureDesc;
	//rootSignatureDesc.Init(7, parameter, 3, staticSamplerDescs, D3D12_ROOT_SIGNATURE_FLAG_ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT);
	rootSignatureDesc.Init(6, parameter, 3, staticSamplerDescs, D3D12_ROOT_SIGNATURE_FLAG_ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT);

	Microsoft::WRL::ComPtr<ID3DBlob> signature;
	Microsoft::WRL::ComPtr<ID3DBlob> error;
	DX::ThrowIfFailed(D3D12SerializeRootSignature(&rootSignatureDesc, D3D_ROOT_SIGNATURE_VERSION_1, &signature, &error));
	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->CreateRootSignature(0, signature->GetBufferPointer(), signature->GetBufferSize(), IID_PPV_ARGS(&m_rootSignatures[0])));
}
