#include "pch.h"
#include "TextureCube.h"
#include "DirectXHelper.h"
#include "DirectXTex/DirectXTex.h"
using namespace Coocoo3DGraphics;
using namespace Microsoft::WRL;
using namespace Windows::Foundation;
using namespace Concurrency;


void TextureCube::ReloadPure(int width, int height, const Platform::Array<Windows::Foundation::Numerics::float4>^ color)
{
	m_width = width;
	m_height = height;
	m_format = DXGI_FORMAT_R32G32B32A32_FLOAT;
	m_mipLevels = 1;
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
	m_imageMipsData.clear();
	m_imageMipsData.emplace_back(ImageMipsData{ m_width, m_height });
}

inline UINT CountMips(_In_ UINT width, _In_ UINT height) noexcept
{
	UINT mipLevels = 1;

	while (height > 1 || width > 1)
	{
		if (height > 1)
			height >>= 1;

		if (width > 1)
			width >>= 1;

		++mipLevels;
	}

	return mipLevels;
}

inline void _Fun1(TextureCube^ tex, DirectX::TexMetadata& metaData, DirectX::ScratchImage& scratchImage, DirectX::ScratchImage& generatedMips)
{
	tex->m_format = DirectX::MakeSRGB(metaData.format);

	tex->m_mipLevels = max(CountMips(tex->m_width, tex->m_height) - 6, 1);

	DX::ThrowIfFailed(DirectX::GenerateMipMaps(*scratchImage.GetImage(0, 0, 0), DirectX::TEX_FILTER_LINEAR | DirectX::TEX_FILTER_MIRROR, tex->m_mipLevels, generatedMips));
	tex->m_textureData = ref new Platform::Array<byte, 1>(generatedMips.GetPixelsSize() * 6);

	tex->m_imageMipsData.clear();
	tex->m_imageMipsData.reserve(generatedMips.GetImageCount());
	for (int i = 0; i < generatedMips.GetImageCount(); i++)
	{
		auto image = generatedMips.GetImage(i, 0, 0);
		tex->m_imageMipsData.emplace_back(ImageMipsData{ static_cast<UINT>(image->width), static_cast<UINT>(image->height) });
	}
	//tex->m_mipLevels = max(generatedMips.GetImageCount() - 6, 1);
	//tex->m_mipLevels = 2;
	memcpy(tex->m_textureData->begin(), generatedMips.GetPixels(), generatedMips.GetPixelsSize());
}

inline void _Fun2(TextureCube^ tex, DirectX::TexMetadata& metaData, DirectX::ScratchImage& scratchImage, DirectX::ScratchImage& generatedMips, int index)
{
	DX::ThrowIfFailed(DirectX::GenerateMipMaps(*scratchImage.GetImage(0, 0, 0), DirectX::TEX_FILTER_LINEAR | DirectX::TEX_FILTER_MIRROR, tex->m_mipLevels, generatedMips));

	DX::ThrowIfFalse((6 * generatedMips.GetPixelsSize()) == tex->m_textureData->Length);
	memcpy(tex->m_textureData->begin() + index * generatedMips.GetPixelsSize(), generatedMips.GetPixels(), generatedMips.GetPixelsSize());
}

void TextureCube::ReloadFromImage1(WICFactory^ wicFactory, int width, int height, const Platform::Array<byte>^ data1, const Platform::Array<byte>^ data2, const Platform::Array<byte>^ data3, const Platform::Array<byte>^ data4, const Platform::Array<byte>^ data5, const Platform::Array<byte>^ data6)
{
	m_width = width;
	m_height = height;
	m_format = DXGI_FORMAT_R16G16B16A16_FLOAT;
	m_mipLevels = 1;

	WICRect rect = {};
	rect.Width = m_width;
	rect.Height = m_height;

	int _sizePerPixel = 8;
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
		DX::ThrowIfFailed(WICConvertBitmapSource(GUID_WICPixelFormat64bppRGBAHalf, frameDecode.Get(), &convertedBitmap));

		DX::ThrowIfFailed(convertedBitmap->CopyPixels(&rect, m_width * _sizePerPixel, _sizePerImage, m_textureData->begin() + i * _sizePerImage));

		GlobalUnlock(HGlobalImage);
	}
}

void TextureCube::ReloadFromImage(WICFactory^ wicFactory, int width, int height, IBuffer^ file1, IBuffer^ file2, IBuffer^ file3, IBuffer^ file4, IBuffer^ file5, IBuffer^ file6)
{
	m_width = width;
	m_height = height;

	{
		ComPtr<IBufferByteAccess> bufferByteAccess;
		reinterpret_cast<IInspectable*>(file1)->QueryInterface(IID_PPV_ARGS(&bufferByteAccess));
		byte* pixels = nullptr;
		DX::ThrowIfFailed(bufferByteAccess->Buffer(&pixels));
		DirectX::TexMetadata metaData;
		DirectX::ScratchImage scratchImage;
		DirectX::ScratchImage generatedMips;
		HRESULT hr1 = DirectX::GetMetadataFromTGAMemory(pixels, file1->Length, metaData);
		if (SUCCEEDED(hr1))
		{
			DX::ThrowIfFailed(DirectX::LoadFromTGAMemory(pixels, file1->Length, &metaData, scratchImage));
			_Fun1(this, metaData, scratchImage, generatedMips);
			goto SomeWhere1;
		}

		HRESULT hr2 = DirectX::GetMetadataFromHDRMemory(pixels, file1->Length, metaData);
		if (SUCCEEDED(hr2))
		{
			DX::ThrowIfFailed(DirectX::LoadFromHDRMemory(pixels, file1->Length, &metaData, scratchImage));
			_Fun1(this, metaData, scratchImage, generatedMips);
			goto SomeWhere1;
		}

		HRESULT hr3 = DirectX::GetMetadataFromWICMemory(pixels, file1->Length, DirectX::WIC_FLAGS_NONE, metaData);
		if (SUCCEEDED(hr3))
		{
			DX::ThrowIfFailed(DirectX::LoadFromWICMemory(pixels, file1->Length, DirectX::WIC_FLAGS_NONE, &metaData, scratchImage));
			_Fun1(this, metaData, scratchImage, generatedMips);
			goto SomeWhere1;
		}
	}
SomeWhere1:
	for (int i = 1; i < 6; i++)
	{
		IBuffer^ data;

		if (i == 0)data = file1;
		else if (i == 1)data = file2;
		else if (i == 2)data = file3;
		else if (i == 3)data = file4;
		else if (i == 4)data = file5;
		else data = file6;

		ComPtr<IBufferByteAccess> bufferByteAccess;
		reinterpret_cast<IInspectable*>(data)->QueryInterface(IID_PPV_ARGS(&bufferByteAccess));
		byte* pixels = nullptr;
		DX::ThrowIfFailed(bufferByteAccess->Buffer(&pixels));

		DirectX::TexMetadata metaData;
		DirectX::ScratchImage scratchImage;
		DirectX::ScratchImage generatedMips;
		HRESULT hr1 = DirectX::GetMetadataFromTGAMemory(pixels, data->Length, metaData);
		if (SUCCEEDED(hr1))
		{
			DX::ThrowIfFailed(DirectX::LoadFromTGAMemory(pixels, data->Length, &metaData, scratchImage));
			_Fun2(this, metaData, scratchImage, generatedMips,i);
			continue;
		}

		HRESULT hr2 = DirectX::GetMetadataFromHDRMemory(pixels, data->Length, metaData);
		if (SUCCEEDED(hr2))
		{
			DX::ThrowIfFailed(DirectX::LoadFromHDRMemory(pixels, data->Length, &metaData, scratchImage));
			_Fun2(this, metaData, scratchImage, generatedMips,i);
			continue;
		}

		HRESULT hr3 = DirectX::GetMetadataFromWICMemory(pixels, data->Length, DirectX::WIC_FLAGS_NONE, metaData);
		if (SUCCEEDED(hr3))
		{
			DX::ThrowIfFailed(DirectX::LoadFromWICMemory(pixels, data->Length, DirectX::WIC_FLAGS_NONE, &metaData, scratchImage));
			_Fun2(this, metaData, scratchImage, generatedMips,i);
			continue;
		}
	}


	//m_format = DXGI_FORMAT_R16G16B16A16_FLOAT;
	//m_mipLevels = 1;
	//WICRect rect = {};
	//rect.Width = m_width;
	//rect.Height = m_height;

	//int _sizePerPixel = 8;
	//int _sizePerImage = m_width * m_height * _sizePerPixel;
	//m_textureData = ref new Platform::Array<byte, 1>(_sizePerImage * 6);
	//for (int i = 0; i < 6; i++)
	//{
	//	IBuffer^ data;

	//	if (i == 0)data = file1;
	//	else if (i == 1)data = file2;
	//	else if (i == 2)data = file3;
	//	else if (i == 3)data = file4;
	//	else if (i == 4)data = file5;
	//	else data = file6;

	//	ComPtr<IBufferByteAccess> bufferByteAccess;
	//	reinterpret_cast<IInspectable*>(data)->QueryInterface(IID_PPV_ARGS(&bufferByteAccess));
	//	byte* pixels = nullptr;
	//	DX::ThrowIfFailed(bufferByteAccess->Buffer(&pixels));

	//	HGLOBAL HGlobalImage = GlobalAlloc(GMEM_ZEROINIT | GMEM_MOVEABLE, data->Length);
	//	ComPtr<IStream> memStream = nullptr;
	//	DX::ThrowIfFailed(CreateStreamOnHGlobal(HGlobalImage, true, &memStream));
	//	DX::ThrowIfFailed(memStream->Write(pixels, data->Length, nullptr));
	//	DX::ThrowIfFailed(memStream->Seek(LARGE_INTEGER{ 0,0 }, STREAM_SEEK_SET, nullptr));

	//	auto factory = wicFactory->GetWicImagingFactory();
	//	ComPtr<IWICBitmapDecoder> decoder = nullptr;
	//	ComPtr<IWICBitmapFrameDecode> frameDecode = nullptr;
	//	DX::ThrowIfFailed(factory->CreateDecoderFromStream(memStream.Get(), nullptr, WICDecodeMetadataCacheOnDemand, &decoder));
	//	DX::ThrowIfFailed(decoder->GetFrame(0, &frameDecode));


	//	UINT width1;
	//	UINT height1;

	//	DX::ThrowIfFailed(frameDecode->GetSize(&width1, &height1));


	//	ComPtr<IWICBitmapSource> convertedBitmap = nullptr;
	//	DX::ThrowIfFailed(WICConvertBitmapSource(GUID_WICPixelFormat64bppRGBAHalf, frameDecode.Get(), &convertedBitmap));

	//	DX::ThrowIfFailed(convertedBitmap->CopyPixels(&rect, m_width * _sizePerPixel, _sizePerImage, m_textureData->begin() + i * _sizePerImage));

	//	GlobalUnlock(HGlobalImage);
	//}
}

void TextureCube::ReleaseUploadHeapResource()
{
	m_textureUpload.Reset();
}
