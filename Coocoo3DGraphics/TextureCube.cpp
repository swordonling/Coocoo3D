#include "pch.h"
#include "TextureCube.h"
#include "DirectXHelper.h"
using namespace Coocoo3DGraphics;
using namespace Microsoft::WRL;
using namespace Windows::Foundation;
using namespace Concurrency;


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
	m_format = DXGI_FORMAT_R16G16B16A16_FLOAT;
	m_bindFlags = D3D11_BIND_SHADER_RESOURCE;

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
	m_format = DXGI_FORMAT_R16G16B16A16_FLOAT;
	m_bindFlags = D3D11_BIND_SHADER_RESOURCE;

	WICRect rect = {};
	rect.Width = m_width;
	rect.Height = m_height;

	int _sizePerPixel = 8;
	int _sizePerImage = m_width * m_height * _sizePerPixel;
	m_textureData = ref new Platform::Array<byte, 1>(_sizePerImage * 6);

	for (int i = 0; i < 6; i++)
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

		HGLOBAL HGlobalImage = GlobalAlloc(GMEM_ZEROINIT | GMEM_MOVEABLE, data->Length);
		ComPtr<IStream> memStream = nullptr;
		DX::ThrowIfFailed(CreateStreamOnHGlobal(HGlobalImage, true, &memStream));
		DX::ThrowIfFailed(memStream->Write(pixels, data->Length, nullptr));
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

//IAsyncAction^ TextureCube::ReloadFromImage(WICFactory^ wicFactory, int width, int height, StorageFile^ file1, StorageFile^ file2, StorageFile^ file3, StorageFile^ file4, StorageFile^ file5, StorageFile^ file6)
//{
//	m_width = width;
//	m_height = height;
//	m_format = DXGI_FORMAT_R16G16B16A16_FLOAT;
//	m_bindFlags = D3D11_BIND_SHADER_RESOURCE;
//
//	WICRect rect = {};
//	rect.Width = m_width;
//	rect.Height = m_height;
//
//	int _sizePerPixel = 8;
//	int _sizePerImage = m_width * m_height * _sizePerPixel;
//	m_textureData = ref new Platform::Array<byte, 1>(_sizePerImage * 6);
//	return create_async([this, width, height, file1, file2, file3, file4, file5, file6]
//		{
//			auto x = this;
//			auto task2 = create_task([x, width, height, file1, file2, file3, file4, file5, file6]
//				{
//
//				});
//			auto task1 = create_task([x, width, height, file1]
//				{
//					return FileIO::ReadBufferAsync(file1);
//				})
//				.then(create_task([](Streams::IBuffer^ fileBuffer)->std::vector<byte>
//					{
//						std::vector<byte>returnBuffer;
//						returnBuffer.resize(fileBuffer->Length);
//						Streams::DataReader::FromBuffer(fileBuffer)->ReadBytes(Platform::ArrayReference<byte>(returnBuffer.data(), fileBuffer->Length));
//						return returnBuffer;
//					})).then(create_task([] {}));
//
//					return task1;
//		});
//	throw ref new Platform::NotImplementedException();
//	// TODO: 在此处插入 return 语句
//}

void TextureCube::ReleaseUploadHeapResource()
{
	m_textureUpload.Reset();
}
