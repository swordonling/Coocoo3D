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
	bool operator()(const GUID & Left, const GUID & Right) const
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

GraphicsContext ^ GraphicsContext::Load(DeviceResources ^ deviceResources)
{
	GraphicsContext^ graphicsContext = ref new GraphicsContext();
	graphicsContext->m_deviceResources = deviceResources;
	return graphicsContext;
}

void GraphicsContext::Reload(DeviceResources ^ deviceResources)
{
	m_deviceResources = deviceResources;
	auto d3dDevice = deviceResources->GetD3DDevice();
	DX::ThrowIfFailed(d3dDevice->CreateCommandList(0, D3D12_COMMAND_LIST_TYPE_DIRECT, m_deviceResources->GetCommandAllocator(), nullptr, IID_PPV_ARGS(&m_commandList)));
	NAME_D3D12_OBJECT(m_commandList);
	DX::ThrowIfFailed(m_commandList->Close());
}

void GraphicsContext::SetMaterial(Material ^ material)
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

void GraphicsContext::SetPObject(PObject ^ pObject, CullMode cullMode, BlendState blendState)
{
	//auto context = m_deviceResources->GetD3DDeviceContext();

	//if (pObject->m_vertexShader != nullptr) {
	//	context->IASetInputLayout(pObject->m_inputLayout.Get());
	//	context->VSSetShader(pObject->m_vertexShader.Get(), nullptr, 0);
	//}
	//else {
	//	context->VSSetShader(nullptr, nullptr, 0);
	//}
	//if (pObject->m_geometryShader != nullptr) {
	//	context->GSSetShader(pObject->m_geometryShader->m_geometryShader.Get(), nullptr, 0);
	//}
	//else {
	//	context->GSSetShader(nullptr, nullptr, 0);
	//}
	//if (pObject->m_pixelShader != nullptr) {
	//	context->PSSetShader(pObject->m_pixelShader->m_pixelShader.Get(), nullptr, 0);
	//}
	//else {
	//	context->PSSetShader(nullptr, nullptr, 0);
	//}
	//if (blendState == BlendState::alpha)
	//	context->OMSetBlendState(pObject->m_blendStateAlpha.Get(), nullptr, 0xffffffff);
	//else if (blendState == BlendState::none)
	//	context->OMSetBlendState(pObject->m_blendStateOqaque.Get(), nullptr, 0xffffffff);

	//if (cullMode == CullMode::none)
	//	context->RSSetState(pObject->m_RasterizerStateCullNone.Get());
	//else if (cullMode == CullMode::front)
	//	context->RSSetState(pObject->m_RasterizerStateCullFront.Get());
	//else if (cullMode == CullMode::back)
	//	context->RSSetState(pObject->m_RasterizerStateCullBack.Get());

	m_commandList->SetPipelineState(pObject->m_pipelineState[0].Get());
}

void GraphicsContext::SetPObjectDepthOnly(PObject ^ pObject)
{
	//auto context = m_deviceResources->GetD3DDeviceContext();

	//if (pObject->m_vertexShader != nullptr) {
	//	context->IASetInputLayout(pObject->m_inputLayout.Get());
	//	context->VSSetShader(pObject->m_vertexShader.Get(), nullptr, 0);
	//}
	//else {
	//	context->VSSetShader(nullptr, nullptr, 0);
	//}
	//if (pObject->m_geometryShader != nullptr) {
	//	context->GSSetShader(pObject->m_geometryShader->m_geometryShader.Get(), nullptr, 0);
	//}
	//else {
	//	context->GSSetShader(nullptr, nullptr, 0);
	//}
	//context->PSSetShader(nullptr, nullptr, 0);
	//context->RSSetState(pObject->m_RasterizerStateCullNone.Get());
}

void GraphicsContext::UpdateResource(ConstantBuffer^ buffer, const Platform::Array<byte>^ data, UINT sizeInByte)
{
	//auto context = m_deviceResources->GetD3DDeviceContext();
	//context->UpdateSubresource(buffer->m_buffer.Get(), 0, nullptr, data->begin(), 0, 0);
	buffer->lastUpdateIndex = (buffer->lastUpdateIndex + 1) % c_frameCount;
	memcpy(buffer->m_mappedConstantBuffer + buffer->lastUpdateIndex*buffer->Size, data->begin(), sizeInByte);
}

void GraphicsContext::UpdateResource(ConstantBuffer ^ buffer, const Platform::Array<byte>^ data, UINT sizeInByte, int dataOffset)
{
	//auto context = m_deviceResources->GetD3DDeviceContext();
	//context->UpdateSubresource(buffer->m_buffer.Get(), 0, nullptr, data->begin() + dataOffset, 0, 0);
	buffer->lastUpdateIndex = (buffer->lastUpdateIndex + 1) % c_frameCount;
	memcpy(buffer->m_mappedConstantBuffer + buffer->lastUpdateIndex*buffer->Size, data->begin() + dataOffset, sizeInByte);
}

void GraphicsContext::UpdateVertices(MMDMesh^ mesh, const Platform::Array<byte>^ verticeData)
{
	//auto context = m_deviceResources->GetD3DDeviceContext();
	//context->UpdateSubresource(mesh->m_vertexBuffer.Get(), 0, nullptr, verticeData->begin(), 0, 0);
}

void GraphicsContext::UpdateVertices2(MMDMesh^ mesh, const Platform::Array<byte>^ verticeData)
{
	//auto context = m_deviceResources->GetD3DDeviceContext();
	//context->UpdateSubresource(mesh->m_vertexBuffer2.Get(), 0, nullptr, verticeData->begin(), 0, 0);
}

void GraphicsContext::UpdateVertices2(MMDMesh^ mesh, const Platform::Array<Windows::Foundation::Numerics::float3>^ verticeData)
{
	//auto context = m_deviceResources->GetD3DDeviceContext();
	//context->UpdateSubresource(mesh->m_vertexBuffer2.Get(), 0, nullptr, verticeData->begin(), 0, 0);
}

void GraphicsContext::SetSRV(PObjectType type, Texture2D ^ texture, int slot)
{
	//auto context = m_deviceResources->GetD3DDeviceContext();
	//if (texture != nullptr) {
	//	context->PSSetShaderResources(slot, 1, texture->m_shaderResourceView.GetAddressOf());
	//	context->PSSetSamplers(slot, 1, texture->m_samplerState.GetAddressOf());
	//}
	//else
	//{
	//	ID3D11ShaderResourceView* srv[1] = {};
	//	ID3D11SamplerState* ss[1] = {};
	//	context->PSSetShaderResources(slot, 1, srv);
	//	context->PSSetSamplers(slot, 1, ss);
	//}
}

void GraphicsContext::SetSRV_RT(PObjectType type, RenderTexture2D ^ texture, int slot)
{
	//auto context = m_deviceResources->GetD3DDeviceContext();
	//if (texture != nullptr) {
	//	context->PSSetShaderResources(slot, 1, texture->m_shaderResourceView.GetAddressOf());
	//	context->PSSetSamplers(slot, 1, texture->m_samplerState.GetAddressOf());
	//}
	//else
	//{
	//	ID3D11ShaderResourceView* srv[1] = {};
	//	ID3D11SamplerState* ss[1] = {};
	//	context->PSSetShaderResources(slot, 1, srv);
	//	context->PSSetSamplers(slot, 1, ss);
	//}
}

void GraphicsContext::SetConstantBuffer(PObjectType type, ConstantBuffer ^ buffer, int slot)
{
	//auto context = m_deviceResources->GetD3DDeviceContext();
	//context->VSSetConstantBuffers(slot, 1, buffer->m_buffer.GetAddressOf());
	//context->GSSetConstantBuffers(slot, 1, buffer->m_buffer.GetAddressOf());
	//context->PSSetConstantBuffers(slot, 1, buffer->m_buffer.GetAddressOf());
}

void GraphicsContext::SetMMDRender1CBResources(ConstantBuffer ^ boneData, ConstantBuffer ^ entityData, ConstantBuffer ^ presentData, ConstantBuffer ^ materialData)
{
	//auto context = m_deviceResources->GetD3DDeviceContext();
	//context->VSSetConstantBuffers(0, 1, entityData->m_buffer.GetAddressOf());
	//context->GSSetConstantBuffers(0, 1, entityData->m_buffer.GetAddressOf());
	//context->PSSetConstantBuffers(0, 1, entityData->m_buffer.GetAddressOf());

	//context->VSSetConstantBuffers(1, 1, boneData->m_buffer.GetAddressOf());

	//context->PSSetConstantBuffers(2, 1, materialData->m_buffer.GetAddressOf());

	//context->VSSetConstantBuffers(3, 1, presentData->m_buffer.GetAddressOf());
	//context->GSSetConstantBuffers(3, 1, presentData->m_buffer.GetAddressOf());
	//context->PSSetConstantBuffers(3, 1, presentData->m_buffer.GetAddressOf());
	m_commandList->SetGraphicsRootConstantBufferView(0, entityData->GetCurrentVirtualAddress());
	m_commandList->SetGraphicsRootConstantBufferView(1, boneData->GetCurrentVirtualAddress());
	m_commandList->SetGraphicsRootConstantBufferView(2, materialData->GetCurrentVirtualAddress());
	m_commandList->SetGraphicsRootConstantBufferView(3, presentData->GetCurrentVirtualAddress());
}

void GraphicsContext::Draw(int indexCount, int startIndexLocation)
{
	//auto context = m_deviceResources->GetD3DDeviceContext();
	//context->Draw(indexCount, startIndexLocation);
}

void GraphicsContext::DrawIndexed(int indexCount, int startIndexLocation, int baseVertexLocation)
{
	//auto context = m_deviceResources->GetD3DDeviceContext();
	//context->DrawIndexed(indexCount, startIndexLocation, baseVertexLocation);
	m_commandList->DrawIndexedInstanced(indexCount, 1, startIndexLocation, baseVertexLocation, 0);
}

void GraphicsContext::UploadMesh(MMDMesh ^ mesh)
{
	//D3D11_SUBRESOURCE_DATA vertexBufferData = { 0 };
	//vertexBufferData.pSysMem = mesh->m_verticeData->begin();
	//vertexBufferData.SysMemPitch = 0;
	//vertexBufferData.SysMemSlicePitch = 0;
	//CD3D11_BUFFER_DESC vertexBufferDesc(mesh->m_verticeData->Length, D3D11_BIND_VERTEX_BUFFER);

	//DX::ThrowIfFailed(
	//	m_deviceResources->GetD3DDevice()->CreateBuffer(
	//		&vertexBufferDesc,
	//		&vertexBufferData,
	//		&mesh->m_vertexBuffer
	//	)
	//);
	//if (mesh->m_verticeData2->Length != 0) {
	//	D3D11_SUBRESOURCE_DATA vertexBufferData2 = { 0 };
	//	vertexBufferData2.pSysMem = mesh->m_verticeData2->begin();
	//	vertexBufferData2.SysMemPitch = 0;
	//	vertexBufferData2.SysMemSlicePitch = 0;
	//	CD3D11_BUFFER_DESC vertexBufferDesc1(mesh->m_verticeData2->Length, D3D11_BIND_VERTEX_BUFFER);

	//	DX::ThrowIfFailed(
	//		m_deviceResources->GetD3DDevice()->CreateBuffer(
	//			&vertexBufferDesc1,
	//			&vertexBufferData2,
	//			&mesh->m_vertexBuffer2
	//		)
	//	);
	//}

	//D3D11_SUBRESOURCE_DATA indexBufferData = { 0 };
	//indexBufferData.pSysMem = mesh->m_indexData->begin();
	//indexBufferData.SysMemPitch = 0;
	//indexBufferData.SysMemSlicePitch = 0;
	//CD3D11_BUFFER_DESC indexBufferDesc(mesh->m_indexData->Length, D3D11_BIND_INDEX_BUFFER);

	//DX::ThrowIfFailed(
	//	m_deviceResources->GetD3DDevice()->CreateBuffer(
	//		&indexBufferDesc,
	//		&indexBufferData,
	//		&mesh->m_indexBuffer
	//	)
	//);
	auto d3dDevice = m_deviceResources->GetD3DDevice();

	CD3DX12_HEAP_PROPERTIES defaultHeapProperties(D3D12_HEAP_TYPE_DEFAULT);
	CD3DX12_HEAP_PROPERTIES uploadHeapProperties(D3D12_HEAP_TYPE_UPLOAD);
	{
		CD3DX12_RESOURCE_DESC vertexBufferDesc = CD3DX12_RESOURCE_DESC::Buffer(mesh->m_verticeData->Length);
		DX::ThrowIfFailed(d3dDevice->CreateCommittedResource(
			&defaultHeapProperties,
			D3D12_HEAP_FLAG_NONE,
			&vertexBufferDesc,
			D3D12_RESOURCE_STATE_COPY_DEST,
			nullptr,
			IID_PPV_ARGS(&mesh->m_vertexBuffers[0])));

		DX::ThrowIfFailed(d3dDevice->CreateCommittedResource(
			&uploadHeapProperties,
			D3D12_HEAP_FLAG_NONE,
			&vertexBufferDesc,
			D3D12_RESOURCE_STATE_GENERIC_READ,
			nullptr,
			IID_PPV_ARGS(&mesh->m_vertexBufferUpload[0])));
		NAME_D3D12_OBJECT(mesh->m_vertexBuffers[0]);

		D3D12_SUBRESOURCE_DATA vertexData = {};
		vertexData.pData = mesh->m_verticeData->begin();
		vertexData.RowPitch = mesh->m_verticeData->Length;
		vertexData.SlicePitch = vertexData.RowPitch;

		UpdateSubresources(m_commandList.Get(), mesh->m_vertexBuffers[0].Get(), mesh->m_vertexBufferUpload[0].Get(), 0, 0, 1, &vertexData);

		CD3DX12_RESOURCE_BARRIER vertexBufferResourceBarrier =
			CD3DX12_RESOURCE_BARRIER::Transition(mesh->m_vertexBuffers[0].Get(), D3D12_RESOURCE_STATE_COPY_DEST, D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER);
		m_commandList->ResourceBarrier(1, &vertexBufferResourceBarrier);
	}
	if (mesh->m_verticeData2->Length > 0)
	{
		CD3DX12_RESOURCE_DESC vertexBufferDesc = CD3DX12_RESOURCE_DESC::Buffer(mesh->m_verticeData2->Length);
		DX::ThrowIfFailed(d3dDevice->CreateCommittedResource(
			&defaultHeapProperties,
			D3D12_HEAP_FLAG_NONE,
			&vertexBufferDesc,
			D3D12_RESOURCE_STATE_COPY_DEST,
			nullptr,
			IID_PPV_ARGS(&mesh->m_vertexBuffers[1])));

		DX::ThrowIfFailed(d3dDevice->CreateCommittedResource(
			&uploadHeapProperties,
			D3D12_HEAP_FLAG_NONE,
			&vertexBufferDesc,
			D3D12_RESOURCE_STATE_GENERIC_READ,
			nullptr,
			IID_PPV_ARGS(&mesh->m_vertexBufferUpload[1])));
		NAME_D3D12_OBJECT(mesh->m_vertexBuffers[1]);

		D3D12_SUBRESOURCE_DATA vertexData = {};
		vertexData.pData = mesh->m_verticeData2->begin();
		vertexData.RowPitch = mesh->m_verticeData2->Length;
		vertexData.SlicePitch = vertexData.RowPitch;

		UpdateSubresources(m_commandList.Get(), mesh->m_vertexBuffers[1].Get(), mesh->m_vertexBufferUpload[1].Get(), 0, 0, 1, &vertexData);

		CD3DX12_RESOURCE_BARRIER vertexBufferResourceBarrier =
			CD3DX12_RESOURCE_BARRIER::Transition(mesh->m_vertexBuffers[1].Get(), D3D12_RESOURCE_STATE_COPY_DEST, D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER);
		m_commandList->ResourceBarrier(1, &vertexBufferResourceBarrier);
	}
	{
		CD3DX12_RESOURCE_DESC indexBufferDesc = CD3DX12_RESOURCE_DESC::Buffer(mesh->m_indexData->Length);
		DX::ThrowIfFailed(d3dDevice->CreateCommittedResource(
			&defaultHeapProperties,
			D3D12_HEAP_FLAG_NONE,
			&indexBufferDesc,
			D3D12_RESOURCE_STATE_COPY_DEST,
			nullptr,
			IID_PPV_ARGS(&mesh->m_indexBuffer)));

		DX::ThrowIfFailed(d3dDevice->CreateCommittedResource(
			&uploadHeapProperties,
			D3D12_HEAP_FLAG_NONE,
			&indexBufferDesc,
			D3D12_RESOURCE_STATE_GENERIC_READ,
			nullptr,
			IID_PPV_ARGS(&mesh->m_indexBufferUpload)));

		NAME_D3D12_OBJECT(mesh->m_indexBuffer);

		// ���������������ص� GPU��
		{
			D3D12_SUBRESOURCE_DATA indexData = {};
			indexData.pData = mesh->m_indexData->begin();
			indexData.RowPitch = mesh->m_indexData->Length;
			indexData.SlicePitch = indexData.RowPitch;

			UpdateSubresources(m_commandList.Get(), mesh->m_indexBuffer.Get(), mesh->m_indexBufferUpload.Get(), 0, 0, 1, &indexData);

			CD3DX12_RESOURCE_BARRIER indexBufferResourceBarrier =
				CD3DX12_RESOURCE_BARRIER::Transition(mesh->m_indexBuffer.Get(), D3D12_RESOURCE_STATE_COPY_DEST, D3D12_RESOURCE_STATE_INDEX_BUFFER);
			m_commandList->ResourceBarrier(1, &indexBufferResourceBarrier);
		}
	}


	// ��������/������������ͼ��
	mesh->m_vertexBufferView[0].BufferLocation = mesh->m_vertexBuffers[0]->GetGPUVirtualAddress();
	mesh->m_vertexBufferView[0].StrideInBytes = mesh->m_vertexStride;
	mesh->m_vertexBufferView[0].SizeInBytes = mesh->m_vertexStride*mesh->m_vertexCount;

	mesh->m_vertexBufferView[1].BufferLocation = mesh->m_vertexBuffers[1]->GetGPUVirtualAddress();
	mesh->m_vertexBufferView[1].StrideInBytes = mesh->m_vertexStride2;
	mesh->m_vertexBufferView[1].SizeInBytes = mesh->m_vertexStride2*mesh->m_vertexCount;

	mesh->m_indexBufferView.BufferLocation = mesh->m_indexBuffer->GetGPUVirtualAddress();
	mesh->m_indexBufferView.SizeInBytes = mesh->m_indexCount*mesh->m_indexStride;
	mesh->m_indexBufferView.Format = DXGI_FORMAT_R32_UINT;
}

void GraphicsContext::UploadTexture(Texture2D ^ texture)
{
	//D3D11_TEXTURE2D_DESC tex2DDesc = {};
	//tex2DDesc.Width = texture->m_width;
	//tex2DDesc.Height = texture->m_height;
	//tex2DDesc.Format = texture->m_format;
	//tex2DDesc.ArraySize = 1;
	//tex2DDesc.SampleDesc.Count = 1;
	//tex2DDesc.SampleDesc.Quality = 0;
	//tex2DDesc.Usage = D3D11_USAGE_DEFAULT;
	//tex2DDesc.CPUAccessFlags = 0;
	//tex2DDesc.BindFlags = texture->m_bindFlags;

	//tex2DDesc.MiscFlags = 0;
	//tex2DDesc.MipLevels = 1;
	//UINT bytesPerPixel = dxgiFormatBytesPerPixel.find(texture->m_format)->second;

	//D3D11_SUBRESOURCE_DATA subresourceData;
	//subresourceData.pSysMem = texture->m_textureData->begin();
	//subresourceData.SysMemPitch = texture->m_width * bytesPerPixel;
	//subresourceData.SysMemSlicePitch = texture->m_width * texture->m_height * bytesPerPixel;

	//DX::ThrowIfFailed(m_deviceResources->GetD3DDevice()->CreateTexture2D(&tex2DDesc, &subresourceData, &texture->m_texture2D));

	//D3D11_SHADER_RESOURCE_VIEW_DESC shaderResourceViewDesc =
	//	CD3D11_SHADER_RESOURCE_VIEW_DESC(texture->m_texture2D.Get(), D3D11_SRV_DIMENSION_TEXTURE2D, texture->m_format);
	//DX::ThrowIfFailed(m_deviceResources->GetD3DDevice()->CreateShaderResourceView(
	//	texture->m_texture2D.Get(),
	//	&shaderResourceViewDesc,
	//	&texture->m_shaderResourceView
	//));
	//float color[4] = { 1,0,1,1 };
	//D3D11_SAMPLER_DESC samplerDesc;
	//samplerDesc.Filter = D3D11_FILTER_MIN_MAG_MIP_LINEAR;
	//samplerDesc.AddressU = D3D11_TEXTURE_ADDRESS_WRAP;
	//samplerDesc.AddressV = D3D11_TEXTURE_ADDRESS_WRAP;
	//samplerDesc.AddressW = D3D11_TEXTURE_ADDRESS_WRAP;
	//samplerDesc.MipLODBias = 0.0f;
	//samplerDesc.MaxAnisotropy = m_deviceResources->GetDeviceFeatureLevel() > D3D_FEATURE_LEVEL_9_1 ? 4 : 2;
	//samplerDesc.ComparisonFunc = D3D11_COMPARISON_NEVER;
	//samplerDesc.BorderColor[0] = 1.0f;
	//samplerDesc.BorderColor[1] = 0.0f;
	//samplerDesc.BorderColor[2] = 1.0f;
	//samplerDesc.BorderColor[3] = 1.0f;
	//samplerDesc.MinLOD = 0;
	//samplerDesc.MaxLOD = D3D11_FLOAT32_MAX;

	//DX::ThrowIfFailed(m_deviceResources->GetD3DDevice()->CreateSamplerState(&samplerDesc, &texture->m_samplerState));
}

void GraphicsContext::SetMesh(MMDMesh ^ mesh)
{
	//auto context = m_deviceResources->GetD3DDeviceContext();
	//context->IASetVertexBuffers(0, 1, mesh->m_vertexBuffer.GetAddressOf(), &mesh->m_vertexStride, &mesh->m_vertexOffset);
	//if (mesh->m_vertexBuffer2 != nullptr)
	//	context->IASetVertexBuffers(1, 1, mesh->m_vertexBuffer2.GetAddressOf(), &mesh->m_vertexStride2, &mesh->m_vertexOffset);
	//context->IASetIndexBuffer(mesh->m_indexBuffer.Get(), DXGI_FORMAT_R32_UINT, 0);
	//context->IASetPrimitiveTopology(mesh->m_primitiveTopology);
	m_commandList->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
	m_commandList->IASetVertexBuffers(0, 1, &mesh->m_vertexBufferView[0]);
	m_commandList->IASetVertexBuffers(1, 1, &mesh->m_vertexBufferView[1]);
	m_commandList->IASetIndexBuffer(&mesh->m_indexBufferView);
}

void GraphicsContext::SetRenderTargetScreenAndClear(Windows::Foundation::Numerics::float4 color)
{
	// ���������ͼ������Ρ�
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

void GraphicsContext::SetAndClearDSV(RenderTexture2D ^ texture)
{
	//auto context = m_deviceResources->GetD3DDeviceContext();
	//D3D11_VIEWPORT viewport = CD3D11_VIEWPORT(
	//	0.0f,
	//	0.0f,
	//	texture->m_width,
	//	texture->m_height
	//);
	//context->RSSetViewports(1, &viewport);
	//ID3D11RenderTargetView* rtv[1] = {};
	//context->OMSetRenderTargets(1, rtv, texture->m_depthStencilView.Get());
	//context->ClearDepthStencilView(texture->m_depthStencilView.Get(), D3D11_CLEAR_DEPTH | D3D11_CLEAR_STENCIL, 1.0f, 0);
}

void GraphicsContext::SetRootSignature(GraphicsSignature ^ rootSignature)
{
	m_commandList->SetGraphicsRootSignature(rootSignature->m_rootSignatures->Get());
}

void GraphicsContext::ResourceBarrierScreen(D3D12ResourceStates before, D3D12ResourceStates after)
{
	CD3DX12_RESOURCE_BARRIER resourceBarrier =
		CD3DX12_RESOURCE_BARRIER::Transition(m_deviceResources->GetRenderTarget(), (D3D12_RESOURCE_STATES)before, (D3D12_RESOURCE_STATES)after);
	m_commandList->ResourceBarrier(1, &resourceBarrier);
}

void GraphicsContext::ClearDepthStencil()
{
	//auto context = m_deviceResources->GetD3DDeviceContext();
	//context->ClearDepthStencilView(m_deviceResources->GetDepthStencilView(), D3D11_CLEAR_DEPTH | D3D11_CLEAR_STENCIL, 1.0f, 0);
}

void GraphicsContext::BeginAlloctor(DeviceResources ^ deviceResources)
{
	DX::ThrowIfFailed(deviceResources->GetCommandAllocator()->Reset());
}

void GraphicsContext::BeginCommand()
{
	DX::ThrowIfFailed(m_commandList->Reset(m_deviceResources->GetCommandAllocator(), nullptr));
}

void GraphicsContext::EndCommand()
{
	DX::ThrowIfFailed(m_commandList->Close());
}

void GraphicsContext::Execute()
{
	ID3D12CommandList* ppCommandLists[] = { m_commandList.Get() };
	m_deviceResources->GetCommandQueue()->ExecuteCommandLists(_countof(ppCommandLists), ppCommandLists);
}
