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

void Texture2D::ReloadPure(int width, int height, Windows::Foundation::Numerics::float4 color)
{
	m_width = width;
	m_height = height;
	m_format = DXGI_FORMAT_R32G32B32A32_FLOAT;
	m_bindFlags = D3D11_BIND_SHADER_RESOURCE;

	int count = width * height;
	m_textureData = ref new Platform::Array<byte, 1>(count * 16);

	void* p = m_textureData->begin();
	float*p1 = (float*)p;
	for (int i = 0; i < count; i++) {
		*p1 = color.x;
		*(p1 + 1) = color.y;
		*(p1 + 2) = color.z;
		*(p1 + 3) = color.w;
		p1 += 4;
	}
}

void Texture2D::Reload(Texture2D ^ texture)
{
	m_width = texture->m_width;
	m_height = texture->m_height;
	m_textureData = texture->m_textureData;
	m_texture = texture->m_texture;
}

void Texture2D::ReloadFromImage1(DeviceResources ^ deviceResources, const Platform::Array<byte>^ data)
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

	DX::ThrowIfFailed(frameDecode->GetSize(&m_width, &m_height));
	m_textureData = ref new Platform::Array<byte, 1>(m_width * m_height * bytesPerPixel);
	m_bindFlags = D3D11_BIND_SHADER_RESOURCE;
	WICRect rect = {};
	rect.Width = m_width;
	rect.Height = m_height;


	if (dxgiFormat != DXGI_FORMAT_UNKNOWN) {
		DX::ThrowIfFailed(frameDecode->CopyPixels(&rect, m_width* bytesPerPixel, m_width * m_height * bytesPerPixel, m_textureData->begin()));
		m_format = dxgiFormat;
	}
	else {
		ComPtr<IWICBitmapSource> convertedBitmap = nullptr;
		DX::ThrowIfFailed(WICConvertBitmapSource(fallbackWicFormat, frameDecode.Get(), &convertedBitmap));

		DX::ThrowIfFailed(convertedBitmap->CopyPixels(&rect, m_width* bytesPerPixel, m_width*m_height * bytesPerPixel, m_textureData->begin()));
		m_format = fallbackDxgiFormat;
	}
	GlobalUnlock(HGlobalImage);
}

Texture2D::~Texture2D()
{
}
