#include "pch.h"
#include "DirectXHelper.h"
#include "GraphicsContext.h"
using namespace Coocoo3DGraphics;

GraphicsContext ^ GraphicsContext::Load(DeviceResources ^ deviceResources)
{
	GraphicsContext^ graphicsContext = ref new GraphicsContext();
	graphicsContext->m_deviceResources = deviceResources;
	return graphicsContext;
}

void GraphicsContext::Reload(DeviceResources ^ deviceResourecs)
{
	m_deviceResources = deviceResourecs;
}

void GraphicsContext::SetMaterial(Material ^ material)
{
	auto context = m_deviceResources->GetD3DDeviceContext();

	if (material->m_pobject->m_vertexShader != nullptr) {
		context->IASetInputLayout(material->m_pobject->m_vertexShader->m_inputLayout.Get());
		context->VSSetShader(material->m_pobject->m_vertexShader->m_vertexShader.Get(), nullptr, 0);
	}
	else {
		context->VSSetShader(nullptr, nullptr, 0);
	}
	if (material->m_pobject->m_geometryShader != nullptr) {
		context->GSSetShader(material->m_pobject->m_geometryShader->m_geometryShader.Get(), nullptr, 0);
	}
	else {
		context->GSSetShader(nullptr, nullptr, 0);
	}
	if (material->m_pobject->m_pixelShader != nullptr) {
		context->PSSetShader(material->m_pobject->m_pixelShader->m_pixelShader.Get(), nullptr, 0);
	}
	else {
		context->PSSetShader(nullptr, nullptr, 0);
	}

	if (material->cullMode == CullMode::none)
		context->RSSetState(m_deviceResources->m_RasterizerStateCullNone.Get());
	if (material->cullMode == CullMode::front)
		context->RSSetState(m_deviceResources->m_RasterizerStateCullFront.Get());
	if (material->cullMode == CullMode::back)
		context->RSSetState(m_deviceResources->m_RasterizerStateCullBack.Get());
	for (int i = 0; i < Material::c_reference_max; i++)
	{
		if (material->references[i] != nullptr&&material->references[i]->m_texture2D != nullptr) {
			context->PSSetShaderResources(i, 1, material->references[i]->m_shaderResourceView.GetAddressOf());
			context->PSSetSamplers(i, 1, material->references[i]->m_samplerState.GetAddressOf());
		}
	}
}

void GraphicsContext::SetPObject(PObject ^ pobject)
{
	auto context = m_deviceResources->GetD3DDeviceContext();

	if (pobject->m_vertexShader != nullptr) {
		context->IASetInputLayout(pobject->m_vertexShader->m_inputLayout.Get());
		context->VSSetShader(pobject->m_vertexShader->m_vertexShader.Get(), nullptr, 0);
	}
	else {
		context->VSSetShader(nullptr, nullptr, 0);
	}
	if (pobject->m_geometryShader != nullptr) {
		context->GSSetShader(pobject->m_geometryShader->m_geometryShader.Get(), nullptr, 0);
	}
	else {
		context->GSSetShader(nullptr, nullptr, 0);
	}
	if (pobject->m_pixelShader != nullptr) {
		context->PSSetShader(pobject->m_pixelShader->m_pixelShader.Get(), nullptr, 0);
	}
	else {
		context->PSSetShader(nullptr, nullptr, 0);
	}
}

void GraphicsContext::SetPObjectDepthOnly(PObject ^ pobject)
{
	auto context = m_deviceResources->GetD3DDeviceContext();

	if (pobject->m_vertexShader != nullptr) {
		context->IASetInputLayout(pobject->m_vertexShader->m_inputLayout.Get());
		context->VSSetShader(pobject->m_vertexShader->m_vertexShader.Get(), nullptr, 0);
	}
	else {
		context->VSSetShader(nullptr, nullptr, 0);
	}
	if (pobject->m_geometryShader != nullptr) {
		context->GSSetShader(pobject->m_geometryShader->m_geometryShader.Get(), nullptr, 0);
	}
	else {
		context->GSSetShader(nullptr, nullptr, 0);
	}
	context->PSSetShader(nullptr, nullptr, 0);
}

void GraphicsContext::SetCullMode(CullMode cullMode)
{
	auto context = m_deviceResources->GetD3DDeviceContext();
	if (cullMode == CullMode::none)
		context->RSSetState(m_deviceResources->m_RasterizerStateCullNone.Get());
	if (cullMode == CullMode::front)
		context->RSSetState(m_deviceResources->m_RasterizerStateCullFront.Get());
	if (cullMode == CullMode::back)
		context->RSSetState(m_deviceResources->m_RasterizerStateCullBack.Get());
}

void GraphicsContext::SetComputeShader(ComputeShader ^ computeShader)
{
	auto context = m_deviceResources->GetD3DDeviceContext();
	if (computeShader->m_computeShader != nullptr) {
		context->CSSetShader(computeShader->m_computeShader.Get(), nullptr, 1);
	}
}

void GraphicsContext::UpdateResource(ConstantBuffer^ buffer, const Platform::Array<byte>^ data)
{
	auto context = m_deviceResources->GetD3DDeviceContext();
	context->UpdateSubresource(buffer->m_buffer.Get(), 0, nullptr, data->begin(), 0, 0);
}

void GraphicsContext::UpdateResource(ConstantBuffer ^ buffer, const Platform::Array<byte>^ data, int dataOffset)
{
	auto context = m_deviceResources->GetD3DDeviceContext();
	context->UpdateSubresource(buffer->m_buffer.Get(), 0, nullptr, data->begin() + dataOffset, 0, 0);
}

void GraphicsContext::UpdateResource(GraphicsBuffer ^ buffer, const Platform::Array<byte>^ data)
{
	auto context = m_deviceResources->GetD3DDeviceContext();
	context->UpdateSubresource(buffer->m_buffer.Get(), 0, nullptr, data->begin(), 0, 0);
}

void GraphicsContext::UpdateVertices(MMDMesh^ mesh, const Platform::Array<byte>^ verticeData)
{
	auto context = m_deviceResources->GetD3DDeviceContext();
	context->UpdateSubresource(mesh->m_vertexBuffer.Get(), 0, nullptr, verticeData->begin(), 0, 0);
}

void GraphicsContext::UpdateVertices2(MMDMesh^ mesh, const Platform::Array<byte>^ verticeData)
{
	auto context = m_deviceResources->GetD3DDeviceContext();
	context->UpdateSubresource(mesh->m_vertexBuffer2.Get(), 0, nullptr, verticeData->begin(), 0, 0);
	//D3D11_MAPPED_SUBRESOURCE mappedSubResource = {};
	//DX::ThrowIfFailed(context->Map(mesh->m_vertexBuffer2.Get(), 0, D3D11_MAP_WRITE_DISCARD, 0, &mappedSubResource));
	//memcpy(mappedSubResource.pData, verticeData->begin(), verticeData->Length);
	//context->Unmap(mesh->m_vertexBuffer2.Get(), 0);
}

void GraphicsContext::UpdateVertices2(MMDMesh^ mesh, const Platform::Array<Windows::Foundation::Numerics::float3>^ verticeData)
{
	auto context = m_deviceResources->GetD3DDeviceContext();
	context->UpdateSubresource(mesh->m_vertexBuffer2.Get(), 0, nullptr, verticeData->begin(), 0, 0);
	//D3D11_MAPPED_SUBRESOURCE mappedSubResource = {};
	//DX::ThrowIfFailed(context->Map(mesh->m_vertexBuffer2.Get(), 0, D3D11_MAP_WRITE_DISCARD, 0, &mappedSubResource));
	//memcpy(mappedSubResource.pData, verticeData->begin(), verticeData->Length * 12);
	//context->Unmap(mesh->m_vertexBuffer2.Get(), 0);
}

void GraphicsContext::CSSetSRV(GraphicsBuffer ^ buffer, int slot)
{
	auto context = m_deviceResources->GetD3DDeviceContext();
	context->CSSetShaderResources(slot, 1, buffer->m_shaderResourceView.GetAddressOf());
}

void GraphicsContext::CSSetSRV(Texture2D ^ texture, int slot)
{
	auto context = m_deviceResources->GetD3DDeviceContext();
	context->CSSetShaderResources(slot, 1, texture->m_shaderResourceView.GetAddressOf());
	context->CSSetSamplers(slot, 1, texture->m_samplerState.GetAddressOf());
}

void GraphicsContext::VSSetSRV(Texture2D ^ texture, int slot)
{
	auto context = m_deviceResources->GetD3DDeviceContext();
	context->VSSetShaderResources(slot, 1, texture->m_shaderResourceView.GetAddressOf());
	context->VSSetSamplers(slot, 1, texture->m_samplerState.GetAddressOf());
}

void GraphicsContext::PSSetSRV(Texture2D ^ texture, int slot)
{
	auto context = m_deviceResources->GetD3DDeviceContext();
	if (texture != nullptr) {
		context->PSSetShaderResources(slot, 1, texture->m_shaderResourceView.GetAddressOf());
		context->PSSetSamplers(slot, 1, texture->m_samplerState.GetAddressOf());
	}
	else
	{
		ID3D11ShaderResourceView* srv[1] = {};
		ID3D11SamplerState* ss[1] = {};
		context->PSSetShaderResources(slot, 1, srv);
		context->PSSetSamplers(slot, 1, ss);
	}
}

void GraphicsContext::CSSetUAV(Texture2D ^ texture, int slot)
{
	auto context = m_deviceResources->GetD3DDeviceContext();
}

void GraphicsContext::CSSetConstantBuffer(ConstantBuffer ^ buffer, int slot)
{
	auto context = m_deviceResources->GetD3DDeviceContext();
	context->CSSetConstantBuffers(slot, 1, buffer->m_buffer.GetAddressOf());
}

void GraphicsContext::VSSetConstantBuffer(ConstantBuffer ^ buffer, int slot)
{
	auto context = m_deviceResources->GetD3DDeviceContext();
	context->VSSetConstantBuffers(slot, 1, buffer->m_buffer.GetAddressOf());
}

void GraphicsContext::GSSetConstantBuffer(ConstantBuffer ^ buffer, int slot)
{
	auto context = m_deviceResources->GetD3DDeviceContext();
	context->GSSetConstantBuffers(slot, 1, buffer->m_buffer.GetAddressOf());
}

void GraphicsContext::PSSetConstantBuffer(ConstantBuffer ^ buffer, int slot)
{
	auto context = m_deviceResources->GetD3DDeviceContext();
	context->PSSetConstantBuffers(slot, 1, buffer->m_buffer.GetAddressOf());
}

void GraphicsContext::Dispathch(int x, int y, int z)
{
	auto context = m_deviceResources->GetD3DDeviceContext();
	context->Dispatch(x, y, z);
}

void GraphicsContext::Draw(int indexCount, int startIndexLocation)
{
	auto context = m_deviceResources->GetD3DDeviceContext();
	context->Draw(indexCount, startIndexLocation);
}

void GraphicsContext::DrawIndexed(int indexCount, int startIndexLocation, int baseVertexLocation)
{
	auto context = m_deviceResources->GetD3DDeviceContext();
	context->DrawIndexed(indexCount, startIndexLocation, baseVertexLocation);
}

void GraphicsContext::SetMesh(MMDMesh ^ mesh)
{
	auto context = m_deviceResources->GetD3DDeviceContext();
	context->IASetVertexBuffers(0, 1, mesh->m_vertexBuffer.GetAddressOf(), &mesh->m_vertexStride, &mesh->m_vertexOffset);
	if (mesh->m_vertexBuffer2 != nullptr)
		context->IASetVertexBuffers(1, 1, mesh->m_vertexBuffer2.GetAddressOf(), &mesh->m_vertexStride2, &mesh->m_vertexOffset);
	context->IASetIndexBuffer(mesh->m_indexBuffer.Get(), DXGI_FORMAT_R32_UINT, 0);
	context->IASetPrimitiveTopology(mesh->m_primitiveTopology);
}

void GraphicsContext::SetRenderTargetScreenAndClear(Windows::Foundation::Numerics::float4 color)
{
	auto context = m_deviceResources->GetD3DDeviceContext();

	auto viewport = m_deviceResources->GetScreenViewport();
	context->RSSetViewports(1, &viewport);

	ID3D11RenderTargetView *const targets[1] = { m_deviceResources->GetBackBufferRenderTargetView() };
	context->OMSetRenderTargets(1, targets, m_deviceResources->GetDepthStencilView());

	float _color[4]{ color.x,color.y,color.z,color.w, };
	context->ClearRenderTargetView(m_deviceResources->GetBackBufferRenderTargetView(), _color);
	context->ClearDepthStencilView(m_deviceResources->GetDepthStencilView(), D3D11_CLEAR_DEPTH | D3D11_CLEAR_STENCIL, 1.0f, 0);
}

void GraphicsContext::SetAndClearDSV(Texture2D^ texture)
{
	auto context = m_deviceResources->GetD3DDeviceContext();
	D3D11_VIEWPORT viewport = CD3D11_VIEWPORT(
		0.0f,
		0.0f,
		texture->m_width,
		texture->m_height
	);
	context->RSSetViewports(1, &viewport);
	ID3D11RenderTargetView* rtv[1] = {};
	context->OMSetRenderTargets(1, rtv, texture->m_depthStencilView.Get());
	context->ClearDepthStencilView(texture->m_depthStencilView.Get(), D3D11_CLEAR_DEPTH | D3D11_CLEAR_STENCIL, 1.0f, 0);
}

void GraphicsContext::SetBlendState(BlendState ^ blendState)
{
	auto context = m_deviceResources->GetD3DDeviceContext();
	context->OMSetBlendState(blendState->m_blendState.Get(), nullptr, 0xffffffff);
}

void GraphicsContext::ClearDepthStencil()
{
	auto context = m_deviceResources->GetD3DDeviceContext();
	context->ClearDepthStencilView(m_deviceResources->GetDepthStencilView(), D3D11_CLEAR_DEPTH | D3D11_CLEAR_STENCIL, 1.0f, 0);
}
