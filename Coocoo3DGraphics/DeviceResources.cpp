﻿#include "pch.h"
#include "DeviceResources.h"
#include "DirectXHelper.h"
#include <windows.ui.xaml.media.dxinterop.h>

using namespace DirectX;
using namespace Microsoft::WRL;
using namespace Windows::Foundation;
using namespace Windows::Graphics::Display;
using namespace Windows::UI::Core;
using namespace Windows::UI::Xaml::Controls;
using namespace Platform;
using namespace Coocoo3DGraphics;

// 用于计算屏幕旋转的常量。
namespace ScreenRotation
{
	// 0 度 Z 旋转
	static const XMFLOAT4X4 Rotation0(
		1.0f, 0.0f, 0.0f, 0.0f,
		0.0f, 1.0f, 0.0f, 0.0f,
		0.0f, 0.0f, 1.0f, 0.0f,
		0.0f, 0.0f, 0.0f, 1.0f
	);

	// 90 度 Z 旋转
	static const XMFLOAT4X4 Rotation90(
		0.0f, 1.0f, 0.0f, 0.0f,
		-1.0f, 0.0f, 0.0f, 0.0f,
		0.0f, 0.0f, 1.0f, 0.0f,
		0.0f, 0.0f, 0.0f, 1.0f
	);

	// 180 度 Z 旋转
	static const XMFLOAT4X4 Rotation180(
		-1.0f, 0.0f, 0.0f, 0.0f,
		0.0f, -1.0f, 0.0f, 0.0f,
		0.0f, 0.0f, 1.0f, 0.0f,
		0.0f, 0.0f, 0.0f, 1.0f
	);

	// 270 度 Z 旋转
	static const XMFLOAT4X4 Rotation270(
		0.0f, -1.0f, 0.0f, 0.0f,
		1.0f, 0.0f, 0.0f, 0.0f,
		0.0f, 0.0f, 1.0f, 0.0f,
		0.0f, 0.0f, 0.0f, 1.0f
	);
};

UINT DeviceResources::BitsPerPixel(DXGI_FORMAT format)
{
	switch (static_cast<int>(format))
	{
	case DXGI_FORMAT_R32G32B32A32_TYPELESS:
	case DXGI_FORMAT_R32G32B32A32_FLOAT:
	case DXGI_FORMAT_R32G32B32A32_UINT:
	case DXGI_FORMAT_R32G32B32A32_SINT:
		return 128;

	case DXGI_FORMAT_R32G32B32_TYPELESS:
	case DXGI_FORMAT_R32G32B32_FLOAT:
	case DXGI_FORMAT_R32G32B32_UINT:
	case DXGI_FORMAT_R32G32B32_SINT:
		return 96;

	case DXGI_FORMAT_R16G16B16A16_TYPELESS:
	case DXGI_FORMAT_R16G16B16A16_FLOAT:
	case DXGI_FORMAT_R16G16B16A16_UNORM:
	case DXGI_FORMAT_R16G16B16A16_UINT:
	case DXGI_FORMAT_R16G16B16A16_SNORM:
	case DXGI_FORMAT_R16G16B16A16_SINT:
	case DXGI_FORMAT_R32G32_TYPELESS:
	case DXGI_FORMAT_R32G32_FLOAT:
	case DXGI_FORMAT_R32G32_UINT:
	case DXGI_FORMAT_R32G32_SINT:
	case DXGI_FORMAT_R32G8X24_TYPELESS:
	case DXGI_FORMAT_D32_FLOAT_S8X24_UINT:
	case DXGI_FORMAT_R32_FLOAT_X8X24_TYPELESS:
	case DXGI_FORMAT_X32_TYPELESS_G8X24_UINT:
	case DXGI_FORMAT_Y416:
	case DXGI_FORMAT_Y210:
	case DXGI_FORMAT_Y216:
		return 64;

	case DXGI_FORMAT_R10G10B10A2_TYPELESS:
	case DXGI_FORMAT_R10G10B10A2_UNORM:
	case DXGI_FORMAT_R10G10B10A2_UINT:
	case DXGI_FORMAT_R11G11B10_FLOAT:
	case DXGI_FORMAT_R8G8B8A8_TYPELESS:
	case DXGI_FORMAT_R8G8B8A8_UNORM:
	case DXGI_FORMAT_R8G8B8A8_UNORM_SRGB:
	case DXGI_FORMAT_R8G8B8A8_UINT:
	case DXGI_FORMAT_R8G8B8A8_SNORM:
	case DXGI_FORMAT_R8G8B8A8_SINT:
	case DXGI_FORMAT_R16G16_TYPELESS:
	case DXGI_FORMAT_R16G16_FLOAT:
	case DXGI_FORMAT_R16G16_UNORM:
	case DXGI_FORMAT_R16G16_UINT:
	case DXGI_FORMAT_R16G16_SNORM:
	case DXGI_FORMAT_R16G16_SINT:
	case DXGI_FORMAT_R32_TYPELESS:
	case DXGI_FORMAT_D32_FLOAT:
	case DXGI_FORMAT_R32_FLOAT:
	case DXGI_FORMAT_R32_UINT:
	case DXGI_FORMAT_R32_SINT:
	case DXGI_FORMAT_R24G8_TYPELESS:
	case DXGI_FORMAT_D24_UNORM_S8_UINT:
	case DXGI_FORMAT_R24_UNORM_X8_TYPELESS:
	case DXGI_FORMAT_X24_TYPELESS_G8_UINT:
	case DXGI_FORMAT_R9G9B9E5_SHAREDEXP:
	case DXGI_FORMAT_R8G8_B8G8_UNORM:
	case DXGI_FORMAT_G8R8_G8B8_UNORM:
	case DXGI_FORMAT_B8G8R8A8_UNORM:
	case DXGI_FORMAT_B8G8R8X8_UNORM:
	case DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM:
	case DXGI_FORMAT_B8G8R8A8_TYPELESS:
	case DXGI_FORMAT_B8G8R8A8_UNORM_SRGB:
	case DXGI_FORMAT_B8G8R8X8_TYPELESS:
	case DXGI_FORMAT_B8G8R8X8_UNORM_SRGB:
	case DXGI_FORMAT_AYUV:
	case DXGI_FORMAT_Y410:
	case DXGI_FORMAT_YUY2:
		return 32;

	case DXGI_FORMAT_P010:
	case DXGI_FORMAT_P016:
		return 24;

	case DXGI_FORMAT_R8G8_TYPELESS:
	case DXGI_FORMAT_R8G8_UNORM:
	case DXGI_FORMAT_R8G8_UINT:
	case DXGI_FORMAT_R8G8_SNORM:
	case DXGI_FORMAT_R8G8_SINT:
	case DXGI_FORMAT_R16_TYPELESS:
	case DXGI_FORMAT_R16_FLOAT:
	case DXGI_FORMAT_D16_UNORM:
	case DXGI_FORMAT_R16_UNORM:
	case DXGI_FORMAT_R16_UINT:
	case DXGI_FORMAT_R16_SNORM:
	case DXGI_FORMAT_R16_SINT:
	case DXGI_FORMAT_B5G6R5_UNORM:
	case DXGI_FORMAT_B5G5R5A1_UNORM:
	case DXGI_FORMAT_A8P8:
	case DXGI_FORMAT_B4G4R4A4_UNORM:
		return 16;

	case DXGI_FORMAT_NV12:
	case DXGI_FORMAT_420_OPAQUE:
	case DXGI_FORMAT_NV11:
		return 12;

	case DXGI_FORMAT_R8_TYPELESS:
	case DXGI_FORMAT_R8_UNORM:
	case DXGI_FORMAT_R8_UINT:
	case DXGI_FORMAT_R8_SNORM:
	case DXGI_FORMAT_R8_SINT:
	case DXGI_FORMAT_A8_UNORM:
	case DXGI_FORMAT_AI44:
	case DXGI_FORMAT_IA44:
	case DXGI_FORMAT_P8:
		return 8;

	case DXGI_FORMAT_R1_UNORM:
		return 1;

	case DXGI_FORMAT_BC1_TYPELESS:
	case DXGI_FORMAT_BC1_UNORM:
	case DXGI_FORMAT_BC1_UNORM_SRGB:
	case DXGI_FORMAT_BC4_TYPELESS:
	case DXGI_FORMAT_BC4_UNORM:
	case DXGI_FORMAT_BC4_SNORM:
		return 4;

	case DXGI_FORMAT_BC2_TYPELESS:
	case DXGI_FORMAT_BC2_UNORM:
	case DXGI_FORMAT_BC2_UNORM_SRGB:
	case DXGI_FORMAT_BC3_TYPELESS:
	case DXGI_FORMAT_BC3_UNORM:
	case DXGI_FORMAT_BC3_UNORM_SRGB:
	case DXGI_FORMAT_BC5_TYPELESS:
	case DXGI_FORMAT_BC5_UNORM:
	case DXGI_FORMAT_BC5_SNORM:
	case DXGI_FORMAT_BC6H_TYPELESS:
	case DXGI_FORMAT_BC6H_UF16:
	case DXGI_FORMAT_BC6H_SF16:
	case DXGI_FORMAT_BC7_TYPELESS:
	case DXGI_FORMAT_BC7_UNORM:
	case DXGI_FORMAT_BC7_UNORM_SRGB:
		return 8;

	default:
		return 0;

	}
}


inline bool IsDirectXRaytracingSupported(ID3D12Device* testDevice)
{
	D3D12_FEATURE_DATA_D3D12_OPTIONS5 featureSupportData = {};

	return SUCCEEDED(testDevice->CheckFeatureSupport(D3D12_FEATURE_D3D12_OPTIONS5, &featureSupportData, sizeof(featureSupportData)))
		&& featureSupportData.RaytracingTier != D3D12_RAYTRACING_TIER_NOT_SUPPORTED;
}

// 配置 Direct3D 设备，并存储设备句柄和设备上下文。
void DeviceResources::CreateDeviceResources()
{
#if defined(_DEBUG)
	// 如果项目处于调试生成阶段，请通过 SDK 层启用调试。
	{
		ComPtr<ID3D12Debug> debugController;
		if (SUCCEEDED(D3D12GetDebugInterface(IID_PPV_ARGS(&debugController))))
		{
			debugController->EnableDebugLayer();
		}
	}
#endif

	DX::ThrowIfFailed(CreateDXGIFactory1(IID_PPV_ARGS(&m_dxgiFactory)));

	ComPtr<IDXGIAdapter1> adapter;
	GetHardwareAdapter(&adapter);

	// 创建 Direct3D 12 API 设备对象
	HRESULT hr = D3D12CreateDevice(
		adapter.Get(),					// 硬件适配器。
		D3D_FEATURE_LEVEL_11_0,			// 此应用可以支持的最低功能级别。
		IID_PPV_ARGS(&m_d3dDevice)		// 返回创建的 Direct3D 设备。
	);

#if defined(_DEBUG)
	if (FAILED(hr))
	{
		// 如果初始化失败，则回退到 WARP 设备。
		// 有关 WARP 的详细信息，请参阅: 
		// https://go.microsoft.com/fwlink/?LinkId=286690

		ComPtr<IDXGIAdapter> warpAdapter;
		DX::ThrowIfFailed(m_dxgiFactory->EnumWarpAdapter(IID_PPV_ARGS(&warpAdapter)));

		hr = D3D12CreateDevice(warpAdapter.Get(), D3D_FEATURE_LEVEL_11_0, IID_PPV_ARGS(&m_d3dDevice));
	}
#endif

	DX::ThrowIfFailed(hr);

	m_isRayTracingSupport = IsDirectXRaytracingSupported(m_d3dDevice.Get());

	m_d3dDevice->QueryInterface(IID_PPV_ARGS(&m_d3dDevice2));
	m_d3dDevice->QueryInterface(IID_PPV_ARGS(&m_d3dDevice5));

	// 创建命令队列。
	D3D12_COMMAND_QUEUE_DESC queueDesc = {};
	queueDesc.Flags = D3D12_COMMAND_QUEUE_FLAG_NONE;
	queueDesc.Type = D3D12_COMMAND_LIST_TYPE_DIRECT;

	DX::ThrowIfFailed(m_d3dDevice->CreateCommandQueue(&queueDesc, IID_PPV_ARGS(&m_commandQueue)));
	NAME_D3D12_OBJECT(m_commandQueue);

	// 为呈现器目标视图和深度模具视图创建描述符堆。
	D3D12_DESCRIPTOR_HEAP_DESC rtvHeapDesc = {};
	rtvHeapDesc.NumDescriptors = 2048;
	rtvHeapDesc.Type = D3D12_DESCRIPTOR_HEAP_TYPE_RTV;
	rtvHeapDesc.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_NONE;
	DX::ThrowIfFailed(m_d3dDevice->CreateDescriptorHeap(&rtvHeapDesc, IID_PPV_ARGS(&m_rtvHeap)));
	NAME_D3D12_OBJECT(m_rtvHeap);
	m_rtvHeapAllocCount = 3;
	m_rtvDescriptorSize = m_d3dDevice->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_RTV);

	D3D12_DESCRIPTOR_HEAP_DESC dsvHeapDesc = {};
	dsvHeapDesc.NumDescriptors = 2048;
	dsvHeapDesc.Type = D3D12_DESCRIPTOR_HEAP_TYPE_DSV;
	dsvHeapDesc.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_NONE;
	DX::ThrowIfFailed(m_d3dDevice->CreateDescriptorHeap(&dsvHeapDesc, IID_PPV_ARGS(&m_dsvHeap)));
	NAME_D3D12_OBJECT(m_dsvHeap);
	m_dsvHeapAllocCount = 0;

	D3D12_DESCRIPTOR_HEAP_DESC heapDesc = {};
	heapDesc.NumDescriptors = c_graphicsPipelineHeapMaxCount;
	heapDesc.Type = D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV;
	heapDesc.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE;
	DX::ThrowIfFailed(m_d3dDevice->CreateDescriptorHeap(&heapDesc, IID_PPV_ARGS(&m_cbvSrvUavHeap)));
	NAME_D3D12_OBJECT(m_cbvSrvUavHeap);
	m_cbvSrvUavHeapAllocCount = 0;

	for (UINT n = 0; n < c_frameCount; n++)
	{
		DX::ThrowIfFailed(
			m_d3dDevice->CreateCommandAllocator(D3D12_COMMAND_LIST_TYPE_DIRECT, IID_PPV_ARGS(&m_commandAllocators[n]))
		);
	}

	// 创建同步对象。
	DX::ThrowIfFailed(m_d3dDevice->CreateFence(m_fenceValues[m_currentFrame], D3D12_FENCE_FLAG_NONE, IID_PPV_ARGS(&m_fence)));
	m_fenceValues[m_currentFrame]++;

	m_fenceEvent = CreateEvent(nullptr, FALSE, FALSE, nullptr);
	if (m_fenceEvent == nullptr)
	{
		DX::ThrowIfFailed(HRESULT_FROM_WIN32(GetLastError()));
	}
}

// 每次更改窗口大小时需要重新创建这些资源。
void DeviceResources::CreateWindowSizeDependentResources()
{
	// 让Present函数处理它
	m_ResolutionChange = true;

	// 等到以前的所有 GPU 工作完成。
	WaitForGpu();

	// 清除特定于先前窗口大小的内容，并更新所跟踪的围栏值。
	for (UINT n = 0; n < c_frameCount; n++)
	{
		m_renderTargets[n] = nullptr;
		m_fenceValues[n] = m_fenceValues[m_currentFrame];
	}

	UpdateRenderTargetSize();

	// 交换链的宽度和高度必须基于窗口的
	// 面向本机的宽度和高度。如果窗口不在本机
	// 方向，则必须使尺寸反转。
	DXGI_MODE_ROTATION displayRotation = ComputeDisplayRotation();

	bool swapDimensions = displayRotation == DXGI_MODE_ROTATION_ROTATE90 || displayRotation == DXGI_MODE_ROTATION_ROTATE270;
	m_d3dRenderTargetSize.Width = swapDimensions ? m_outputSize.Height : m_outputSize.Width;
	m_d3dRenderTargetSize.Height = swapDimensions ? m_outputSize.Width : m_outputSize.Height;

	UINT backBufferWidth = lround(m_d3dRenderTargetSize.Width);
	UINT backBufferHeight = lround(m_d3dRenderTargetSize.Height);

	if (m_swapChain != nullptr)
	{
		// 如果交换链已存在，请调整其大小。
		HRESULT hr = m_swapChain->ResizeBuffers(c_frameCount, backBufferWidth, backBufferHeight, m_backBufferFormat, DXGI_SWAP_CHAIN_FLAG_ALLOW_TEARING);

		if (hr == DXGI_ERROR_DEVICE_REMOVED || hr == DXGI_ERROR_DEVICE_RESET)
		{
			// 如果出于任何原因移除了设备，将需要创建一个新的设备和交换链。
			m_deviceRemoved = true;

			// 请勿继续执行此方法。会销毁并重新创建 DeviceResources。
			return;
		}
		else
		{
			DX::ThrowIfFailed(hr);
		}
	}
	else
	{
		// 否则，使用与现有 Direct3D 设备相同的适配器新建一个。
		DXGI_SCALING scaling = DXGI_SCALING_STRETCH;
		DXGI_SWAP_CHAIN_DESC1 swapChainDesc = {};

		swapChainDesc.Width = backBufferWidth;						// 匹配窗口的大小。
		swapChainDesc.Height = backBufferHeight;
		swapChainDesc.Format = m_backBufferFormat;
		swapChainDesc.Stereo = false;
		swapChainDesc.SampleDesc.Count = 1;							// 请不要使用多采样。
		swapChainDesc.SampleDesc.Quality = 0;
		swapChainDesc.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
		swapChainDesc.BufferCount = c_frameCount;					// 使用三重缓冲最大程度地减小延迟。
		swapChainDesc.SwapEffect = DXGI_SWAP_EFFECT_FLIP_DISCARD;	// 所有 Windows 通用应用都必须使用 _FLIP_ SwapEffects。
		swapChainDesc.Flags = DXGI_SWAP_CHAIN_FLAG_ALLOW_TEARING;
		swapChainDesc.Scaling = scaling;
		swapChainDesc.AlphaMode = DXGI_ALPHA_MODE_IGNORE;

		ComPtr<IDXGISwapChain1> swapChain;
		DX::ThrowIfFailed(
			m_dxgiFactory->CreateSwapChainForComposition(
				m_commandQueue.Get(),								// 交换链需要对 DirectX 12 中的命令队列的引用。
				&swapChainDesc,
				nullptr,
				&swapChain
			)
		);

		DX::ThrowIfFailed(swapChain.As(&m_swapChain));

		// 将交换链与 SwapChainPanel 关联
		// UI 更改将需要调度回 UI 线程
		m_window->Dispatcher->RunAsync(CoreDispatcherPriority::High, ref new DispatchedHandler([=]()
			{
				//获取 SwapChainPanel 的受支持的本机接口
				ComPtr<ISwapChainPanelNative> panelNative;
				DX::ThrowIfFailed(
					reinterpret_cast<IUnknown*>(m_window)->QueryInterface(IID_PPV_ARGS(&panelNative))
				);

				DX::ThrowIfFailed(
					panelNative->SetSwapChain(m_swapChain.Get())
				);
			}, CallbackContext::Any));
	}

	// 为交换链设置正确方向，并生成
	// 转换以渲染到旋转交换链。
	// 显式指定 3D 矩阵可以避免舍入误差。

	switch (displayRotation)
	{
	case DXGI_MODE_ROTATION_IDENTITY:
		m_orientationTransform3D = ScreenRotation::Rotation0;
		break;

	case DXGI_MODE_ROTATION_ROTATE90:
		m_orientationTransform3D = ScreenRotation::Rotation270;
		break;

	case DXGI_MODE_ROTATION_ROTATE180:
		m_orientationTransform3D = ScreenRotation::Rotation180;
		break;

	case DXGI_MODE_ROTATION_ROTATE270:
		m_orientationTransform3D = ScreenRotation::Rotation90;
		break;

	default:
		throw ref new FailureException();
	}

	DX::ThrowIfFailed(
		m_swapChain->SetRotation(displayRotation)
	);



	// 在交换链上设置反向缩放
	DXGI_MATRIX_3X2_F inverseScale = { 0 };
	inverseScale._11 = 1.0f / m_compositionScaleX;
	inverseScale._22 = 1.0f / m_compositionScaleY;
	ComPtr<IDXGISwapChain2> spSwapChain2;
	DX::ThrowIfFailed(
		m_swapChain.As<IDXGISwapChain2>(&spSwapChain2)
	);

	DX::ThrowIfFailed(
		spSwapChain2->SetMatrixTransform(&inverseScale)
	);

	// 创建交换链后台缓冲区的呈现目标视图。
	{
		m_currentFrame = m_swapChain->GetCurrentBackBufferIndex();
		CD3DX12_CPU_DESCRIPTOR_HANDLE rtvDescriptor(m_rtvHeap->GetCPUDescriptorHandleForHeapStart());
		for (UINT n = 0; n < c_frameCount; n++)
		{
			DX::ThrowIfFailed(m_swapChain->GetBuffer(n, IID_PPV_ARGS(&m_renderTargets[n])));
			m_d3dDevice->CreateRenderTargetView(m_renderTargets[n].Get(), nullptr, rtvDescriptor);
			rtvDescriptor.Offset(m_rtvDescriptorSize);

			WCHAR name[25];
			if (swprintf_s(name, L"m_renderTargets[%u]", n) > 0)
			{
				DX::SetName(m_renderTargets[n].Get(), name);
			}
		}
	}

	// 设置用于确定整个窗口的 3D 渲染视区。
	m_screenViewport = { 0.0f, 0.0f, m_d3dRenderTargetSize.Width, m_d3dRenderTargetSize.Height, 0.0f, 1.0f };
}

// 确定呈现器目标的尺寸及其是否将缩小。
void DeviceResources::UpdateRenderTargetSize()
{
	m_effectiveDpi = m_dpi;

	// 计算必要的呈现目标大小(以像素为单位)。
	m_outputSize.Width = DX::ConvertDipsToPixels(m_logicalSize.Width, m_effectiveDpi);
	m_outputSize.Height = DX::ConvertDipsToPixels(m_logicalSize.Height, m_effectiveDpi);

	// 防止创建大小为零的 DirectX 内容。
	m_outputSize.Width = max(m_outputSize.Width, 1);
	m_outputSize.Height = max(m_outputSize.Height, 1);
}

DeviceResources::DeviceResources() :
	m_currentFrame(0),
	m_screenViewport(),
	m_rtvDescriptorSize(0),
	m_fenceEvent(0),
	m_backBufferFormat(DXGI_FORMAT_B8G8R8A8_UNORM),
	m_fenceValues{},
	m_d3dRenderTargetSize(),
	m_outputSize(),
	m_logicalSize(),
	m_nativeOrientation(DisplayOrientations::None),
	m_currentOrientation(DisplayOrientations::None),
	m_dpi(-1.0f),
	m_effectiveDpi(-1.0f),
	m_deviceRemoved(false)
{
	CreateDeviceResources();
}

// 当创建(或重新创建) CoreWindow 时调用此方法。
void DeviceResources::SetSwapChainPanel(SwapChainPanel^ window)
{
	DisplayInformation^ currentDisplayInformation = DisplayInformation::GetForCurrentView();

	m_window = window;
	m_logicalSize = Windows::Foundation::Size(window->Width, window->Height);
	m_nativeOrientation = currentDisplayInformation->NativeOrientation;
	m_currentOrientation = currentDisplayInformation->CurrentOrientation;
	m_compositionScaleX = window->CompositionScaleX;
	m_compositionScaleY = window->CompositionScaleY;
	m_dpi = currentDisplayInformation->LogicalDpi;

	CreateWindowSizeDependentResources();
}

// 在 SizeChanged 事件的事件处理程序中调用此方法。
void DeviceResources::SetLogicalSize(Windows::Foundation::Size logicalSize)
{
	if (m_logicalSize != logicalSize)
	{
		m_logicalSize = logicalSize;
		CreateWindowSizeDependentResources();
	}
}

// 在 DpiChanged 事件的事件处理程序中调用此方法。
void DeviceResources::SetDpi(float dpi)
{
	if (dpi != m_dpi)
	{
		m_dpi = dpi;

		// 显示 DPI 更改时，窗口的逻辑大小(以 Dip 为单位)也将更改并且需要更新。
		m_logicalSize = Windows::Foundation::Size(m_window->Width, m_window->Height);

		CreateWindowSizeDependentResources();
	}
}

// 在 OrientationChanged 事件的事件处理程序中调用此方法。
void DeviceResources::SetCurrentOrientation(DisplayOrientations currentOrientation)
{
	if (m_currentOrientation != currentOrientation)
	{
		m_currentOrientation = currentOrientation;
		CreateWindowSizeDependentResources();
	}
}

// 在 DisplayContentsInvalidated 事件的事件处理程序中调用此方法。
void DeviceResources::ValidateDevice()
{
	// 如果默认适配器更改，D3D 设备将不再有效，因为该设备
	// 已创建或该设备已移除。

	// 首先在创建设备时，从中获取默认适配器的 LUID。

	DXGI_ADAPTER_DESC previousDesc;
	{
		ComPtr<IDXGIAdapter1> previousDefaultAdapter;
		DX::ThrowIfFailed(m_dxgiFactory->EnumAdapters1(0, &previousDefaultAdapter));

		DX::ThrowIfFailed(previousDefaultAdapter->GetDesc(&previousDesc));
	}

	// 接下来，获取当前默认适配器的信息。

	DXGI_ADAPTER_DESC currentDesc;
	{
		ComPtr<IDXGIFactory4> currentDxgiFactory;
		DX::ThrowIfFailed(CreateDXGIFactory1(IID_PPV_ARGS(&currentDxgiFactory)));

		ComPtr<IDXGIAdapter1> currentDefaultAdapter;
		DX::ThrowIfFailed(currentDxgiFactory->EnumAdapters1(0, &currentDefaultAdapter));

		DX::ThrowIfFailed(currentDefaultAdapter->GetDesc(&currentDesc));
	}

	// 如果适配器 LUID 不匹配，或者该设备报告它已被移除，
	// 则必须创建新的 D3D 设备。

	if (previousDesc.AdapterLuid.LowPart != currentDesc.AdapterLuid.LowPart ||
		previousDesc.AdapterLuid.HighPart != currentDesc.AdapterLuid.HighPart ||
		FAILED(m_d3dDevice->GetDeviceRemovedReason()))
	{
		m_deviceRemoved = true;
	}
}

void DeviceResources::Present(bool vsync)
{
	// 第一个参数指示 DXGI 进行阻止直到 VSync，这使应用程序
// 在下一个 VSync 前进入休眠。这将确保我们不会浪费任何周期渲染
// 从不会在屏幕上显示的帧。
	HRESULT hr;
	if (vsync)
	{
		if (m_ResolutionChange)
		{
			hr = m_swapChain->Present(1, 0);
			m_ResolutionChange = false;
		}
		else
		{
			hr = m_swapChain->Present(1, 0);
		}
	}
	else
	{
		if (m_ResolutionChange)
		{
			hr = m_swapChain->Present(0, DXGI_PRESENT_ALLOW_TEARING);
			m_ResolutionChange = false;
		}
		else
		{
			hr = m_swapChain->Present(0, DXGI_PRESENT_ALLOW_TEARING);
		}
	}

	// 如果通过断开连接或升级驱动程序移除了设备，则必须
	// 必须重新创建所有设备资源。
	if (hr == DXGI_ERROR_DEVICE_REMOVED || hr == DXGI_ERROR_DEVICE_RESET)
	{
		m_deviceRemoved = true;
	}
	else
	{
		DX::ThrowIfFailed(hr);

		MoveToNextFrame();
	}
}

// 等待挂起的 GPU 工作完成。
void DeviceResources::WaitForGpu()
{
	// 在队列中安排信号命令。
	DX::ThrowIfFailed(m_commandQueue->Signal(m_fence.Get(), m_fenceValues[m_currentFrame]));

	// 等待跨越围栏。
	DX::ThrowIfFailed(m_fence->SetEventOnCompletion(m_fenceValues[m_currentFrame], m_fenceEvent));
	WaitForSingleObjectEx(m_fenceEvent, INFINITE, FALSE);

	// 对当前帧递增围栏值。
	m_fenceValues[m_currentFrame]++;
}

bool DeviceResources::IsRayTracingSupport()
{
	return m_isRayTracingSupport;
}

DxgiFormat DeviceResources::GetBackBufferFormat1()
{
	return DxgiFormat(m_backBufferFormat);
}

UINT DeviceResources::BitsPerPixel(DxgiFormat format)
{
	return BitsPerPixel((DXGI_FORMAT)format);
}

Platform::String^ DeviceResources::GetDeviceDescription()
{
	return ref new Platform::String(m_deviceDescription);
}

UINT64 DeviceResources::GetDeviceVideoMemory()
{
	return m_deviceVideoMem;
}

// 准备呈现下一帧。
void DeviceResources::MoveToNextFrame()
{
	// 在队列中安排信号命令。
	const UINT64 currentFenceValue = m_fenceValues[m_currentFrame];
	DX::ThrowIfFailed(m_commandQueue->Signal(m_fence.Get(), currentFenceValue));

	// 提高帧索引。
	m_currentFrame = m_swapChain->GetCurrentBackBufferIndex();

	// 检查下一帧是否准备好启动。
	if (m_fence->GetCompletedValue() < m_fenceValues[m_currentFrame])
	{
		DX::ThrowIfFailed(m_fence->SetEventOnCompletion(m_fenceValues[m_currentFrame], m_fenceEvent));
		WaitForSingleObjectEx(m_fenceEvent, INFINITE, FALSE);
	}

	// 为下一帧设置围栏值。
	m_fenceValues[m_currentFrame] = currentFenceValue + 1;
}

// 此方法确定以下两个元素之间的旋转方式: 显示设备的本机方向和
// 当前显示方向。
DXGI_MODE_ROTATION DeviceResources::ComputeDisplayRotation()
{
	DXGI_MODE_ROTATION rotation = DXGI_MODE_ROTATION_UNSPECIFIED;

	// 注意: NativeOrientation 只能为 "Landscape" 或 "Portrait"，即使
	// DisplayOrientations 枚举具有其他值。
	switch (m_nativeOrientation)
	{
	case DisplayOrientations::Landscape:
		switch (m_currentOrientation)
		{
		case DisplayOrientations::Landscape:
			rotation = DXGI_MODE_ROTATION_IDENTITY;
			break;

		case DisplayOrientations::Portrait:
			rotation = DXGI_MODE_ROTATION_ROTATE270;
			break;

		case DisplayOrientations::LandscapeFlipped:
			rotation = DXGI_MODE_ROTATION_ROTATE180;
			break;

		case DisplayOrientations::PortraitFlipped:
			rotation = DXGI_MODE_ROTATION_ROTATE90;
			break;
		}
		break;

	case DisplayOrientations::Portrait:
		switch (m_currentOrientation)
		{
		case DisplayOrientations::Landscape:
			rotation = DXGI_MODE_ROTATION_ROTATE90;
			break;

		case DisplayOrientations::Portrait:
			rotation = DXGI_MODE_ROTATION_IDENTITY;
			break;

		case DisplayOrientations::LandscapeFlipped:
			rotation = DXGI_MODE_ROTATION_ROTATE270;
			break;

		case DisplayOrientations::PortraitFlipped:
			rotation = DXGI_MODE_ROTATION_ROTATE180;
			break;
		}
		break;
	}
	return rotation;
}

// 此方法获取支持 Direct3D 12 的第一个可用硬件适配器。
// 如果找不到此类适配器，则 *ppAdapter 将设置为 nullptr。
void DeviceResources::GetHardwareAdapter(IDXGIAdapter1** ppAdapter)
{
	ComPtr<IDXGIAdapter1> adapter;
	*ppAdapter = nullptr;

	for (UINT adapterIndex = 0; DXGI_ERROR_NOT_FOUND != m_dxgiFactory->EnumAdapters1(adapterIndex, &adapter); adapterIndex++)
	{
		DXGI_ADAPTER_DESC1 desc;
		adapter->GetDesc1(&desc);

		if (desc.Flags & DXGI_ADAPTER_FLAG_SOFTWARE)
		{
			// 不要选择基本呈现驱动程序适配器。
			continue;
		}

		// 检查适配器是否支持 Direct3D 12，但不要创建
		// 仍为实际设备。
		if (SUCCEEDED(D3D12CreateDevice(adapter.Get(), D3D_FEATURE_LEVEL_11_0, _uuidof(ID3D12Device), nullptr)))
		{
			memcpy(m_deviceDescription, desc.Description, sizeof(desc.Description));
			m_deviceVideoMem = desc.DedicatedVideoMemory;
			break;
		}
	}

	*ppAdapter = adapter.Detach();
}
