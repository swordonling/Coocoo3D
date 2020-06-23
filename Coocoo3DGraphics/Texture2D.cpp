#include "pch.h"
#include "Texture2D.h"
#include "DirectXHelper.h"
using namespace Coocoo3DGraphics;
using namespace Microsoft::WRL;

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

Texture2D ^ Texture2D::LoadFromImage(DeviceResources^ deviceResources, const Platform::Array<byte>^ data)
{
	Texture2D^ renderTexture = ref new Texture2D();
	renderTexture->ReloadFromImage(deviceResources, data);
	return renderTexture;
}

void Texture2D::ReloadFromImage(DeviceResources ^ deviceResources, const Platform::Array<byte>^ data)
{
	HGLOBAL HGlobalImage = GlobalAlloc(GMEM_ZEROINIT | GMEM_MOVEABLE, data->Length);
	ComPtr<IStream> memStream = nullptr;
	DX::ThrowIfFailed(CreateStreamOnHGlobal(HGlobalImage, true, &memStream));
	DX::ThrowIfFailed(memStream->Write(data->begin(), data->Length, nullptr));
	DX::ThrowIfFailed(memStream->Seek(LARGE_INTEGER{ 0,0 }, STREAM_SEEK_SET, nullptr));

	auto factory = deviceResources->GetWicImagingFactory();
	ComPtr<IWICBitmapDecoder> decoder = nullptr;
	ComPtr<IWICBitmapFrameDecode> frameDecode = nullptr;
	DX::ThrowIfFailed(factory->CreateDecoderFromStream(memStream.Get(), nullptr, WICDecodeMetadataCacheOnDemand, &decoder));
	DX::ThrowIfFailed(decoder->GetFrame(0, &frameDecode));
	WICPixelFormatGUID pixelFormatGuid;
	frameDecode->GetPixelFormat(&pixelFormatGuid);

	auto wicdata = wicFormatInfo.at(pixelFormatGuid);
	DXGI_FORMAT dxgiFormat = wicdata.dxgiFormat;
	DXGI_FORMAT fallbackDxgiFormat;
	WICPixelFormatGUID fallbackWicFormat = wicdata.fallbackWicFormat;
	UINT bytesPerPixel;
	if (dxgiFormat != DXGI_FORMAT_UNKNOWN)
		bytesPerPixel = dxgiFormatBytesPerPixel.at(dxgiFormat);
	else {
		wicdata = wicFormatInfo.at(fallbackWicFormat);
		fallbackDxgiFormat = wicdata.dxgiFormat;
		bytesPerPixel = dxgiFormatBytesPerPixel.at(fallbackDxgiFormat);
	}
	void* bitmapData;
	UINT width;
	UINT height;
	WICRect rect = {};
	DX::ThrowIfFailed(frameDecode->GetSize(&width, &height));
	bitmapData = malloc(width*height * bytesPerPixel);
	rect.Width = width;
	rect.Height = height;
	if (dxgiFormat != DXGI_FORMAT_UNKNOWN) {
		DX::ThrowIfFailed(frameDecode->CopyPixels(&rect, width* bytesPerPixel, width*height * bytesPerPixel, (byte*)bitmapData));
		Initialize(deviceResources, width, height, dxgiFormat, D3D11_BIND_SHADER_RESOURCE, bitmapData);
	}
	else {
		ComPtr<IWICBitmapSource> convertedBitmap = nullptr;
		DX::ThrowIfFailed(WICConvertBitmapSource(fallbackWicFormat, frameDecode.Get(), &convertedBitmap));

		DX::ThrowIfFailed(convertedBitmap->CopyPixels(&rect, width* bytesPerPixel, width*height * bytesPerPixel, (byte*)bitmapData));
		Initialize(deviceResources, width, height, fallbackDxgiFormat, D3D11_BIND_SHADER_RESOURCE, bitmapData);
	}
	free(bitmapData);
	GlobalUnlock(HGlobalImage);

	return;
}

Texture2D ^ Texture2D::LoadFromData(DeviceResources ^ deviceResources, int width, int height, DxgiFormat dxgiFormat, const Platform::Array<byte>^ data)
{
	Texture2D^ renderTexture = ref new Texture2D();
	renderTexture->ReloadFromData(deviceResources, width, height, dxgiFormat, data);
	return renderTexture;
}

void Texture2D::ReloadFromData(DeviceResources ^ deviceResources, int width, int height, DxgiFormat dxgiFormat, const Platform::Array<byte>^ data)
{
	Initialize(deviceResources, width, height, (DXGI_FORMAT)dxgiFormat, D3D11_BIND_SHADER_RESOURCE, data->begin());
}

Texture2D ^ Texture2D::LoadAsRenderTexture(DeviceResources ^ deviceResources, int width, int height, DxgiFormat dxgiFormat)
{
	Texture2D^ renderTexture = ref new Texture2D();
	renderTexture->ReloadAsRenderTexture(deviceResources, width, height, dxgiFormat);
	return renderTexture;
}

void Texture2D::ReloadAsRenderTexture(DeviceResources ^ deviceResources, int width, int height, DxgiFormat dxgiFormat)
{
	Initialize2(deviceResources, width, height, (DXGI_FORMAT)dxgiFormat, D3D11_BIND_SHADER_RESOURCE | D3D11_BIND_UNORDERED_ACCESS);
}

void Texture2D::ReloadAsDepthStencil(DeviceResources ^ deviceResources, int width, int height)
{
	Initialize3(deviceResources, width, height);
}

void Texture2D::ReloadPure(DeviceResources ^ deviceResources, int width, int height, Windows::Foundation::Numerics::float4 color)
{
	int count = width * height;
	void* p = malloc(count * 16);
	float*p1 = (float*)p;
	for (int i = 0; i < count; i++) {
		*p1 = color.x;
		*(p1 + 1) = color.y;
		*(p1 + 2) = color.z;
		*(p1 + 3) = color.w;
		p1 += 4;
	}
	Initialize(deviceResources, width, height, DXGI_FORMAT_R32G32B32A32_FLOAT, D3D11_BIND_SHADER_RESOURCE, p);
	free(p);
}

void Texture2D::Reload(Texture2D ^ texture)
{
	m_texture2D = texture->m_texture2D;
	m_shaderResourceView = texture->m_shaderResourceView;
	m_samplerState = texture->m_samplerState;
	m_unorderedAccessView = texture->m_unorderedAccessView;
	m_depthStencilView = texture->m_depthStencilView;
	m_width = texture->m_width;
	m_height = texture->m_height;
}

void Texture2D::Reload(DeviceResources^ deviceResources, Coocoo3DSmallPack ^ pack)
{
	Initialize(deviceResources, pack->width, pack->height, (DXGI_FORMAT)pack->value1, pack->value2, pack->pDataUnManaged);
}

Coocoo3DSmallPack^ Texture2D::LoadImage(DeviceResources^ deviceResources, const Platform::Array<byte>^ data)
{

	HGLOBAL HGlobalImage = GlobalAlloc(GMEM_ZEROINIT | GMEM_MOVEABLE, data->Length);
	ComPtr<IStream> memStream = nullptr;
	DX::ThrowIfFailed(CreateStreamOnHGlobal(HGlobalImage, true, &memStream));
	DX::ThrowIfFailed(memStream->Write(data->begin(), data->Length, nullptr));
	DX::ThrowIfFailed(memStream->Seek(LARGE_INTEGER{ 0,0 }, STREAM_SEEK_SET, nullptr));

	auto factory = deviceResources->GetWicImagingFactory();
	ComPtr<IWICBitmapDecoder> decoder = nullptr;
	ComPtr<IWICBitmapFrameDecode> frameDecode = nullptr;
	DX::ThrowIfFailed(factory->CreateDecoderFromStream(memStream.Get(), nullptr, WICDecodeMetadataCacheOnDemand, &decoder));
	DX::ThrowIfFailed(decoder->GetFrame(0, &frameDecode));
	WICPixelFormatGUID pixelFormatGuid;
	frameDecode->GetPixelFormat(&pixelFormatGuid);

	auto wicdata = wicFormatInfo.at(pixelFormatGuid);
	DXGI_FORMAT dxgiFormat = wicdata.dxgiFormat;
	DXGI_FORMAT fallbackDxgiFormat;
	WICPixelFormatGUID fallbackWicFormat = wicdata.fallbackWicFormat;
	UINT bytesPerPixel;
	if (dxgiFormat != DXGI_FORMAT_UNKNOWN)
		bytesPerPixel = dxgiFormatBytesPerPixel.at(dxgiFormat);
	else {
		wicdata = wicFormatInfo.at(fallbackWicFormat);
		fallbackDxgiFormat = wicdata.dxgiFormat;
		bytesPerPixel = dxgiFormatBytesPerPixel.at(fallbackDxgiFormat);
	}
	Coocoo3DSmallPack^ pack = ref new Coocoo3DSmallPack();
	pack->value2 = D3D11_BIND_SHADER_RESOURCE;
	UINT width;
	UINT height;
	WICRect rect = {};
	DX::ThrowIfFailed(frameDecode->GetSize(&width, &height));
	pack->pDataUnManaged = malloc(width*height * bytesPerPixel);
	rect.Width = width;
	rect.Height = height;
	pack->width = width;
	pack->height = height;
	if (dxgiFormat != DXGI_FORMAT_UNKNOWN) {
		DX::ThrowIfFailed(frameDecode->CopyPixels(&rect, width* bytesPerPixel, width*height * bytesPerPixel, (byte*)pack->pDataUnManaged));
		pack->value1 = dxgiFormat;
	}
	else {
		ComPtr<IWICBitmapSource> convertedBitmap = nullptr;
		DX::ThrowIfFailed(WICConvertBitmapSource(fallbackWicFormat, frameDecode.Get(), &convertedBitmap));

		DX::ThrowIfFailed(convertedBitmap->CopyPixels(&rect, width* bytesPerPixel, width*height * bytesPerPixel, (byte*)pack->pDataUnManaged));
		pack->value1 = fallbackDxgiFormat;
	}
	GlobalUnlock(HGlobalImage);

	return pack;
}

Texture2D::~Texture2D()
{
}

void Texture2D::Initialize(DeviceResources ^ deviceResources, int width, int height, DXGI_FORMAT format, UINT bindFlags, void * initData)
{
	m_width = width;
	m_height = height;
	D3D11_TEXTURE2D_DESC tex2DDesc = {};
	tex2DDesc.Width = width;
	tex2DDesc.Height = height;
	tex2DDesc.Format = format;
	tex2DDesc.ArraySize = 1;
	tex2DDesc.SampleDesc.Count = 1;
	tex2DDesc.SampleDesc.Quality = 0;
	tex2DDesc.Usage = D3D11_USAGE_DEFAULT;
	tex2DDesc.CPUAccessFlags = 0;
	tex2DDesc.BindFlags = bindFlags;
	//if (useMipMap)
	//{
	//tex2DDesc.BindFlags |= D3D11_BIND_RENDER_TARGET;
	//tex2DDesc.MiscFlags = D3D11_RESOURCE_MISC_GENERATE_MIPS;
	//tex2DDesc.MipLevels = 0;
	//}
	//else
	//{
	tex2DDesc.MiscFlags = 0;
	tex2DDesc.MipLevels = 1;
	//}
	UINT bytesPerPixel = dxgiFormatBytesPerPixel.find(format)->second;

	D3D11_SUBRESOURCE_DATA subresourceData;
	subresourceData.pSysMem = initData;
	subresourceData.SysMemPitch = width * bytesPerPixel;
	subresourceData.SysMemSlicePitch = width * height * bytesPerPixel;

	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->CreateTexture2D(&tex2DDesc, &subresourceData, &m_texture2D));

	D3D11_SHADER_RESOURCE_VIEW_DESC shaderResourceViewDesc =
		CD3D11_SHADER_RESOURCE_VIEW_DESC(m_texture2D.Get(), D3D11_SRV_DIMENSION_TEXTURE2D, format);
	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->CreateShaderResourceView(
		m_texture2D.Get(),
		&shaderResourceViewDesc,
		&m_shaderResourceView
	));
	m_unorderedAccessView = nullptr;
	//deviceResources->GetD3DDeviceContext()->GenerateMips(m_shaderResourceView.Get());
	float color[4] = { 1,0,1,1 };
	D3D11_SAMPLER_DESC samplerDesc;
	samplerDesc.Filter = D3D11_FILTER_MIN_MAG_MIP_LINEAR;
	samplerDesc.AddressU = D3D11_TEXTURE_ADDRESS_WRAP;
	samplerDesc.AddressV = D3D11_TEXTURE_ADDRESS_WRAP;
	samplerDesc.AddressW = D3D11_TEXTURE_ADDRESS_WRAP;
	samplerDesc.MipLODBias = 0.0f;
	samplerDesc.MaxAnisotropy = deviceResources->GetDeviceFeatureLevel() > D3D_FEATURE_LEVEL_9_1 ? 4 : 2;
	samplerDesc.ComparisonFunc = D3D11_COMPARISON_NEVER;
	samplerDesc.BorderColor[0] = 1.0f;
	samplerDesc.BorderColor[1] = 0.0f;
	samplerDesc.BorderColor[2] = 1.0f;
	samplerDesc.BorderColor[3] = 1.0f;
	samplerDesc.MinLOD = 0;
	samplerDesc.MaxLOD = D3D11_FLOAT32_MAX;

	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->CreateSamplerState(&samplerDesc, &m_samplerState));
}

void Texture2D::Initialize2(DeviceResources ^ deviceResources, int width, int height, DXGI_FORMAT format, UINT bindFlags)
{
	m_width = width;
	m_height = height;
	D3D11_TEXTURE2D_DESC tex2DDesc = {};
	tex2DDesc.Width = width;
	tex2DDesc.Height = height;
	tex2DDesc.Format = format;
	tex2DDesc.ArraySize = 1;
	tex2DDesc.SampleDesc.Count = 1;
	tex2DDesc.SampleDesc.Quality = 0;
	tex2DDesc.Usage = D3D11_USAGE_DEFAULT;
	tex2DDesc.CPUAccessFlags = 0;
	tex2DDesc.BindFlags = bindFlags;
	tex2DDesc.MiscFlags = 0;
	tex2DDesc.MipLevels = 1;

	D3D11_SUBRESOURCE_DATA subresourceData;
	subresourceData.pSysMem = nullptr;
	subresourceData.SysMemPitch = 0;
	subresourceData.SysMemSlicePitch = 0;

	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->CreateTexture2D(&tex2DDesc, &subresourceData, &m_texture2D));

	D3D11_SHADER_RESOURCE_VIEW_DESC shaderResourceViewDesc =
		CD3D11_SHADER_RESOURCE_VIEW_DESC(m_texture2D.Get(), D3D11_SRV_DIMENSION_TEXTURE2D, format);
	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->CreateShaderResourceView(
		m_texture2D.Get(),
		&shaderResourceViewDesc,
		&m_shaderResourceView
	));

	D3D11_UNORDERED_ACCESS_VIEW_DESC unorderedAccessViewDesc =
		CD3D11_UNORDERED_ACCESS_VIEW_DESC(m_texture2D.Get(), D3D11_UAV_DIMENSION_TEXTURE2D);
	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->CreateUnorderedAccessView(
		m_texture2D.Get(),
		&unorderedAccessViewDesc,
		&m_unorderedAccessView
	));

	float color[4] = { 1,0,1,1 };
	D3D11_SAMPLER_DESC samplerDesc;
	samplerDesc.Filter = D3D11_FILTER_MIN_MAG_MIP_LINEAR;
	samplerDesc.AddressU = D3D11_TEXTURE_ADDRESS_WRAP;
	samplerDesc.AddressV = D3D11_TEXTURE_ADDRESS_WRAP;
	samplerDesc.AddressW = D3D11_TEXTURE_ADDRESS_WRAP;
	samplerDesc.MipLODBias = 0.0f;
	samplerDesc.MaxAnisotropy = deviceResources->GetDeviceFeatureLevel() > D3D_FEATURE_LEVEL_9_1 ? 4 : 2;
	samplerDesc.ComparisonFunc = D3D11_COMPARISON_NEVER;
	samplerDesc.BorderColor[0] = 1.0f;
	samplerDesc.BorderColor[1] = 0.0f;
	samplerDesc.BorderColor[2] = 1.0f;
	samplerDesc.BorderColor[3] = 1.0f;
	samplerDesc.MinLOD = 0;
	samplerDesc.MaxLOD = D3D11_FLOAT32_MAX;

	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->CreateSamplerState(&samplerDesc, &m_samplerState));
}

void Texture2D::Initialize3(DeviceResources ^ deviceResources, int width, int height)
{
	m_width = width;
	m_height = height;
	D3D11_TEXTURE2D_DESC tex2DDesc = {};
	tex2DDesc.Width = width;
	tex2DDesc.Height = height;
	tex2DDesc.Format = DXGI_FORMAT_R24G8_TYPELESS;
	tex2DDesc.ArraySize = 1;
	tex2DDesc.SampleDesc.Count = 1;
	tex2DDesc.SampleDesc.Quality = 0;
	tex2DDesc.Usage = D3D11_USAGE_DEFAULT;
	tex2DDesc.CPUAccessFlags = 0;
	tex2DDesc.BindFlags = D3D11_BIND_DEPTH_STENCIL | D3D11_BIND_SHADER_RESOURCE;
	tex2DDesc.MiscFlags = 0;
	tex2DDesc.MipLevels = 1;

	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->CreateTexture2D(&tex2DDesc, nullptr, &m_texture2D));

	D3D11_SHADER_RESOURCE_VIEW_DESC shaderResourceViewDesc =
		CD3D11_SHADER_RESOURCE_VIEW_DESC(m_texture2D.Get(), D3D11_SRV_DIMENSION_TEXTURE2D, DXGI_FORMAT_R24_UNORM_X8_TYPELESS);
	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->CreateShaderResourceView(
		m_texture2D.Get(),
		&shaderResourceViewDesc,
		&m_shaderResourceView
	));

	D3D11_DEPTH_STENCIL_VIEW_DESC depthStencilViewDesc = {};
	depthStencilViewDesc.Format = DXGI_FORMAT_D24_UNORM_S8_UINT;
	depthStencilViewDesc.ViewDimension = D3D11_DSV_DIMENSION_TEXTURE2D;
	depthStencilViewDesc.Texture2D.MipSlice = 0;
	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->CreateDepthStencilView(m_texture2D.Get(), &depthStencilViewDesc, &m_depthStencilView));

	float color[4] = { 1,0,1,1 };
	D3D11_SAMPLER_DESC samplerDesc;
	samplerDesc.Filter = D3D11_FILTER_COMPARISON_MIN_MAG_MIP_LINEAR;
	samplerDesc.AddressU = D3D11_TEXTURE_ADDRESS_WRAP;
	samplerDesc.AddressV = D3D11_TEXTURE_ADDRESS_WRAP;
	samplerDesc.AddressW = D3D11_TEXTURE_ADDRESS_WRAP;
	samplerDesc.MipLODBias = 0.0f;
	samplerDesc.MaxAnisotropy = deviceResources->GetDeviceFeatureLevel() > D3D_FEATURE_LEVEL_9_1 ? 4 : 2;
	samplerDesc.ComparisonFunc = D3D11_COMPARISON_LESS;
	samplerDesc.BorderColor[0] = 1.0f;
	samplerDesc.BorderColor[1] = 0.0f;
	samplerDesc.BorderColor[2] = 1.0f;
	samplerDesc.BorderColor[3] = 1.0f;
	samplerDesc.MinLOD = 0;
	samplerDesc.MaxLOD = D3D11_FLOAT32_MAX;

	DX::ThrowIfFailed(deviceResources->GetD3DDevice()->CreateSamplerState(&samplerDesc, &m_samplerState));
}
