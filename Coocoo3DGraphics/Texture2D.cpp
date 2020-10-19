#include "pch.h"
#include "Texture2D.h"
#include "DirectXHelper.h"
#include "DirectXTex/DirectXTex.h"
using namespace Coocoo3DGraphics;
using namespace Microsoft::WRL;

void Texture2D::ReloadPure(int width, int height, Windows::Foundation::Numerics::float4 color)
{
	m_width = width;
	m_height = height;
	m_format = DXGI_FORMAT_R32G32B32A32_FLOAT;
	m_mipLevels = 1;
	int count = width * height;
	m_textureData = ref new Platform::Array<byte, 1>(count * 16);

	void* p = m_textureData->begin();
	float* p1 = (float*)p;
	for (int i = 0; i < count; i++) {
		*p1 = color.x;
		*(p1 + 1) = color.y;
		*(p1 + 2) = color.z;
		*(p1 + 3) = color.w;
		p1 += 4;
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

inline void _Fun1(Texture2D^ tex, DirectX::TexMetadata& metaData, DirectX::ScratchImage& scratchImage, DirectX::ScratchImage& generatedMips)
{
	tex->m_format = DirectX::MakeSRGB(metaData.format);
	tex->m_width = metaData.width;
	tex->m_height = metaData.height;

	tex->m_mipLevels = max(CountMips(tex->m_width, tex->m_height) - 6, 1);
	if (tex->m_mipLevels > 1)
	{
		DX::ThrowIfFailed(DirectX::GenerateMipMaps(*scratchImage.GetImage(0, 0, 0), DirectX::TEX_FILTER_LINEAR | DirectX::TEX_FILTER_MIRROR | DirectX::TEX_FILTER_SEPARATE_ALPHA, tex->m_mipLevels, generatedMips));
		tex->m_textureData = ref new Platform::Array<byte, 1>(generatedMips.GetPixelsSize());

		tex->m_imageMipsData.clear();
		tex->m_imageMipsData.reserve(generatedMips.GetImageCount());
		for (int i = 0; i < generatedMips.GetImageCount(); i++)
		{
			auto image = generatedMips.GetImage(i, 0, 0);
			tex->m_imageMipsData.emplace_back(ImageMipsData{ static_cast<UINT>(image->width), static_cast<UINT>(image->height) });
		}
		memcpy(tex->m_textureData->begin(), generatedMips.GetPixels(), generatedMips.GetPixelsSize());
	}
	else
	{
		tex->m_textureData = ref new Platform::Array<byte, 1>(scratchImage.GetPixelsSize());
		tex->m_imageMipsData.clear();
		tex->m_imageMipsData.emplace_back(ImageMipsData{ tex->m_width, tex->m_height });
		memcpy(tex->m_textureData->begin(), scratchImage.GetPixels(), scratchImage.GetPixelsSize());
	}
}

inline void _Fun2(Texture2D^ tex, DirectX::TexMetadata& metaData, DirectX::ScratchImage& scratchImage, DirectX::ScratchImage& generatedMips)
{
	tex->m_format = DirectX::MakeSRGB(metaData.format);
	tex->m_width = metaData.width;
	tex->m_height = metaData.height;

	tex->m_mipLevels = 1;

	tex->m_textureData = ref new Platform::Array<byte, 1>(scratchImage.GetPixelsSize());
	tex->m_imageMipsData.clear();
	tex->m_imageMipsData.emplace_back(ImageMipsData{ tex->m_width, tex->m_height });
	memcpy(tex->m_textureData->begin(), scratchImage.GetPixels(), scratchImage.GetPixelsSize());
}

void Texture2D::ReloadFromImage(IBuffer^ file1)
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
		return;
	}

	HRESULT hr2 = DirectX::GetMetadataFromHDRMemory(pixels, file1->Length, metaData);
	if (SUCCEEDED(hr2))
	{
		DX::ThrowIfFailed(DirectX::LoadFromHDRMemory(pixels, file1->Length, &metaData, scratchImage));
		_Fun1(this, metaData, scratchImage, generatedMips);
		return;
	}

	HRESULT hr3 = DirectX::GetMetadataFromWICMemory(pixels, file1->Length, DirectX::WIC_FLAGS_NONE, metaData);
	if (SUCCEEDED(hr3))
	{
		DX::ThrowIfFailed(DirectX::LoadFromWICMemory(pixels, file1->Length, DirectX::WIC_FLAGS_NONE, &metaData, scratchImage));
		_Fun1(this, metaData, scratchImage, generatedMips);
		return;
	}
}

void Texture2D::ReloadFromImageNoMip(IBuffer^ file1)
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
		_Fun2(this, metaData, scratchImage, generatedMips);
		return;
	}

	HRESULT hr2 = DirectX::GetMetadataFromHDRMemory(pixels, file1->Length, metaData);
	if (SUCCEEDED(hr2))
	{
		DX::ThrowIfFailed(DirectX::LoadFromHDRMemory(pixels, file1->Length, &metaData, scratchImage));
		_Fun2(this, metaData, scratchImage, generatedMips);
		return;
	}

	HRESULT hr3 = DirectX::GetMetadataFromWICMemory(pixels, file1->Length, DirectX::WIC_FLAGS_NONE, metaData);
	if (SUCCEEDED(hr3))
	{
		DX::ThrowIfFailed(DirectX::LoadFromWICMemory(pixels, file1->Length, DirectX::WIC_FLAGS_NONE, &metaData, scratchImage));
		_Fun2(this, metaData, scratchImage, generatedMips);
		return;
	}
}

void Texture2D::Reload(Texture2D^ texture)
{
	m_width = texture->m_width;
	m_height = texture->m_height;
	m_textureData = texture->m_textureData;
	m_texture = texture->m_texture;
	m_heapRefIndex = texture->m_heapRefIndex;
	m_mipLevels = texture->m_mipLevels;
}

void Texture2D::Unload()
{
	m_width = 0;
	m_height = 0;
	m_textureData = nullptr;
	m_texture.Reset();
	m_textureUpload.Reset();
	m_format = DXGI_FORMAT_UNKNOWN;
}

void Texture2D::ReleaseUploadHeapResource()
{
	m_textureUpload.Reset();
}

Texture2D::~Texture2D()
{
}
