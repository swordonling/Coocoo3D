#include "pch.h"
#include "DirectXHelper.h"
#include "GraphicsContext.h"
using namespace Coocoo3DGraphics;

struct wicToDxgiFormat
{
	DXGI_FORMAT dxgiFormat;
	WICPixelFormatGUID fallbackWicFormat;
};
struct GUIDComparer
{
	bool operator()(const GUID& Left, const GUID& Right) const
	{
		return memcmp(&Left, &Right, sizeof(GUID)) < 0;
	}
};

const std::map<GUID, wicToDxgiFormat, GUIDComparer>wicFormatInfo =
{
	{GUID_WICPixelFormat128bppRGBAFloat,{DXGI_FORMAT_R32G32B32A32_FLOAT,0}},
	{GUID_WICPixelFormat32bppPRGBA,{DXGI_FORMAT_R8G8B8A8_UNORM_SRGB,0}},
	{GUID_WICPixelFormat32bppRGBA,{DXGI_FORMAT_R8G8B8A8_UNORM_SRGB,0}},
	{GUID_WICPixelFormat32bppBGRA,{DXGI_FORMAT_B8G8R8A8_UNORM_SRGB,0}},
	{GUID_WICPixelFormat32bppBGR,{DXGI_FORMAT_B8G8R8A8_UNORM_SRGB,0}},
	{GUID_WICPixelFormat16bppBGR565,{DXGI_FORMAT_B5G6R5_UNORM,0}},
	{GUID_WICPixelFormat24bppBGR,{DXGI_FORMAT_UNKNOWN,GUID_WICPixelFormat32bppRGBA}},
	{GUID_WICPixelFormat8bppIndexed,{DXGI_FORMAT_UNKNOWN,GUID_WICPixelFormat32bppRGBA}},
};
const std::map<DXGI_FORMAT, UINT>dxgiFormatBytesPerPixel =
{
	{DXGI_FORMAT_R32G32B32A32_FLOAT,16},
	{DXGI_FORMAT_R8G8B8A8_UNORM,4},
	{DXGI_FORMAT_R8G8B8A8_UNORM_SRGB,4},
	{DXGI_FORMAT_B8G8R8A8_UNORM,4},
	{DXGI_FORMAT_B8G8R8A8_UNORM_SRGB,4},
	{DXGI_FORMAT_B5G6R5_UNORM,2},
};

GraphicsContext^ GraphicsContext::Load(DeviceResources^ deviceResources)
{
	GraphicsContext^ graphicsContext = ref new GraphicsContext();
	graphicsContext->m_deviceResources = deviceResources;
	return graphicsContext;
}

void GraphicsContext::Reload(DeviceResources^ deviceResources)
{
	m_deviceResources = deviceResources;
	auto d3dDevice = deviceResources->GetD3DDevice();
	DX::ThrowIfFailed(d3dDevice->CreateCommandList(0, D3D12_COMMAND_LIST_TYPE_DIRECT, m_deviceResources->GetCommandAllocator(), nullptr, IID_PPV_ARGS(&m_commandList)));
	NAME_D3D12_OBJECT(m_commandList);
	DX::ThrowIfFailed(m_commandList->Close());
}

void GraphicsContext::SetMaterial(Material^ material)
{
	//auto context = m_deviceResources->GetD3DDeviceContext();

	//if (material->m_pObject->m_vertexShader != nullptr) {
	//	context->IASetInputLayout(material->m_pObject->m_inputLayout.Get());
	//	context->VSSetShader(material->m_pObject->m_vertexShader.Get(), nullptr, 0);
	//}
	//else {
	//	context->VSSetShader(nullptr, nullptr, 0);
	//}
	//if (material->m_pObject->m_geometryShader != nullptr) {
	//	context->GSSetShader(material->m_pObject->m_geometryShader->m_geometryShader.Get(), nullptr, 0);
	//}
	//else {
	//	context->GSSetShader(nullptr, nullptr, 0);
	//}
	//if (material->m_pObject->m_pixelShader != nullptr) {
	//	context->PSSetShader(material->m_pObject->m_pixelShader->m_pixelShader.Get(), nullptr, 0);
	//}
	//else {
	//	context->PSSetShader(nullptr, nullptr, 0);
	//}

	//if (material->cullMode == CullMode::none)
	//	context->RSSetState(material->m_pObject->m_RasterizerStateCullNone.Get());
	//else if (material->cullMode == CullMode::front)
	//	context->RSSetState(material->m_pObject->m_RasterizerStateCullFront.Get());
	//else if (material->cullMode == CullMode::back)
	//	context->RSSetState(material->m_pObject->m_RasterizerStateCullBack.Get());
	//for (int i = 0; i < Material::c_reference_max; i++)
	//{
	//	if (material->references[i] != nullptr&&material->references[i]->m_texture2D != nullptr) {
	//		context->PSSetShaderResources(i, 1, material->references[i]->m_shaderResourceView.GetAddressOf());
	//		context->PSSetSamplers(i, 1, material->references[i]->m_samplerState.GetAddressOf());
	//	}
	//}
}

void GraphicsContext::SetPObject(PObject^ pObject, CullMode cullMode, BlendState blendState)
{
	int a = (int)cullMode;
	a += (int)blendState * 3;

	m_commandList->SetPipelineState(pObject->m_pipelineState[a].Get());

}

void GraphicsContext::SetPObject(PObject^ pObject, int index)
{
	m_commandList->SetPipelineState(pObject->m_pipelineState[index].Get());
}

void GraphicsContext::SetPObjectDepthOnly(PObject^ pObject)
{
	m_commandList->SetPipelineState(pObject->m_pipelineState[PObject::c_indexPipelineStateDepth].Get());
}

void GraphicsContext::SetPObjectStreamOut(PObject^ pObject)
{
	m_commandList->SetPipelineState(pObject->m_pipelineState[PObject::c_indexPipelineStateSkinning].Get());
}

void GraphicsContext::SetPObject(ComputePO^ pObject)
{
	m_commandList->SetPipelineState(pObject->m_pipelineState.Get());
}

void GraphicsContext::UpdateResource(ConstantBuffer^ buffer, const Platform::Array<byte>^ data, UINT sizeInByte)
{
	buffer->lastUpdateIndex = (buffer->lastUpdateIndex + 1) % c_frameCount;
	memcpy(buffer->m_mappedConstantBuffer + buffer->lastUpdateIndex * buffer->Size, data->begin(), sizeInByte);
}

void GraphicsContext::UpdateResource(ConstantBuffer^ buffer, const Platform::Array<byte>^ data, UINT sizeInByte, int dataOffset)
{
	buffer->lastUpdateIndex = (buffer->lastUpdateIndex + 1) % c_frameCount;
	memcpy(buffer->m_mappedConstantBuffer + buffer->lastUpdateIndex * buffer->Size, data->begin() + dataOffset, sizeInByte);
}

void GraphicsContext::UpdateResource(ConstantBuffer^ buffer, const Platform::Array<Windows::Foundation::Numerics::float4x4>^ data, UINT sizeInByte, int dataOffset)
{
	buffer->lastUpdateIndex = (buffer->lastUpdateIndex + 1) % c_frameCount;
	memcpy(buffer->m_mappedConstantBuffer + buffer->lastUpdateIndex * buffer->Size, data->begin() + dataOffset, sizeInByte);
}

inline void UpdateCBStaticResource(ConstantBufferStatic^ buffer, ID3D12GraphicsCommandList* commmandList, void* data, UINT sizeInByte, int dataOffset)
{
	buffer->lastUpdateIndex = (buffer->lastUpdateIndex + 1) % c_frameCount;
	int lastUpdateIndex = buffer->lastUpdateIndex;

	D3D12_SUBRESOURCE_DATA bufferData = {};
	bufferData.pData = (byte*)data + dataOffset;
	bufferData.RowPitch = sizeInByte;
	bufferData.SlicePitch = sizeInByte;
	commmandList->ResourceBarrier(1, &CD3DX12_RESOURCE_BARRIER::Transition(buffer->m_constantBuffers[lastUpdateIndex].Get(), D3D12_RESOURCE_STATE_GENERIC_READ, D3D12_RESOURCE_STATE_COPY_DEST));
	UpdateSubresources(commmandList, buffer->m_constantBuffers[lastUpdateIndex].Get(), buffer->m_constantBufferUpload.Get(), 0, 0, 1, &bufferData);
	commmandList->ResourceBarrier(1, &CD3DX12_RESOURCE_BARRIER::Transition(buffer->m_constantBuffers[lastUpdateIndex].Get(), D3D12_RESOURCE_STATE_COPY_DEST, D3D12_RESOURCE_STATE_GENERIC_READ));
}

void GraphicsContext::UpdateResource(ConstantBufferStatic^ buffer, const Platform::Array<byte>^ data, UINT sizeInByte)
{
	UpdateCBStaticResource(buffer, m_commandList.Get(), data->begin(), sizeInByte, 0);
}

void GraphicsContext::UpdateResource(ConstantBufferStatic^ buffer, const Platform::Array<byte>^ data, UINT sizeInByte, int dataOffset)
{
	UpdateCBStaticResource(buffer, m_commandList.Get(), data->begin(), sizeInByte, dataOffset);
}

void GraphicsContext::UpdateResource(ConstantBufferStatic^ buffer, const Platform::Array<Windows::Foundation::Numerics::float4x4>^ data, UINT sizeInByte, int dataOffset)
{
	UpdateCBStaticResource(buffer, m_commandList.Get(), data->begin(), sizeInByte, dataOffset);
}

void GraphicsContext::UpdateVertices(MMDMesh^ mesh, const Platform::Array<byte>^ verticeData)
{
	//auto context = m_deviceResources->GetD3DDeviceContext();
	//context->UpdateSubresource(mesh->m_vertexBuffer.Get(), 0, nullptr, verticeData->begin(), 0, 0);
}

inline void _UpdateVerticesPos(ID3D12GraphicsCommandList* commandList, ID3D12Resource* resource, ID3D12Resource* uploaderResource, void* dataBegin, UINT dataLength)
{
	D3D12_SUBRESOURCE_DATA vertexData = {};
	vertexData.pData = dataBegin;
	vertexData.RowPitch = dataLength;
	vertexData.SlicePitch = vertexData.RowPitch;
	commandList->ResourceBarrier(1, &CD3DX12_RESOURCE_BARRIER::Transition(resource, D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER, D3D12_RESOURCE_STATE_COPY_DEST));
	UpdateSubresources(commandList, resource, uploaderResource, 0, 0, 1, &vertexData);
	commandList->ResourceBarrier(1, &CD3DX12_RESOURCE_BARRIER::Transition(resource, D3D12_RESOURCE_STATE_COPY_DEST, D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER));
}

void GraphicsContext::UpdateVerticesPos0(MMDMesh^ mesh, const Platform::Array<byte>^ verticeData)
{
	mesh->lastUpdateIndex0++;
	mesh->lastUpdateIndex0 %= c_frameCount;
	_UpdateVerticesPos(m_commandList.Get(), mesh->m_vertexBufferPos0[mesh->lastUpdateIndex0].Get(), mesh->m_vertexBufferPosUpload0->Get(), verticeData->begin(), verticeData->Length);
}

void GraphicsContext::UpdateVerticesPos0(MMDMesh^ mesh, const Platform::Array<Windows::Foundation::Numerics::float3>^ verticeData)
{
	mesh->lastUpdateIndex0++;
	mesh->lastUpdateIndex0 %= c_frameCount;
	_UpdateVerticesPos(m_commandList.Get(), mesh->m_vertexBufferPos0[mesh->lastUpdateIndex0].Get(), mesh->m_vertexBufferPosUpload0->Get(), verticeData->begin(), verticeData->Length * sizeof(Windows::Foundation::Numerics::float3));
}

void GraphicsContext::UpdateVerticesPos1(MMDMesh^ mesh, const Platform::Array<Windows::Foundation::Numerics::float3>^ verticeData)
{
	mesh->lastUpdateIndex1++;
	mesh->lastUpdateIndex1 %= c_frameCount;
	_UpdateVerticesPos(m_commandList.Get(), mesh->m_vertexBufferPos1[mesh->lastUpdateIndex1].Get(), mesh->m_vertexBufferPosUpload1->Get(), verticeData->begin(), verticeData->Length * sizeof(Windows::Foundation::Numerics::float3));
}

void GraphicsContext::SetSRVRSkinnedMesh(MMDMesh^ mesh, int index)
{
	if (mesh->prevStateSkinnedVertice != D3D12_RESOURCE_STATE_GENERIC_READ)
		m_commandList->ResourceBarrier(1, &CD3DX12_RESOURCE_BARRIER::Transition(mesh->m_skinnedVertice.Get(), mesh->prevStateSkinnedVertice, D3D12_RESOURCE_STATE_GENERIC_READ));
	mesh->prevStateSkinnedVertice = D3D12_RESOURCE_STATE_GENERIC_READ;
	m_commandList->SetGraphicsRootShaderResourceView(index, mesh->m_skinnedVertice->GetGPUVirtualAddress());
}

void GraphicsContext::SetSRVR(StaticBuffer^ buffer, int index)
{
	m_commandList->SetGraphicsRootShaderResourceView(index, buffer->m_buffer->GetGPUVirtualAddress());
}

void GraphicsContext::SetSRVT(Texture2D^ texture, int index)
{
	auto d3dDevice = m_deviceResources->GetD3DDevice();
	UINT incrementSize = d3dDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);
	if (texture != nullptr)
	{
		CD3DX12_GPU_DESCRIPTOR_HANDLE gpuHandle(m_deviceResources->m_graphicsPipelineHeap->GetGPUDescriptorHandleForHeapStart(), texture->m_heapRefIndex, incrementSize);
		m_commandList->SetGraphicsRootDescriptorTable(index, gpuHandle);
	}
	else
	{
		throw ref new Platform::NotImplementedException();
	}
}

void GraphicsContext::SetSRVT(TextureCube^ texture, int index)
{
	auto d3dDevice = m_deviceResources->GetD3DDevice();
	UINT incrementSize = d3dDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);
	if (texture != nullptr)
	{
		CD3DX12_GPU_DESCRIPTOR_HANDLE gpuHandle(m_deviceResources->m_graphicsPipelineHeap->GetGPUDescriptorHandleForHeapStart(), texture->m_heapRefIndex, incrementSize);
		m_commandList->SetGraphicsRootDescriptorTable(index, gpuHandle);
	}
	else
	{
		throw ref new Platform::NotImplementedException();
	}
}

void GraphicsContext::SetSRVT(RenderTexture2D^ texture, int index)
{
	auto d3dDevice = m_deviceResources->GetD3DDevice();
	UINT incrementSize = d3dDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);
	if (texture != nullptr)
	{
		if (texture->prevResourceState != D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE)
			m_commandList->ResourceBarrier(1, &CD3DX12_RESOURCE_BARRIER::Transition(texture->m_texture.Get(), texture->prevResourceState, D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE));
		texture->prevResourceState = D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE;

		CD3DX12_GPU_DESCRIPTOR_HANDLE gpuHandle(m_deviceResources->m_graphicsPipelineHeap->GetGPUDescriptorHandleForHeapStart(), texture->m_srvRefIndex, incrementSize);
		m_commandList->SetGraphicsRootDescriptorTable(index, gpuHandle);
	}
	else
	{
		throw ref new Platform::NotImplementedException();
	}
}

void GraphicsContext::SetSRVT(RenderTextureCube^ texture, int index)
{
	auto d3dDevice = m_deviceResources->GetD3DDevice();
	UINT incrementSize = d3dDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);
	if (texture != nullptr)
	{
		if (texture->prevResourceState != D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE)
			m_commandList->ResourceBarrier(1, &CD3DX12_RESOURCE_BARRIER::Transition(texture->m_texture.Get(), texture->prevResourceState, D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE));
		texture->prevResourceState = D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE;

		CD3DX12_GPU_DESCRIPTOR_HANDLE gpuHandle(m_deviceResources->m_graphicsPipelineHeap->GetGPUDescriptorHandleForHeapStart(), texture->m_srvRefIndex, incrementSize);
		m_commandList->SetGraphicsRootDescriptorTable(index, gpuHandle);
	}
	else
	{
		throw ref new Platform::NotImplementedException();
	}
}

void GraphicsContext::SetCBVR(ConstantBuffer^ buffer, int index)
{
	m_commandList->SetGraphicsRootConstantBufferView(index, buffer->GetCurrentVirtualAddress());
}

void GraphicsContext::SetCBVR(ConstantBufferStatic^ buffer, int index)
{
	m_commandList->SetGraphicsRootConstantBufferView(index, buffer->GetCurrentVirtualAddress());
}

void GraphicsContext::SetUAVT(RenderTexture2D^ texture, int index)
{
	auto d3dDevice = m_deviceResources->GetD3DDevice();
	UINT incrementSize = d3dDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);
	if (texture != nullptr)
	{
		if (texture->prevResourceState != D3D12_RESOURCE_STATE_UNORDERED_ACCESS)
			m_commandList->ResourceBarrier(1, &CD3DX12_RESOURCE_BARRIER::Transition(texture->m_texture.Get(), texture->prevResourceState, D3D12_RESOURCE_STATE_UNORDERED_ACCESS));
		texture->prevResourceState = D3D12_RESOURCE_STATE_UNORDERED_ACCESS;

		CD3DX12_GPU_DESCRIPTOR_HANDLE gpuHandle(m_deviceResources->m_graphicsPipelineHeap->GetGPUDescriptorHandleForHeapStart(), texture->m_uavRefIndex, incrementSize);
		m_commandList->SetGraphicsRootDescriptorTable(index, gpuHandle);
	}
	else
	{
		throw ref new Platform::NotImplementedException();
	}
}

void GraphicsContext::SetComputeSRVT(TextureCube^ texture, int index)
{
	auto d3dDevice = m_deviceResources->GetD3DDevice();
	UINT incrementSize = d3dDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);
	if (texture != nullptr)
	{
		CD3DX12_GPU_DESCRIPTOR_HANDLE gpuHandle(m_deviceResources->m_graphicsPipelineHeap->GetGPUDescriptorHandleForHeapStart(), texture->m_heapRefIndex, incrementSize);
		m_commandList->SetComputeRootDescriptorTable(index, gpuHandle);
	}
	else
	{
		throw ref new Platform::NotImplementedException();
	}
}

void GraphicsContext::SetComputeSRVT(RenderTextureCube^ texture, int index)
{
	auto d3dDevice = m_deviceResources->GetD3DDevice();
	UINT incrementSize = d3dDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);
	if (texture != nullptr)
	{
		if (texture->prevResourceState != D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE)
			m_commandList->ResourceBarrier(1, &CD3DX12_RESOURCE_BARRIER::Transition(texture->m_texture.Get(), texture->prevResourceState, D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE));
		texture->prevResourceState = D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE;

		CD3DX12_GPU_DESCRIPTOR_HANDLE gpuHandle(m_deviceResources->m_graphicsPipelineHeap->GetGPUDescriptorHandleForHeapStart(), texture->m_srvRefIndex, incrementSize);
		m_commandList->SetComputeRootDescriptorTable(index, gpuHandle);
	}
	else
	{
		throw ref new Platform::NotImplementedException();
	}
}

void GraphicsContext::SetComputeCBVR(ConstantBuffer^ buffer, int index)
{
	m_commandList->SetComputeRootConstantBufferView(index, buffer->GetCurrentVirtualAddress());
}

void GraphicsContext::SetComputeCBVR(ConstantBufferStatic^ buffer, int index)
{
	m_commandList->SetComputeRootConstantBufferView(index, buffer->GetCurrentVirtualAddress());
}

void GraphicsContext::SetComputeUAVT(RenderTexture2D^ texture, int index)
{
	auto d3dDevice = m_deviceResources->GetD3DDevice();
	UINT incrementSize = d3dDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);
	if (texture != nullptr)
	{
		if (texture->prevResourceState != D3D12_RESOURCE_STATE_UNORDERED_ACCESS)
			m_commandList->ResourceBarrier(1, &CD3DX12_RESOURCE_BARRIER::Transition(texture->m_texture.Get(), texture->prevResourceState, D3D12_RESOURCE_STATE_UNORDERED_ACCESS));
		else
			m_commandList->ResourceBarrier(1, &CD3DX12_RESOURCE_BARRIER::UAV(texture->m_texture.Get()));
		texture->prevResourceState = D3D12_RESOURCE_STATE_UNORDERED_ACCESS;

		CD3DX12_GPU_DESCRIPTOR_HANDLE gpuHandle(m_deviceResources->m_graphicsPipelineHeap->GetGPUDescriptorHandleForHeapStart(), texture->m_uavRefIndex, incrementSize);
		m_commandList->SetComputeRootDescriptorTable(index, gpuHandle);
	}
	else
	{
		throw ref new Platform::NotImplementedException();
	}
}

void GraphicsContext::SetComputeUAVT(RenderTextureCube^ texture, int index)
{
	auto d3dDevice = m_deviceResources->GetD3DDevice();
	UINT incrementSize = d3dDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);
	if (texture != nullptr)
	{
		if (texture->prevResourceState != D3D12_RESOURCE_STATE_UNORDERED_ACCESS)
			m_commandList->ResourceBarrier(1, &CD3DX12_RESOURCE_BARRIER::Transition(texture->m_texture.Get(), texture->prevResourceState, D3D12_RESOURCE_STATE_UNORDERED_ACCESS));
		else
			m_commandList->ResourceBarrier(1, &CD3DX12_RESOURCE_BARRIER::UAV(texture->m_texture.Get()));
		texture->prevResourceState = D3D12_RESOURCE_STATE_UNORDERED_ACCESS;

		CD3DX12_GPU_DESCRIPTOR_HANDLE gpuHandle(m_deviceResources->m_graphicsPipelineHeap->GetGPUDescriptorHandleForHeapStart(), texture->m_uavRefIndex, incrementSize);
		m_commandList->SetComputeRootDescriptorTable(index, gpuHandle);
	}
	else
	{
		throw ref new Platform::NotImplementedException();
	}
}

void GraphicsContext::SetSOMesh(MMDMesh^ mesh)
{
	if (mesh != nullptr)
	{
		if (mesh->prevStateSkinnedVertice != D3D12_RESOURCE_STATE_COPY_DEST)
			m_commandList->ResourceBarrier(1, &CD3DX12_RESOURCE_BARRIER::Transition(mesh->m_skinnedVertice.Get(), mesh->prevStateSkinnedVertice, D3D12_RESOURCE_STATE_COPY_DEST));
		mesh->prevStateSkinnedVertice = D3D12_RESOURCE_STATE_COPY_DEST;
		D3D12_WRITEBUFFERIMMEDIATE_PARAMETER parameter = { mesh->m_skinnedVertice->GetGPUVirtualAddress() + mesh->m_indexCount * 64,0 };
		D3D12_WRITEBUFFERIMMEDIATE_MODE modes[] = { D3D12_WRITEBUFFERIMMEDIATE_MODE_MARKER_IN };
		m_commandList->WriteBufferImmediate(1, &parameter, modes);
		if (mesh->prevStateSkinnedVertice != D3D12_RESOURCE_STATE_STREAM_OUT)
			m_commandList->ResourceBarrier(1, &CD3DX12_RESOURCE_BARRIER::Transition(mesh->m_skinnedVertice.Get(), mesh->prevStateSkinnedVertice, D3D12_RESOURCE_STATE_STREAM_OUT));
		mesh->prevStateSkinnedVertice = D3D12_RESOURCE_STATE_STREAM_OUT;
		m_commandList->SOSetTargets(0, 1, &mesh->m_skinnedVerticeStreamOutputBufferView);
	}
	else
	{
		D3D12_STREAM_OUTPUT_BUFFER_VIEW bufferView = {};
		m_commandList->SOSetTargets(0, 1, &bufferView);
	}
}

void GraphicsContext::Draw(int vertexCount, int startVertexLocation)
{
	m_commandList->DrawInstanced(vertexCount, 1, startVertexLocation, 0);
}

void GraphicsContext::DrawIndexed(int indexCount, int startIndexLocation, int baseVertexLocation)
{
	m_commandList->DrawIndexedInstanced(indexCount, 1, startIndexLocation, baseVertexLocation, 0);
}

void GraphicsContext::Dispatch(int x, int y, int z)
{
	m_commandList->Dispatch(x, y, z);
}

void GraphicsContext::DoRayTracing(RayTracingScene^ rayTracingScene, int asIndex)
{
	//m_commandList->SetComputeRootSignature(rayTracingScene->m_rootSignatures[0].Get());
	m_commandList->SetComputeRootShaderResourceView(asIndex, rayTracingScene->m_topLevelAccelerationStructure[rayTracingScene->asLastUpdateIndex]->GetGPUVirtualAddress());

	int lastUpdateIndexRtpso = rayTracingScene->stLastUpdateIndex;
	D3D12_DISPATCH_RAYS_DESC dispatchDesc = {};
	dispatchDesc.HitGroupTable.StartAddress = rayTracingScene->m_hitGroupShaderTable[lastUpdateIndexRtpso]->GetGPUVirtualAddress();
	dispatchDesc.HitGroupTable.SizeInBytes = rayTracingScene->m_hitGroupShaderTable[lastUpdateIndexRtpso]->GetDesc().Width;
	dispatchDesc.HitGroupTable.StrideInBytes = rayTracingScene->m_hitGroupShaderTableStrideInBytes;
	dispatchDesc.MissShaderTable.StartAddress = rayTracingScene->m_missShaderTable[lastUpdateIndexRtpso]->GetGPUVirtualAddress();
	dispatchDesc.MissShaderTable.SizeInBytes = rayTracingScene->m_missShaderTable[lastUpdateIndexRtpso]->GetDesc().Width;
	dispatchDesc.MissShaderTable.StrideInBytes = rayTracingScene->m_missShaderTableStrideInBytes;
	dispatchDesc.RayGenerationShaderRecord.StartAddress = rayTracingScene->m_rayGenShaderTable[lastUpdateIndexRtpso]->GetGPUVirtualAddress();
	dispatchDesc.RayGenerationShaderRecord.SizeInBytes = rayTracingScene->m_rayGenShaderTable[lastUpdateIndexRtpso]->GetDesc().Width;
	dispatchDesc.Width = (UINT)m_deviceResources->GetOutputSize().Width;
	dispatchDesc.Height = (UINT)m_deviceResources->GetOutputSize().Height;
	dispatchDesc.Depth = 1;
	m_commandList->SetPipelineState1(rayTracingScene->m_dxrStateObject.Get());
	m_commandList->DispatchRays(&dispatchDesc);
}

void GraphicsContext::UploadMesh(MMDMesh^ mesh)
{
	auto d3dDevice = m_deviceResources->GetD3DDevice();

	CD3DX12_HEAP_PROPERTIES defaultHeapProperties(D3D12_HEAP_TYPE_DEFAULT);
	CD3DX12_HEAP_PROPERTIES uploadHeapProperties(D3D12_HEAP_TYPE_UPLOAD);
	if (mesh->m_verticeData->Length > 0)
	{
		CD3DX12_RESOURCE_DESC vertexBufferDesc = CD3DX12_RESOURCE_DESC::Buffer(mesh->m_verticeData->Length);
		DX::ThrowIfFailed(d3dDevice->CreateCommittedResource(
			&defaultHeapProperties,
			D3D12_HEAP_FLAG_NONE,
			&vertexBufferDesc,
			D3D12_RESOURCE_STATE_COPY_DEST,
			nullptr,
			IID_PPV_ARGS(&mesh->m_vertexBuffer)));
		NAME_D3D12_OBJECT(mesh->m_vertexBuffer);

		DX::ThrowIfFailed(d3dDevice->CreateCommittedResource(
			&uploadHeapProperties,
			D3D12_HEAP_FLAG_NONE,
			&vertexBufferDesc,
			D3D12_RESOURCE_STATE_GENERIC_READ,
			nullptr,
			IID_PPV_ARGS(&mesh->m_vertexBufferUpload)));
		NAME_D3D12_OBJECT(mesh->m_vertexBufferUpload);

		D3D12_SUBRESOURCE_DATA vertexData = {};
		vertexData.pData = mesh->m_verticeData->begin();
		vertexData.RowPitch = mesh->m_verticeData->Length;
		vertexData.SlicePitch = vertexData.RowPitch;

		UpdateSubresources(m_commandList.Get(), mesh->m_vertexBuffer.Get(), mesh->m_vertexBufferUpload.Get(), 0, 0, 1, &vertexData);

		m_commandList->ResourceBarrier(1, &CD3DX12_RESOURCE_BARRIER::Transition(mesh->m_vertexBuffer.Get(), D3D12_RESOURCE_STATE_COPY_DEST, D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER));
	}
	if (mesh->m_verticeDataPos->Length > 0)
	{
		CD3DX12_RESOURCE_DESC vertexBufferDesc = CD3DX12_RESOURCE_DESC::Buffer(mesh->m_verticeDataPos->Length);
		for (int i = 0; i < c_frameCount; i++)
		{
			DX::ThrowIfFailed(d3dDevice->CreateCommittedResource(
				&defaultHeapProperties,
				D3D12_HEAP_FLAG_NONE,
				&vertexBufferDesc,
				D3D12_RESOURCE_STATE_COPY_DEST,
				nullptr,
				IID_PPV_ARGS(&mesh->m_vertexBufferPos0[i])));
			NAME_D3D12_OBJECT(mesh->m_vertexBufferPos0[i]);

			vertexBufferDesc = CD3DX12_RESOURCE_DESC::Buffer(mesh->m_verticeDataPos->Length);
			DX::ThrowIfFailed(d3dDevice->CreateCommittedResource(
				&uploadHeapProperties,
				D3D12_HEAP_FLAG_NONE,
				&vertexBufferDesc,
				D3D12_RESOURCE_STATE_GENERIC_READ,
				nullptr,
				IID_PPV_ARGS(&mesh->m_vertexBufferPosUpload0[i])));
			NAME_D3D12_OBJECT(mesh->m_vertexBufferPosUpload0[i]);
		}
		for (int i = 0; i < c_frameCount; i++)
		{
			DX::ThrowIfFailed(d3dDevice->CreateCommittedResource(
				&defaultHeapProperties,
				D3D12_HEAP_FLAG_NONE,
				&vertexBufferDesc,
				D3D12_RESOURCE_STATE_COPY_DEST,
				nullptr,
				IID_PPV_ARGS(&mesh->m_vertexBufferPos1[i])));
			NAME_D3D12_OBJECT(mesh->m_vertexBufferPos1[i]);

			vertexBufferDesc = CD3DX12_RESOURCE_DESC::Buffer(mesh->m_verticeDataPos->Length);
			DX::ThrowIfFailed(d3dDevice->CreateCommittedResource(
				&uploadHeapProperties,
				D3D12_HEAP_FLAG_NONE,
				&vertexBufferDesc,
				D3D12_RESOURCE_STATE_GENERIC_READ,
				nullptr,
				IID_PPV_ARGS(&mesh->m_vertexBufferPosUpload1[i])));
			NAME_D3D12_OBJECT(mesh->m_vertexBufferPosUpload1[i]);
		}

		D3D12_SUBRESOURCE_DATA vertexData = {};
		vertexData.pData = mesh->m_verticeDataPos->begin();
		vertexData.RowPitch = mesh->m_verticeDataPos->Length;
		vertexData.SlicePitch = vertexData.RowPitch;
		for (int i = 0; i < c_frameCount; i++)
			UpdateSubresources(m_commandList.Get(), mesh->m_vertexBufferPos0[i].Get(), mesh->m_vertexBufferPosUpload0[i].Get(), 0, 0, 1, &vertexData);
		for (int i = 0; i < c_frameCount; i++)
			UpdateSubresources(m_commandList.Get(), mesh->m_vertexBufferPos1[i].Get(), mesh->m_vertexBufferPosUpload1[i].Get(), 0, 0, 1, &vertexData);
		CD3DX12_RESOURCE_BARRIER barriers[c_frameCount * 2];
		for (int i = 0; i < c_frameCount; i++)
			barriers[i] = { CD3DX12_RESOURCE_BARRIER::Transition(mesh->m_vertexBufferPos0[i].Get(), D3D12_RESOURCE_STATE_COPY_DEST, D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER) };
		for (int i = 0; i < c_frameCount; i++)
			barriers[i + c_frameCount] = { CD3DX12_RESOURCE_BARRIER::Transition(mesh->m_vertexBufferPos1[i].Get(), D3D12_RESOURCE_STATE_COPY_DEST, D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER) };

		m_commandList->ResourceBarrier(c_frameCount * 2, barriers);
	}
	if (mesh->m_indexData->Length > 0)
	{
		CD3DX12_RESOURCE_DESC indexBufferDesc = CD3DX12_RESOURCE_DESC::Buffer(mesh->m_indexData->Length);
		DX::ThrowIfFailed(d3dDevice->CreateCommittedResource(
			&defaultHeapProperties,
			D3D12_HEAP_FLAG_NONE,
			&indexBufferDesc,
			D3D12_RESOURCE_STATE_COPY_DEST,
			nullptr,
			IID_PPV_ARGS(&mesh->m_indexBuffer)));
		NAME_D3D12_OBJECT(mesh->m_indexBuffer);

		DX::ThrowIfFailed(d3dDevice->CreateCommittedResource(
			&uploadHeapProperties,
			D3D12_HEAP_FLAG_NONE,
			&indexBufferDesc,
			D3D12_RESOURCE_STATE_GENERIC_READ,
			nullptr,
			IID_PPV_ARGS(&mesh->m_indexBufferUpload)));
		NAME_D3D12_OBJECT(mesh->m_indexBufferUpload);

		D3D12_SUBRESOURCE_DATA indexData = {};
		indexData.pData = mesh->m_indexData->begin();
		indexData.RowPitch = mesh->m_indexData->Length;
		indexData.SlicePitch = indexData.RowPitch;

		UpdateSubresources(m_commandList.Get(), mesh->m_indexBuffer.Get(), mesh->m_indexBufferUpload.Get(), 0, 0, 1, &indexData);
		m_commandList->ResourceBarrier(1, &CD3DX12_RESOURCE_BARRIER::Transition(mesh->m_indexBuffer.Get(), D3D12_RESOURCE_STATE_COPY_DEST, D3D12_RESOURCE_STATE_INDEX_BUFFER));
	}
	mesh->lastUpdateIndex0 = 0;

	{
		CD3DX12_RESOURCE_DESC vertexBufferDesc = CD3DX12_RESOURCE_DESC::Buffer(mesh->m_indexCount * MMDMesh::c_skinnedVerticeStride + 64);
		DX::ThrowIfFailed(d3dDevice->CreateCommittedResource(
			&defaultHeapProperties,
			D3D12_HEAP_FLAG_NONE,
			&vertexBufferDesc,
			D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER,
			nullptr,
			IID_PPV_ARGS(&mesh->m_skinnedVertice)));
		NAME_D3D12_OBJECT(mesh->m_skinnedVertice);
	}

	// 创建顶点/索引缓冲区视图。
	if (mesh->m_verticeData->Length > 0)
	{
		mesh->m_vertexBufferView.BufferLocation = mesh->m_vertexBuffer->GetGPUVirtualAddress();
		mesh->m_vertexBufferView.StrideInBytes = mesh->m_vertexStride;
		mesh->m_vertexBufferView.SizeInBytes = mesh->m_vertexStride * mesh->m_vertexCount;
	}
	if (mesh->m_verticeDataPos->Length > 0)
	{
		for (int i = 0; i < c_frameCount; i++)
		{
			mesh->m_vertexBufferPosView0[i].BufferLocation = mesh->m_vertexBufferPos0[i]->GetGPUVirtualAddress();
			mesh->m_vertexBufferPosView0[i].StrideInBytes = mesh->m_vertexStride2;
			mesh->m_vertexBufferPosView0[i].SizeInBytes = mesh->m_vertexStride2 * mesh->m_vertexCount;

			mesh->m_vertexBufferPosView1[i].BufferLocation = mesh->m_vertexBufferPos1[i]->GetGPUVirtualAddress();
			mesh->m_vertexBufferPosView1[i].StrideInBytes = mesh->m_vertexStride2;
			mesh->m_vertexBufferPosView1[i].SizeInBytes = mesh->m_vertexStride2 * mesh->m_vertexCount;
		}
	}
	if (mesh->m_indexData->Length > 0)
	{
		mesh->m_indexBufferView.BufferLocation = mesh->m_indexBuffer->GetGPUVirtualAddress();
		mesh->m_indexBufferView.SizeInBytes = mesh->m_indexCount * mesh->m_indexStride;
		mesh->m_indexBufferView.Format = DXGI_FORMAT_R32_UINT;
	}

	{
		mesh->m_skinnedVerticeVertexBufferView.BufferLocation = mesh->m_skinnedVertice->GetGPUVirtualAddress();
		mesh->m_skinnedVerticeVertexBufferView.StrideInBytes = MMDMesh::c_skinnedVerticeStride;
		mesh->m_skinnedVerticeVertexBufferView.SizeInBytes = mesh->m_indexCount * MMDMesh::c_skinnedVerticeStride;

		mesh->m_skinnedVerticeStreamOutputBufferView.BufferLocation = mesh->m_skinnedVertice->GetGPUVirtualAddress();
		mesh->m_skinnedVerticeStreamOutputBufferView.BufferFilledSizeLocation = mesh->m_skinnedVertice->GetGPUVirtualAddress() + mesh->m_indexCount * MMDMesh::c_skinnedVerticeStride;
		mesh->m_skinnedVerticeStreamOutputBufferView.SizeInBytes = mesh->m_indexCount * MMDMesh::c_skinnedVerticeStride;
		mesh->prevStateSkinnedVertice = D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER;
	}
}

void GraphicsContext::UploadTexture(ITexture^ texture)
{
	Texture2D^ tex2D = dynamic_cast<Texture2D^>(texture);
	TextureCube^ texCube = dynamic_cast<TextureCube^>(texture);

	auto d3dDevice = m_deviceResources->GetD3DDevice();
	if (tex2D != nullptr)
	{
		{
			D3D12_RESOURCE_DESC textureDesc = {};
			textureDesc.MipLevels = 1;
			textureDesc.Format = tex2D->m_format;
			textureDesc.Width = tex2D->m_width;
			textureDesc.Height = tex2D->m_height;
			textureDesc.Flags = D3D12_RESOURCE_FLAG_NONE;
			textureDesc.DepthOrArraySize = 1;
			textureDesc.SampleDesc.Count = 1;
			textureDesc.SampleDesc.Quality = 0;
			textureDesc.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D;

			DX::ThrowIfFailed(d3dDevice->CreateCommittedResource(
				&CD3DX12_HEAP_PROPERTIES(D3D12_HEAP_TYPE_DEFAULT),
				D3D12_HEAP_FLAG_NONE,
				&textureDesc,
				D3D12_RESOURCE_STATE_COPY_DEST,
				nullptr,
				IID_PPV_ARGS(&tex2D->m_texture)));
			NAME_D3D12_OBJECT(tex2D->m_texture);

			DX::ThrowIfFailed(d3dDevice->CreateCommittedResource(
				&CD3DX12_HEAP_PROPERTIES(D3D12_HEAP_TYPE_UPLOAD),
				D3D12_HEAP_FLAG_NONE,
				&CD3DX12_RESOURCE_DESC::Buffer(tex2D->m_textureData->Length),
				D3D12_RESOURCE_STATE_GENERIC_READ,
				nullptr,
				IID_PPV_ARGS(&tex2D->m_textureUpload)));

			D3D12_SUBRESOURCE_DATA textureData = {};
			textureData.pData = tex2D->m_textureData->begin();
			textureData.RowPitch = tex2D->m_textureData->Length / tex2D->m_height;
			textureData.SlicePitch = tex2D->m_textureData->Length;

			UpdateSubresources(m_commandList.Get(), tex2D->m_texture.Get(), tex2D->m_textureUpload.Get(), 0, 0, 1, &textureData);

			CD3DX12_RESOURCE_BARRIER textureResourceBarrier =
				CD3DX12_RESOURCE_BARRIER::Transition(tex2D->m_texture.Get(), D3D12_RESOURCE_STATE_COPY_DEST, D3D12_RESOURCE_STATE_GENERIC_READ);
			m_commandList->ResourceBarrier(1, &textureResourceBarrier);
		}
		{
			UINT incrementSize = d3dDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);
			tex2D->m_heapRefIndex = m_deviceResources->m_graphicsPipelineHeapAllocCount;
			m_deviceResources->m_graphicsPipelineHeapAllocCount++;

			D3D12_SHADER_RESOURCE_VIEW_DESC srvDesc = {};
			srvDesc.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING;
			srvDesc.Format = tex2D->m_format;
			srvDesc.ViewDimension = D3D12_SRV_DIMENSION_TEXTURE2D;
			srvDesc.Texture2D.MipLevels = 1;
			CD3DX12_CPU_DESCRIPTOR_HANDLE handle = CD3DX12_CPU_DESCRIPTOR_HANDLE(m_deviceResources->m_graphicsPipelineHeap->GetCPUDescriptorHandleForHeapStart(), incrementSize * tex2D->m_heapRefIndex);
			d3dDevice->CreateShaderResourceView(tex2D->m_texture.Get(), &srvDesc, handle);

		}
	}
	else if (texCube != nullptr)
	{
		{
			D3D12_RESOURCE_DESC textureDesc = {};
			textureDesc.MipLevels = 1;
			textureDesc.Format = texCube->m_format;
			textureDesc.Width = texCube->m_width;
			textureDesc.Height = texCube->m_height;
			textureDesc.Flags = D3D12_RESOURCE_FLAG_NONE;
			textureDesc.DepthOrArraySize = 6;
			textureDesc.SampleDesc.Count = 1;
			textureDesc.SampleDesc.Quality = 0;
			textureDesc.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D;

			DX::ThrowIfFailed(d3dDevice->CreateCommittedResource(
				&CD3DX12_HEAP_PROPERTIES(D3D12_HEAP_TYPE_DEFAULT),
				D3D12_HEAP_FLAG_NONE,
				&textureDesc,
				D3D12_RESOURCE_STATE_COPY_DEST,
				nullptr,
				IID_PPV_ARGS(&texCube->m_texture)));

			DX::ThrowIfFailed(d3dDevice->CreateCommittedResource(
				&CD3DX12_HEAP_PROPERTIES(D3D12_HEAP_TYPE_UPLOAD),
				D3D12_HEAP_FLAG_NONE,
				&CD3DX12_RESOURCE_DESC::Buffer(texCube->m_textureData->Length),
				D3D12_RESOURCE_STATE_GENERIC_READ,
				nullptr,
				IID_PPV_ARGS(&texCube->m_textureUpload)));
			NAME_D3D12_OBJECT(texCube->m_texture);

			D3D12_SUBRESOURCE_DATA textureDatas[6] = {};
			for (int i = 0; i < 6; i++)
			{
				textureDatas[i].pData = texCube->m_textureData->begin() + (texCube->m_textureData->Length / 6) * i;
				textureDatas[i].RowPitch = texCube->m_textureData->Length / texCube->m_height / 6;
				textureDatas[i].SlicePitch = texCube->m_textureData->Length / 6;
			}

			UpdateSubresources(m_commandList.Get(), texCube->m_texture.Get(), texCube->m_textureUpload.Get(), 0, 0, 6, textureDatas);

			CD3DX12_RESOURCE_BARRIER textureResourceBarrier =
				CD3DX12_RESOURCE_BARRIER::Transition(texCube->m_texture.Get(), D3D12_RESOURCE_STATE_COPY_DEST, D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE);
			m_commandList->ResourceBarrier(1, &textureResourceBarrier);
		}
		{
			UINT incrementSize = d3dDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);
			texCube->m_heapRefIndex = m_deviceResources->m_graphicsPipelineHeapAllocCount;
			m_deviceResources->m_graphicsPipelineHeapAllocCount++;

			D3D12_SHADER_RESOURCE_VIEW_DESC srvDesc = {};
			srvDesc.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING;
			srvDesc.Format = texCube->m_format;
			srvDesc.ViewDimension = D3D12_SRV_DIMENSION_TEXTURECUBE;
			srvDesc.TextureCube.MipLevels = 1;
			CD3DX12_CPU_DESCRIPTOR_HANDLE handle = CD3DX12_CPU_DESCRIPTOR_HANDLE(m_deviceResources->m_graphicsPipelineHeap->GetCPUDescriptorHandleForHeapStart(), incrementSize * texCube->m_heapRefIndex);
			d3dDevice->CreateShaderResourceView(texCube->m_texture.Get(), &srvDesc, handle);
		}
	}
}

void GraphicsContext::UploadBuffer(StaticBuffer^ buffer)
{
	auto d3dDevice = m_deviceResources->GetD3DDevice();

	DX::ThrowIfFailed(d3dDevice->CreateCommittedResource(
		&CD3DX12_HEAP_PROPERTIES(D3D12_HEAP_TYPE_DEFAULT),
		D3D12_HEAP_FLAG_NONE,
		&CD3DX12_RESOURCE_DESC::Buffer(buffer->m_bufferData->Length),
		D3D12_RESOURCE_STATE_COPY_DEST,
		nullptr,
		IID_PPV_ARGS(&buffer->m_buffer)));
	NAME_D3D12_OBJECT(buffer->m_buffer);

	DX::ThrowIfFailed(d3dDevice->CreateCommittedResource(
		&CD3DX12_HEAP_PROPERTIES(D3D12_HEAP_TYPE_UPLOAD),
		D3D12_HEAP_FLAG_NONE,
		&CD3DX12_RESOURCE_DESC::Buffer(buffer->m_bufferData->Length),
		D3D12_RESOURCE_STATE_GENERIC_READ,
		nullptr,
		IID_PPV_ARGS(&buffer->m_bufferUpload)));

	D3D12_SUBRESOURCE_DATA textureData = {};
	textureData.pData = buffer->m_bufferData->begin();
	textureData.RowPitch = buffer->m_bufferData->Length;
	textureData.SlicePitch = buffer->m_bufferData->Length;

	UpdateSubresources(m_commandList.Get(), buffer->m_buffer.Get(), buffer->m_bufferUpload.Get(), 0, 0, 1, &textureData);

	m_commandList->ResourceBarrier(1, &CD3DX12_RESOURCE_BARRIER::Transition(buffer->m_buffer.Get(), D3D12_RESOURCE_STATE_COPY_DEST, D3D12_RESOURCE_STATE_GENERIC_READ));
	UINT incrementSize = d3dDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);
	buffer->m_heapRefIndex = m_deviceResources->m_graphicsPipelineHeapAllocCount;
	m_deviceResources->m_graphicsPipelineHeapAllocCount++;

	D3D12_SHADER_RESOURCE_VIEW_DESC srvDesc = {};
	srvDesc.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING;
	srvDesc.Format = DXGI_FORMAT_UNKNOWN;
	srvDesc.ViewDimension = D3D12_SRV_DIMENSION_BUFFER;
	srvDesc.Buffer.StructureByteStride = buffer->m_stride;
	srvDesc.Buffer.NumElements = buffer->m_bufferData->Length / buffer->m_stride;
	CD3DX12_CPU_DESCRIPTOR_HANDLE handle = CD3DX12_CPU_DESCRIPTOR_HANDLE(m_deviceResources->m_graphicsPipelineHeap->GetCPUDescriptorHandleForHeapStart(), incrementSize * buffer->m_heapRefIndex);
	d3dDevice->CreateShaderResourceView(buffer->m_buffer.Get(), &srvDesc, handle);
}

void GraphicsContext::UpdateRenderTexture(IRenderTexture^ texture)
{
	RenderTexture2D^ tex2D = dynamic_cast<RenderTexture2D^>(texture);
	RenderTextureCube^ texCube = dynamic_cast<RenderTextureCube^>(texture);
	auto d3dDevice = m_deviceResources->GetD3DDevice();
	if (tex2D != nullptr)
	{
		if (tex2D->m_texture == nullptr)
		{
			tex2D->m_srvRefIndex = m_deviceResources->m_graphicsPipelineHeapAllocCount;
			m_deviceResources->m_graphicsPipelineHeapAllocCount++;
			if (tex2D->m_dsvFormat != DXGI_FORMAT_UNKNOWN)
			{
				tex2D->m_dsvHeapRefIndex = m_deviceResources->m_dsvHeapAllocCount;
				m_deviceResources->m_dsvHeapAllocCount++;
			}
			if (tex2D->m_rtvFormat != DXGI_FORMAT_UNKNOWN)
			{
				tex2D->m_rtvHeapRefIndex = m_deviceResources->m_rtvHeapAllocCount;
				m_deviceResources->m_rtvHeapAllocCount++;
			}
			if (tex2D->m_uavFormat != DXGI_FORMAT_UNKNOWN)
			{
				tex2D->m_uavRefIndex = m_deviceResources->m_graphicsPipelineHeapAllocCount;
				m_deviceResources->m_graphicsPipelineHeapAllocCount++;
			}
		}

		{
			D3D12_RESOURCE_DESC textureDesc = {};
			textureDesc.MipLevels = 1;
			if (tex2D->m_dsvFormat != DXGI_FORMAT_UNKNOWN)
				textureDesc.Format = tex2D->m_dsvFormat;
			else
				textureDesc.Format = tex2D->m_format;
			textureDesc.Width = tex2D->m_width;
			textureDesc.Height = tex2D->m_height;
			textureDesc.Flags = tex2D->m_resourceFlags;
			textureDesc.DepthOrArraySize = 1;
			textureDesc.SampleDesc.Count = 1;
			textureDesc.SampleDesc.Quality = 0;
			textureDesc.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D;

			if (tex2D->m_dsvFormat == DXGI_FORMAT_D32_FLOAT)
			{
				CD3DX12_CLEAR_VALUE clearValue(tex2D->m_dsvFormat, 1.0f, 0);
				DX::ThrowIfFailed(d3dDevice->CreateCommittedResource(
					&CD3DX12_HEAP_PROPERTIES(D3D12_HEAP_TYPE_DEFAULT),
					D3D12_HEAP_FLAG_NONE,
					&textureDesc,
					D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE,
					&clearValue,
					IID_PPV_ARGS(&tex2D->m_texture)));
			}
			else
			{
				float color[] = { 0.0f,0.0f,0.0f,0.0f };
				CD3DX12_CLEAR_VALUE clearValue(tex2D->m_format, color);
				DX::ThrowIfFailed(d3dDevice->CreateCommittedResource(
					&CD3DX12_HEAP_PROPERTIES(D3D12_HEAP_TYPE_DEFAULT),
					D3D12_HEAP_FLAG_NONE,
					&textureDesc,
					D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE,
					&clearValue,
					IID_PPV_ARGS(&tex2D->m_texture)));
			}
			tex2D->prevResourceState = D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE;
			NAME_D3D12_OBJECT(tex2D->m_texture);
		}
		D3D12_SHADER_RESOURCE_VIEW_DESC srvDesc = {};
		srvDesc.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING;
		srvDesc.Format = tex2D->m_format;
		srvDesc.ViewDimension = D3D12_SRV_DIMENSION_TEXTURE2D;
		srvDesc.Texture2D.MipLevels = 1;

		UINT incrementSize = d3dDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);
		CD3DX12_CPU_DESCRIPTOR_HANDLE handle = CD3DX12_CPU_DESCRIPTOR_HANDLE(m_deviceResources->m_graphicsPipelineHeap->GetCPUDescriptorHandleForHeapStart(), incrementSize * tex2D->m_srvRefIndex);
		d3dDevice->CreateShaderResourceView(tex2D->m_texture.Get(), &srvDesc, handle);
		if (tex2D->m_dsvFormat != DXGI_FORMAT_UNKNOWN)
		{
			incrementSize = d3dDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_DSV);
			handle = CD3DX12_CPU_DESCRIPTOR_HANDLE(m_deviceResources->m_dsvHeap->GetCPUDescriptorHandleForHeapStart());
			handle.Offset(incrementSize * tex2D->m_dsvHeapRefIndex);
			d3dDevice->CreateDepthStencilView(tex2D->m_texture.Get(), nullptr, handle);
		}
		if (tex2D->m_rtvFormat != DXGI_FORMAT_UNKNOWN)
		{
			incrementSize = d3dDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_RTV);
			handle = CD3DX12_CPU_DESCRIPTOR_HANDLE(m_deviceResources->m_rtvHeap->GetCPUDescriptorHandleForHeapStart());
			handle.Offset(incrementSize * tex2D->m_rtvHeapRefIndex);
			d3dDevice->CreateRenderTargetView(tex2D->m_texture.Get(), nullptr, handle);
		}
		if (tex2D->m_uavFormat != DXGI_FORMAT_UNKNOWN)
		{
			D3D12_UNORDERED_ACCESS_VIEW_DESC uavDesc = {};
			uavDesc.ViewDimension = D3D12_UAV_DIMENSION_TEXTURE2D;
			uavDesc.Format = tex2D->m_uavFormat;


			incrementSize = d3dDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);
			CD3DX12_CPU_DESCRIPTOR_HANDLE handle2 = CD3DX12_CPU_DESCRIPTOR_HANDLE(m_deviceResources->m_graphicsPipelineHeap->GetCPUDescriptorHandleForHeapStart(), incrementSize * tex2D->m_uavRefIndex);
			d3dDevice->CreateUnorderedAccessView(tex2D->m_texture.Get(), nullptr, &uavDesc, handle2);
		}
	}
	else if (texCube != nullptr)
	{
		if (texCube->m_texture == nullptr)
		{
			texCube->m_srvRefIndex = m_deviceResources->m_graphicsPipelineHeapAllocCount;
			m_deviceResources->m_graphicsPipelineHeapAllocCount++;
			//if (texture->m_dsvFormat != DXGI_FORMAT_UNKNOWN)
			//{
			//	texture->m_dsvHeapRefIndex = m_deviceResources->m_dsvHeapAllocCount;
			//	m_deviceResources->m_dsvHeapAllocCount++;
			//}
			//if (texture->m_rtvFormat != DXGI_FORMAT_UNKNOWN)
			//{
			//	texture->m_rtvHeapRefIndex = m_deviceResources->m_rtvHeapAllocCount;
			//	m_deviceResources->m_rtvHeapAllocCount++;
			//}
			if (texCube->m_uavFormat != DXGI_FORMAT_UNKNOWN)
			{
				texCube->m_uavRefIndex = m_deviceResources->m_graphicsPipelineHeapAllocCount;
				m_deviceResources->m_graphicsPipelineHeapAllocCount++;
			}
		}

		{
			D3D12_RESOURCE_DESC textureDesc = {};
			textureDesc.MipLevels = 1;
			if (texCube->m_dsvFormat != DXGI_FORMAT_UNKNOWN)
				textureDesc.Format = texCube->m_dsvFormat;
			else
				textureDesc.Format = texCube->m_format;
			textureDesc.Width = texCube->m_width;
			textureDesc.Height = texCube->m_height;
			textureDesc.Flags = texCube->m_resourceFlags;
			textureDesc.DepthOrArraySize = 6;
			textureDesc.SampleDesc.Count = 1;
			textureDesc.SampleDesc.Quality = 0;
			textureDesc.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D;

			if (texCube->m_dsvFormat == DXGI_FORMAT_D32_FLOAT)
			{
				CD3DX12_CLEAR_VALUE clearValue(texCube->m_dsvFormat, 1.0f, 0);
				DX::ThrowIfFailed(d3dDevice->CreateCommittedResource(
					&CD3DX12_HEAP_PROPERTIES(D3D12_HEAP_TYPE_DEFAULT),
					D3D12_HEAP_FLAG_NONE,
					&textureDesc,
					D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE,
					&clearValue,
					IID_PPV_ARGS(&texCube->m_texture)));
			}
			else
			{
				float color[] = { 0.0f,0.0f,0.0f,0.0f };
				CD3DX12_CLEAR_VALUE clearValue(texCube->m_format, color);
				DX::ThrowIfFailed(d3dDevice->CreateCommittedResource(
					&CD3DX12_HEAP_PROPERTIES(D3D12_HEAP_TYPE_DEFAULT),
					D3D12_HEAP_FLAG_NONE,
					&textureDesc,
					D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE,
					&clearValue,
					IID_PPV_ARGS(&texCube->m_texture)));
			}
			texCube->prevResourceState = D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE;
			NAME_D3D12_OBJECT(texCube->m_texture);
		}
		D3D12_SHADER_RESOURCE_VIEW_DESC srvDesc = {};
		srvDesc.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING;
		srvDesc.Format = texCube->m_format;
		srvDesc.ViewDimension = D3D12_SRV_DIMENSION_TEXTURECUBE;
		srvDesc.Texture2D.MipLevels = 1;
		UINT incrementSize = d3dDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);
		CD3DX12_CPU_DESCRIPTOR_HANDLE handle = CD3DX12_CPU_DESCRIPTOR_HANDLE(m_deviceResources->m_graphicsPipelineHeap->GetCPUDescriptorHandleForHeapStart());
		handle.Offset(incrementSize * texCube->m_srvRefIndex);
		d3dDevice->CreateShaderResourceView(texCube->m_texture.Get(), &srvDesc, handle);
		//if (texture->m_dsvFormat != DXGI_FORMAT_UNKNOWN)
		//{
		//	incrementSize = d3dDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_DSV);
		//	handle = CD3DX12_CPU_DESCRIPTOR_HANDLE(m_deviceResources->m_dsvHeap->GetCPUDescriptorHandleForHeapStart());
		//	handle.Offset(incrementSize * texture->m_dsvHeapRefIndex);
		//	d3dDevice->CreateDepthStencilView(texture->m_texture.Get(), nullptr, handle);
		//}
		//if (texture->m_rtvFormat != DXGI_FORMAT_UNKNOWN)
		//{
		//	incrementSize = d3dDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_RTV);
		//	handle = CD3DX12_CPU_DESCRIPTOR_HANDLE(m_deviceResources->m_rtvHeap->GetCPUDescriptorHandleForHeapStart());
		//	handle.Offset(incrementSize * texture->m_rtvHeapRefIndex);
		//	d3dDevice->CreateRenderTargetView(texture->m_texture.Get(), nullptr, handle);
		//}
		if (texCube->m_uavFormat != DXGI_FORMAT_UNKNOWN)
		{
			D3D12_UNORDERED_ACCESS_VIEW_DESC uavDesc = {};
			uavDesc.ViewDimension = D3D12_UAV_DIMENSION_TEXTURE2DARRAY;
			uavDesc.Format = texCube->m_uavFormat;
			uavDesc.Texture2DArray.ArraySize = 6;

			incrementSize = d3dDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);
			CD3DX12_CPU_DESCRIPTOR_HANDLE handle2 = CD3DX12_CPU_DESCRIPTOR_HANDLE(m_deviceResources->m_graphicsPipelineHeap->GetCPUDescriptorHandleForHeapStart(), incrementSize * texCube->m_uavRefIndex);
			d3dDevice->CreateUnorderedAccessView(texCube->m_texture.Get(), nullptr, &uavDesc, handle2);
		}
	}
}

void GraphicsContext::UpdateReadBackTexture(ReadBackTexture2D^ texture)
{
	auto d3dDevice = m_deviceResources->GetD3DDevice();
	for (int i = 0; i < c_frameCount; i++)
	{
		DX::ThrowIfFailed(d3dDevice->CreateCommittedResource(
			&CD3DX12_HEAP_PROPERTIES(D3D12_HEAP_TYPE_READBACK),
			D3D12_HEAP_FLAG_NONE,
			&CD3DX12_RESOURCE_DESC::Buffer(((texture->m_width + 63) & ~63) * texture->m_height * texture->m_bytesPerPixel),
			D3D12_RESOURCE_STATE_COPY_DEST,
			nullptr,
			IID_PPV_ARGS(&texture->m_textureReadBack[i])));
	}
}

void GraphicsContext::Copy(TextureCube^ source, RenderTextureCube^ dest)
{
	m_commandList->CopyResource(dest->m_texture.Get(), source->m_texture.Get());
}

void GraphicsContext::CopyBackBuffer(ReadBackTexture2D^ target, int index)
{
	auto d3dDevice = m_deviceResources->GetD3DDevice();
	auto backBuffer = m_deviceResources->GetRenderTarget();
	m_commandList->ResourceBarrier(1, &CD3DX12_RESOURCE_BARRIER::Transition(backBuffer, D3D12_RESOURCE_STATE_RENDER_TARGET, D3D12_RESOURCE_STATE_COPY_SOURCE));

	D3D12_PLACED_SUBRESOURCE_FOOTPRINT footPrint = {};
	footPrint.Footprint.Width = target->m_width;
	footPrint.Footprint.Height = target->m_height;
	footPrint.Footprint.Depth = 1;
	footPrint.Footprint.RowPitch = (target->m_width * 4 + 255) & ~255;
	footPrint.Footprint.Format = m_deviceResources->GetBackBufferFormat();
	CD3DX12_TEXTURE_COPY_LOCATION Dst(target->m_textureReadBack[index].Get(), footPrint);
	CD3DX12_TEXTURE_COPY_LOCATION Src(backBuffer, 0);
	m_commandList->CopyTextureRegion(&Dst, 0, 0, 0, &Src, nullptr);

	m_commandList->ResourceBarrier(1, &CD3DX12_RESOURCE_BARRIER::Transition(backBuffer, D3D12_RESOURCE_STATE_COPY_SOURCE, D3D12_RESOURCE_STATE_RENDER_TARGET));
}

void GraphicsContext::BuildBottomAccelerationStructures(RayTracingScene^ rayTracingAccelerationStructure, MMDMesh^ mesh, int vertexBegin, int vertexCount)
{
	int lastUpdateIndex = rayTracingAccelerationStructure->asLastUpdateIndex;

	auto m_dxrDevice = m_deviceResources->GetD3DDevice5();
	CD3DX12_HEAP_PROPERTIES defaultHeapProperties(D3D12_HEAP_TYPE_DEFAULT);

	D3D12_RAYTRACING_GEOMETRY_DESC geometryDesc = {};
	geometryDesc.Type = D3D12_RAYTRACING_GEOMETRY_TYPE_TRIANGLES;
	geometryDesc.Triangles.VertexFormat = DXGI_FORMAT_R32G32B32_FLOAT;
	geometryDesc.Flags = D3D12_RAYTRACING_GEOMETRY_FLAG_OPAQUE;
	geometryDesc.Triangles.VertexBuffer.StrideInBytes = MMDMesh::c_skinnedVerticeStride;
	geometryDesc.Triangles.VertexCount = vertexCount;
	geometryDesc.Triangles.VertexBuffer.StartAddress = mesh->m_skinnedVertice->GetGPUVirtualAddress() + vertexBegin * MMDMesh::c_skinnedVerticeStride;


	D3D12_RAYTRACING_ACCELERATION_STRUCTURE_PREBUILD_INFO bottomLevelPrebuildInfo = {};
	D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_INPUTS bottomLevelInputs = {};
	bottomLevelInputs.DescsLayout = D3D12_ELEMENTS_LAYOUT_ARRAY;
	bottomLevelInputs.Flags = D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAG_PREFER_FAST_BUILD;
	bottomLevelInputs.NumDescs = 1;
	bottomLevelInputs.Type = D3D12_RAYTRACING_ACCELERATION_STRUCTURE_TYPE_BOTTOM_LEVEL;
	bottomLevelInputs.pGeometryDescs = &geometryDesc;
	m_dxrDevice->GetRaytracingAccelerationStructurePrebuildInfo(&bottomLevelInputs, &bottomLevelPrebuildInfo);
	DX::ThrowIfFalse(bottomLevelPrebuildInfo.ResultDataMaxSizeInBytes > 0);
	Microsoft::WRL::ComPtr<ID3D12Resource> asStruct;
	DX::ThrowIfFailed(m_dxrDevice->CreateCommittedResource(
		&defaultHeapProperties,
		D3D12_HEAP_FLAG_NONE,
		&CD3DX12_RESOURCE_DESC::Buffer(bottomLevelPrebuildInfo.ResultDataMaxSizeInBytes, D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS),
		D3D12_RESOURCE_STATE_RAYTRACING_ACCELERATION_STRUCTURE,
		nullptr,
		IID_PPV_ARGS(&asStruct)));
	NAME_D3D12_OBJECT(asStruct);

	D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_DESC bottomLevelBuildDesc = {};
	bottomLevelBuildDesc.Inputs = bottomLevelInputs;
	bottomLevelBuildDesc.ScratchAccelerationStructureData = rayTracingAccelerationStructure->m_scratchResource[lastUpdateIndex]->GetGPUVirtualAddress();
	bottomLevelBuildDesc.DestAccelerationStructureData = asStruct->GetGPUVirtualAddress();
	rayTracingAccelerationStructure->m_bottomLevelASs[lastUpdateIndex].push_back(asStruct);

	if (mesh->prevStateSkinnedVertice != D3D12_RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE)
		m_commandList->ResourceBarrier(1, &CD3DX12_RESOURCE_BARRIER::Transition(mesh->m_skinnedVertice.Get(), mesh->prevStateSkinnedVertice, D3D12_RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE));
	mesh->prevStateSkinnedVertice = D3D12_RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE;
	m_commandList->BuildRaytracingAccelerationStructure(&bottomLevelBuildDesc, 0, nullptr);
	m_commandList->ResourceBarrier(1, &CD3DX12_RESOURCE_BARRIER::UAV(asStruct.Get()));
}

void GraphicsContext::BuildBASAndParam(RayTracingScene^ rayTracingAccelerationStructure, MMDMesh^ mesh, UINT instanceMask, int vertexBegin, int vertexCount, int rayTypeCount, Texture2D^ diff, ConstantBufferStatic^ mat)
{
	auto d3dDevice = m_deviceResources->GetD3DDevice();
	UINT incrementSize = d3dDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV);
	void* pBase = (byte*)rayTracingAccelerationStructure->pArgumentCache + (RayTracingScene::c_argumentCacheStride * rayTracingAccelerationStructure->m_instanceDescs.size());

	CD3DX12_GPU_DESCRIPTOR_HANDLE gpuHandle(m_deviceResources->m_graphicsPipelineHeap->GetGPUDescriptorHandleForHeapStart(), diff->m_heapRefIndex, incrementSize);

	*(D3D12_GPU_VIRTUAL_ADDRESS*)(pBase) = mat->GetCurrentVirtualAddress();
	*(D3D12_GPU_VIRTUAL_ADDRESS*)((byte*)pBase + sizeof(D3D12_GPU_VIRTUAL_ADDRESS)) = mesh->m_skinnedVertice.Get()->GetGPUVirtualAddress() + MMDMesh::c_skinnedVerticeStride * vertexBegin;
	*(D3D12_GPU_VIRTUAL_ADDRESS*)((byte*)pBase + sizeof(D3D12_GPU_VIRTUAL_ADDRESS) * 2) = gpuHandle.ptr;

	BuildBottomAccelerationStructures(rayTracingAccelerationStructure, mesh, vertexBegin, vertexCount);

	int index1 = rayTracingAccelerationStructure->m_instanceDescs.size();
	D3D12_RAYTRACING_INSTANCE_DESC instanceDesc = {};
	instanceDesc.Transform[0][0] = instanceDesc.Transform[1][1] = instanceDesc.Transform[2][2] = 1;
	instanceDesc.InstanceMask = instanceMask;
	instanceDesc.InstanceID = index1;
	instanceDesc.InstanceContributionToHitGroupIndex = index1 * rayTypeCount;
	instanceDesc.AccelerationStructure = rayTracingAccelerationStructure->m_bottomLevelASs[rayTracingAccelerationStructure->asLastUpdateIndex][index1]->GetGPUVirtualAddress();
	rayTracingAccelerationStructure->m_instanceDescs.push_back(instanceDesc);
}

void GraphicsContext::BuildTopAccelerationStructures(RayTracingScene^ rayTracingAccelerationStructure)
{
	int lastUpdateIndex = rayTracingAccelerationStructure->asLastUpdateIndex;
	auto m_dxrDevice = m_deviceResources->GetD3DDevice5();
	CD3DX12_HEAP_PROPERTIES defaultHeapProperties(D3D12_HEAP_TYPE_DEFAULT);
	int meshCount = rayTracingAccelerationStructure->m_bottomLevelASs[lastUpdateIndex].size();

	D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_DESC topLevelBuildDesc = {};
	D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_INPUTS& topLevelInputs = topLevelBuildDesc.Inputs;
	topLevelInputs.Type = D3D12_RAYTRACING_ACCELERATION_STRUCTURE_TYPE_TOP_LEVEL;
	topLevelInputs.DescsLayout = D3D12_ELEMENTS_LAYOUT_ARRAY;
	topLevelInputs.Flags = D3D12_RAYTRACING_ACCELERATION_STRUCTURE_BUILD_FLAG_PREFER_FAST_BUILD;
	topLevelInputs.NumDescs = meshCount;

	D3D12_RAYTRACING_ACCELERATION_STRUCTURE_PREBUILD_INFO topLevelPrebuildInfo = {};
	m_dxrDevice->GetRaytracingAccelerationStructurePrebuildInfo(&topLevelInputs, &topLevelPrebuildInfo);
	DX::ThrowIfFalse(topLevelPrebuildInfo.ResultDataMaxSizeInBytes > 0);

	DX::ThrowIfFailed(m_dxrDevice->CreateCommittedResource(
		&defaultHeapProperties,
		D3D12_HEAP_FLAG_NONE,
		&CD3DX12_RESOURCE_DESC::Buffer(topLevelPrebuildInfo.ResultDataMaxSizeInBytes, D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS),
		D3D12_RESOURCE_STATE_RAYTRACING_ACCELERATION_STRUCTURE,
		nullptr,
		IID_PPV_ARGS(&rayTracingAccelerationStructure->m_topLevelAccelerationStructure[lastUpdateIndex])));
	NAME_D3D12_OBJECT(rayTracingAccelerationStructure->m_topLevelAccelerationStructure[lastUpdateIndex]);
	rayTracingAccelerationStructure->m_topLevelAccelerationStructureSize[lastUpdateIndex] = topLevelPrebuildInfo.ResultDataMaxSizeInBytes;



	void* pMappedData;
	rayTracingAccelerationStructure->instanceDescs[lastUpdateIndex]->Map(0, nullptr, &pMappedData);
	memcpy(pMappedData, rayTracingAccelerationStructure->m_instanceDescs.data(), rayTracingAccelerationStructure->m_instanceDescs.size() * sizeof(D3D12_RAYTRACING_INSTANCE_DESC));
	rayTracingAccelerationStructure->instanceDescs[lastUpdateIndex]->Unmap(0, nullptr);

	topLevelInputs.InstanceDescs = rayTracingAccelerationStructure->instanceDescs[lastUpdateIndex]->GetGPUVirtualAddress();
	topLevelBuildDesc.DestAccelerationStructureData = rayTracingAccelerationStructure->m_topLevelAccelerationStructure[lastUpdateIndex]->GetGPUVirtualAddress();
	topLevelBuildDesc.ScratchAccelerationStructureData = rayTracingAccelerationStructure->m_scratchResource[lastUpdateIndex]->GetGPUVirtualAddress();

	m_commandList->BuildRaytracingAccelerationStructure(&topLevelBuildDesc, 0, nullptr);
}

void GraphicsContext::SetMesh(MMDMesh^ mesh)
{
	m_commandList->IASetPrimitiveTopology(mesh->m_primitiveTopology);
	m_commandList->IASetVertexBuffers(0, 1, &mesh->m_vertexBufferView);
	m_commandList->IASetVertexBuffers(1, 1, &mesh->m_vertexBufferPosView0[mesh->lastUpdateIndex0]);
	m_commandList->IASetVertexBuffers(2, 1, &mesh->m_vertexBufferPosView1[mesh->lastUpdateIndex1]);
	m_commandList->IASetIndexBuffer(&mesh->m_indexBufferView);
}

void GraphicsContext::SetMeshSkinned(MMDMesh^ mesh)
{
	if (mesh->prevStateSkinnedVertice != D3D12_RESOURCE_STATE_GENERIC_READ)
		m_commandList->ResourceBarrier(1, &CD3DX12_RESOURCE_BARRIER::Transition(mesh->m_skinnedVertice.Get(), mesh->prevStateSkinnedVertice, D3D12_RESOURCE_STATE_GENERIC_READ));
	mesh->prevStateSkinnedVertice = D3D12_RESOURCE_STATE_GENERIC_READ;
	m_commandList->IASetPrimitiveTopology(mesh->m_primitiveTopology);
	m_commandList->IASetVertexBuffers(0, 1, &mesh->m_skinnedVerticeVertexBufferView);
	m_commandList->IASetIndexBuffer(&mesh->m_indexBufferView);
}

void GraphicsContext::SetRenderTargetScreenAndClear(Windows::Foundation::Numerics::float4 color)
{
	// 设置视区和剪刀矩形。
	D3D12_VIEWPORT viewport = m_deviceResources->GetScreenViewport();
	D3D12_RECT scissorRect = { 0, 0, static_cast<LONG>(viewport.Width), static_cast<LONG>(viewport.Height) };
	m_commandList->RSSetViewports(1, &viewport);
	m_commandList->RSSetScissorRects(1, &scissorRect);


	float _color[4] = { color.x,color.y,color.z,color.w };
	D3D12_CPU_DESCRIPTOR_HANDLE renderTargetView = m_deviceResources->GetRenderTargetView();
	D3D12_CPU_DESCRIPTOR_HANDLE depthStencilView = m_deviceResources->GetDepthStencilView();
	m_commandList->ClearRenderTargetView(renderTargetView, _color, 0, nullptr);
	m_commandList->ClearDepthStencilView(depthStencilView, D3D12_CLEAR_FLAG_DEPTH, 1.0f, 0, 0, nullptr);
	m_commandList->OMSetRenderTargets(1, &renderTargetView, false, &depthStencilView);
}

void GraphicsContext::SetAndClearDSV(RenderTexture2D^ texture)
{
	// 设置视区和剪刀矩形。
	D3D12_VIEWPORT viewport = CD3DX12_VIEWPORT(
		0.0f,
		0.0f,
		texture->m_width,
		texture->m_height
	);
	D3D12_RECT scissorRect = { 0, 0, static_cast<LONG>(viewport.Width), static_cast<LONG>(viewport.Height) };
	m_commandList->RSSetViewports(1, &viewport);
	m_commandList->RSSetScissorRects(1, &scissorRect);
	if (texture->prevResourceState == D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE)
		m_commandList->ResourceBarrier(1, &CD3DX12_RESOURCE_BARRIER::Transition(texture->m_texture.Get(), texture->prevResourceState, D3D12_RESOURCE_STATE_DEPTH_WRITE));
	texture->prevResourceState = D3D12_RESOURCE_STATE_DEPTH_WRITE;

	auto d3dDevice = m_deviceResources->GetD3DDevice();
	UINT incrementSize = d3dDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_DSV);

	D3D12_CPU_DESCRIPTOR_HANDLE depthStencilView = CD3DX12_CPU_DESCRIPTOR_HANDLE(m_deviceResources->m_dsvHeap->GetCPUDescriptorHandleForHeapStart(), texture->m_dsvHeapRefIndex, incrementSize);
	m_commandList->ClearDepthStencilView(depthStencilView, D3D12_CLEAR_FLAG_DEPTH | D3D12_CLEAR_FLAG_STENCIL, 1.0f, 0, 0, nullptr);
	m_commandList->OMSetRenderTargets(0, nullptr, false, &depthStencilView);
}

void GraphicsContext::SetAndClearRTVDSV(RenderTexture2D^ RTV, RenderTexture2D^ DSV, Windows::Foundation::Numerics::float4 color)
{
	if ((RTV->m_width > DSV->m_width) || (RTV->m_height > DSV->m_height))
	{
		throw ref new Platform::NotImplementedException();
	}
	// 设置视区和剪刀矩形。
	D3D12_VIEWPORT viewport = CD3DX12_VIEWPORT(
		0.0f,
		0.0f,
		RTV->m_width,
		RTV->m_height
	);
	D3D12_RECT scissorRect = { 0, 0, static_cast<LONG>(viewport.Width), static_cast<LONG>(viewport.Height) };
	m_commandList->RSSetViewports(1, &viewport);
	m_commandList->RSSetScissorRects(1, &scissorRect);


	if (RTV->prevResourceState == D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE)
		m_commandList->ResourceBarrier(1, &CD3DX12_RESOURCE_BARRIER::Transition(RTV->m_texture.Get(), RTV->prevResourceState, D3D12_RESOURCE_STATE_RENDER_TARGET));
	RTV->prevResourceState = D3D12_RESOURCE_STATE_RENDER_TARGET;
	if (DSV->prevResourceState == D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE)
		m_commandList->ResourceBarrier(1, &CD3DX12_RESOURCE_BARRIER::Transition(DSV->m_texture.Get(), DSV->prevResourceState, D3D12_RESOURCE_STATE_DEPTH_WRITE));
	DSV->prevResourceState = D3D12_RESOURCE_STATE_DEPTH_WRITE;

	auto d3dDevice = m_deviceResources->GetD3DDevice();

	UINT incrementSize = d3dDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_RTV);
	D3D12_CPU_DESCRIPTOR_HANDLE renderTargetView = CD3DX12_CPU_DESCRIPTOR_HANDLE(m_deviceResources->m_rtvHeap->GetCPUDescriptorHandleForHeapStart(), RTV->m_rtvHeapRefIndex, incrementSize);
	incrementSize = d3dDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_DSV);
	D3D12_CPU_DESCRIPTOR_HANDLE depthStencilView = CD3DX12_CPU_DESCRIPTOR_HANDLE(m_deviceResources->m_dsvHeap->GetCPUDescriptorHandleForHeapStart(), DSV->m_dsvHeapRefIndex, incrementSize);

	float _color[4] = { color.x,color.y,color.z,color.w };
	m_commandList->ClearRenderTargetView(renderTargetView, _color, 0, nullptr);
	m_commandList->ClearDepthStencilView(depthStencilView, D3D12_CLEAR_FLAG_DEPTH | D3D12_CLEAR_FLAG_STENCIL, 1.0f, 0, 0, nullptr);
	m_commandList->OMSetRenderTargets(1, &renderTargetView, false, &depthStencilView);
}

void GraphicsContext::SetAndClearRTV(RenderTexture2D^ RTV, Windows::Foundation::Numerics::float4 color)
{
	// 设置视区和剪刀矩形。
	D3D12_VIEWPORT viewport = CD3DX12_VIEWPORT(
		0.0f,
		0.0f,
		RTV->m_width,
		RTV->m_height
	);
	D3D12_RECT scissorRect = { 0, 0, static_cast<LONG>(viewport.Width), static_cast<LONG>(viewport.Height) };
	m_commandList->RSSetViewports(1, &viewport);
	m_commandList->RSSetScissorRects(1, &scissorRect);


	if (RTV->prevResourceState == D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE)
		m_commandList->ResourceBarrier(1, &CD3DX12_RESOURCE_BARRIER::Transition(RTV->m_texture.Get(), RTV->prevResourceState, D3D12_RESOURCE_STATE_RENDER_TARGET));
	RTV->prevResourceState = D3D12_RESOURCE_STATE_RENDER_TARGET;

	auto d3dDevice = m_deviceResources->GetD3DDevice();

	UINT incrementSize = d3dDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_RTV);
	D3D12_CPU_DESCRIPTOR_HANDLE renderTargetView = CD3DX12_CPU_DESCRIPTOR_HANDLE(m_deviceResources->m_rtvHeap->GetCPUDescriptorHandleForHeapStart(), RTV->m_rtvHeapRefIndex, incrementSize);

	float _color[4] = { color.x,color.y,color.z,color.w };
	m_commandList->ClearRenderTargetView(renderTargetView, _color, 0, nullptr);
	m_commandList->OMSetRenderTargets(1, &renderTargetView, false, nullptr);
}

void GraphicsContext::SetRootSignature(GraphicsSignature^ rootSignature)
{
	m_commandList->SetGraphicsRootSignature(rootSignature->m_rootSignature.Get());
}

void GraphicsContext::SetRootSignatureCompute(GraphicsSignature^ rootSignature)
{
	m_commandList->SetComputeRootSignature(rootSignature->m_rootSignature.Get());
}

void GraphicsContext::SetRootSignatureRayTracing(RayTracingScene^ rayTracingScene)
{
	m_commandList->SetComputeRootSignature(rayTracingScene->m_rootSignatures[0].Get());
}

void GraphicsContext::ResourceBarrierScreen(D3D12ResourceStates before, D3D12ResourceStates after)
{
	CD3DX12_RESOURCE_BARRIER resourceBarrier =
		CD3DX12_RESOURCE_BARRIER::Transition(m_deviceResources->GetRenderTarget(), (D3D12_RESOURCE_STATES)before, (D3D12_RESOURCE_STATES)after);
	m_commandList->ResourceBarrier(1, &resourceBarrier);
}

void GraphicsContext::SetRenderTargetScreenAndClearDepth()
{
	D3D12_VIEWPORT viewport = m_deviceResources->GetScreenViewport();
	D3D12_RECT scissorRect = { 0, 0, static_cast<LONG>(viewport.Width), static_cast<LONG>(viewport.Height) };
	m_commandList->RSSetViewports(1, &viewport);
	m_commandList->RSSetScissorRects(1, &scissorRect);
	D3D12_CPU_DESCRIPTOR_HANDLE renderTargetView = m_deviceResources->GetRenderTargetView();
	D3D12_CPU_DESCRIPTOR_HANDLE depthStencilView = m_deviceResources->GetDepthStencilView();
	m_commandList->ClearDepthStencilView(depthStencilView, D3D12_CLEAR_FLAG_DEPTH, 1.0f, 0, 0, nullptr);
	m_commandList->OMSetRenderTargets(1, &renderTargetView, false, &depthStencilView);
}

void GraphicsContext::BeginAlloctor(DeviceResources^ deviceResources)
{
	DX::ThrowIfFailed(deviceResources->GetCommandAllocator()->Reset());
}

void GraphicsContext::SetDescriptorHeapDefault()
{
	ID3D12DescriptorHeap* heaps[] = { m_deviceResources->m_graphicsPipelineHeap.Get() };
	m_commandList->SetDescriptorHeaps(_countof(heaps), heaps);
}

void GraphicsContext::BeginCommand()
{
	DX::ThrowIfFailed(m_commandList->Reset(m_deviceResources->GetCommandAllocator(), nullptr));
}

void GraphicsContext::EndCommand()
{
	DX::ThrowIfFailed(m_commandList->Close());
}

void GraphicsContext::BeginEvent()
{
	PIXBeginEvent(m_commandList.Get(), 0, L"Draw");
}

void GraphicsContext::EndEvent()
{
	PIXEndEvent(m_commandList.Get());
}

void GraphicsContext::Execute()
{
	ID3D12CommandList* ppCommandLists[] = { m_commandList.Get() };
	m_deviceResources->GetCommandQueue()->ExecuteCommandLists(_countof(ppCommandLists), ppCommandLists);
}
