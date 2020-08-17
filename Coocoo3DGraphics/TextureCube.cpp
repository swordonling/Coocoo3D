#include "pch.h"
#include "TextureCube.h"
#include "DirectXHelper.h"
using namespace Coocoo3DGraphics;
using namespace Microsoft::WRL;

void TextureCube::ReloadPure(int width, int height, const Platform::Array<Windows::Foundation::Numerics::float4>^ color)
{
	m_width = width;
	m_height = height;
	m_format = DXGI_FORMAT_R32G32B32A32_FLOAT;
	m_bindFlags = D3D11_BIND_SHADER_RESOURCE;

	int count = width * height;
	if (count < 256)throw ref new Platform::NotImplementedException("Texture too small");
	m_textureData = ref new Platform::Array<byte, 1>(count * 16 * 6);

	void* p = m_textureData->begin();
	float* p1 = (float*)p;
	for (int c = 0; c < 6; c++)
	{
		for (int i = 0; i < count; i++) {
			*p1 = color[c].x;
			*(p1 + 1) = color[c].y;
			*(p1 + 2) = color[c].z;
			*(p1 + 3) = color[c].w;
			p1 += 4;
		}
	}
}

void TextureCube::ReloadFromImage1(WICFactory^ wicFactory, int width, int height, const Platform::Array<byte>^ data1, const Platform::Array<byte>^ data2, const Platform::Array<byte>^ data3, const Platform::Array<byte>^ data4, const Platform::Array<byte>^ data5, const Platform::Array<byte>^ data6)
{
	m_width = width;
	m_height = height;
	m_format = DXGI_FORMAT_R32G32B32A32_FLOAT;
	m_bindFlags = D3D11_BIND_SHADER_RESOURCE;

	WICRect rect = {};
	rect.Width = m_width;
	rect.Height = m_height;

	int _sizePerPixel = 16;
	int _sizePerImage = m_width * m_height * _sizePerPixel;
	m_textureData = ref new Platform::Array<byte, 1>(_sizePerImage * 6);
	for (int i = 0; i < 6; i++)
	{
		const Platform::Array<byte>^ data;

		if (i == 0)data = data1;
		else if (i == 1)data = data2;
		else if (i == 2)data = data3;
		else if (i == 3)data = data4;
		else if (i == 4)data = data5;
		else data = data6;

		HGLOBAL HGlobalImage = GlobalAlloc(GMEM_ZEROINIT | GMEM_MOVEABLE, data->Length);
		ComPtr<IStream> memStream = nullptr;
		DX::ThrowIfFailed(CreateStreamOnHGlobal(HGlobalImage, true, &memStream));
		DX::ThrowIfFailed(memStream->Write(data->begin(), data->Length, nullptr));
		DX::ThrowIfFailed(memStream->Seek(LARGE_INTEGER{ 0,0 }, STREAM_SEEK_SET, nullptr));

		auto factory = wicFactory->GetWicImagingFactory();
		ComPtr<IWICBitmapDecoder> decoder = nullptr;
		ComPtr<IWICBitmapFrameDecode> frameDecode = nullptr;
		DX::ThrowIfFailed(factory->CreateDecoderFromStream(memStream.Get(), nullptr, WICDecodeMetadataCacheOnDemand, &decoder));
		DX::ThrowIfFailed(decoder->GetFrame(0, &frameDecode));


		UINT width1;
		UINT height1;

		DX::ThrowIfFailed(frameDecode->GetSize(&width1, &height1));


		ComPtr<IWICBitmapSource> convertedBitmap = nullptr;
		DX::ThrowIfFailed(WICConvertBitmapSource(GUID_WICPixelFormat128bppRGBAFloat, frameDecode.Get(), &convertedBitmap));

		DX::ThrowIfFailed(convertedBitmap->CopyPixels(&rect, m_width * _sizePerPixel, _sizePerImage, m_textureData->begin() + i * _sizePerImage));

		GlobalUnlock(HGlobalImage);
	}
}

void TextureCube::ReleaseUploadHeapResource()
{
	m_textureUpload.Reset();
}
