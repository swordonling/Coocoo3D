﻿#pragma once

namespace Coocoo3DGraphics
{
	static const UINT c_frameCount = 3;		// 使用三重缓冲。
	static const UINT c_graphicsPipelineHeapMaxCount = 65536;

	public enum struct DxgiFormat
	{
		DXGI_FORMAT_UNKNOWN = 0,
		DXGI_FORMAT_R32G32B32A32_TYPELESS = 1,
		DXGI_FORMAT_R32G32B32A32_FLOAT = 2,
		DXGI_FORMAT_R32G32B32A32_UINT = 3,
		DXGI_FORMAT_R32G32B32A32_SINT = 4,
		DXGI_FORMAT_R32G32B32_TYPELESS = 5,
		DXGI_FORMAT_R32G32B32_FLOAT = 6,
		DXGI_FORMAT_R32G32B32_UINT = 7,
		DXGI_FORMAT_R32G32B32_SINT = 8,
		DXGI_FORMAT_R16G16B16A16_TYPELESS = 9,
		DXGI_FORMAT_R16G16B16A16_FLOAT = 10,
		DXGI_FORMAT_R16G16B16A16_UNORM = 11,
		DXGI_FORMAT_R16G16B16A16_UINT = 12,
		DXGI_FORMAT_R16G16B16A16_SNORM = 13,
		DXGI_FORMAT_R16G16B16A16_SINT = 14,
		DXGI_FORMAT_R32G32_TYPELESS = 15,
		DXGI_FORMAT_R32G32_FLOAT = 16,
		DXGI_FORMAT_R32G32_UINT = 17,
		DXGI_FORMAT_R32G32_SINT = 18,
		DXGI_FORMAT_R32G8X24_TYPELESS = 19,
		DXGI_FORMAT_D32_FLOAT_S8X24_UINT = 20,
		DXGI_FORMAT_R32_FLOAT_X8X24_TYPELESS = 21,
		DXGI_FORMAT_X32_TYPELESS_G8X24_UINT = 22,
		DXGI_FORMAT_R10G10B10A2_TYPELESS = 23,
		DXGI_FORMAT_R10G10B10A2_UNORM = 24,
		DXGI_FORMAT_R10G10B10A2_UINT = 25,
		DXGI_FORMAT_R11G11B10_FLOAT = 26,
		DXGI_FORMAT_R8G8B8A8_TYPELESS = 27,
		DXGI_FORMAT_R8G8B8A8_UNORM = 28,
		DXGI_FORMAT_R8G8B8A8_UNORM_SRGB = 29,
		DXGI_FORMAT_R8G8B8A8_UINT = 30,
		DXGI_FORMAT_R8G8B8A8_SNORM = 31,
		DXGI_FORMAT_R8G8B8A8_SINT = 32,
		DXGI_FORMAT_R16G16_TYPELESS = 33,
		DXGI_FORMAT_R16G16_FLOAT = 34,
		DXGI_FORMAT_R16G16_UNORM = 35,
		DXGI_FORMAT_R16G16_UINT = 36,
		DXGI_FORMAT_R16G16_SNORM = 37,
		DXGI_FORMAT_R16G16_SINT = 38,
		DXGI_FORMAT_R32_TYPELESS = 39,
		DXGI_FORMAT_D32_FLOAT = 40,
		DXGI_FORMAT_R32_FLOAT = 41,
		DXGI_FORMAT_R32_UINT = 42,
		DXGI_FORMAT_R32_SINT = 43,
		DXGI_FORMAT_R24G8_TYPELESS = 44,
		DXGI_FORMAT_D24_UNORM_S8_UINT = 45,
		DXGI_FORMAT_R24_UNORM_X8_TYPELESS = 46,
		DXGI_FORMAT_X24_TYPELESS_G8_UINT = 47,
		DXGI_FORMAT_R8G8_TYPELESS = 48,
		DXGI_FORMAT_R8G8_UNORM = 49,
		DXGI_FORMAT_R8G8_UINT = 50,
		DXGI_FORMAT_R8G8_SNORM = 51,
		DXGI_FORMAT_R8G8_SINT = 52,
		DXGI_FORMAT_R16_TYPELESS = 53,
		DXGI_FORMAT_R16_FLOAT = 54,
		DXGI_FORMAT_D16_UNORM = 55,
		DXGI_FORMAT_R16_UNORM = 56,
		DXGI_FORMAT_R16_UINT = 57,
		DXGI_FORMAT_R16_SNORM = 58,
		DXGI_FORMAT_R16_SINT = 59,
		DXGI_FORMAT_R8_TYPELESS = 60,
		DXGI_FORMAT_R8_UNORM = 61,
		DXGI_FORMAT_R8_UINT = 62,
		DXGI_FORMAT_R8_SNORM = 63,
		DXGI_FORMAT_R8_SINT = 64,
		DXGI_FORMAT_A8_UNORM = 65,
		DXGI_FORMAT_R1_UNORM = 66,
		DXGI_FORMAT_R9G9B9E5_SHAREDEXP = 67,
		DXGI_FORMAT_R8G8_B8G8_UNORM = 68,
		DXGI_FORMAT_G8R8_G8B8_UNORM = 69,
		DXGI_FORMAT_BC1_TYPELESS = 70,
		DXGI_FORMAT_BC1_UNORM = 71,
		DXGI_FORMAT_BC1_UNORM_SRGB = 72,
		DXGI_FORMAT_BC2_TYPELESS = 73,
		DXGI_FORMAT_BC2_UNORM = 74,
		DXGI_FORMAT_BC2_UNORM_SRGB = 75,
		DXGI_FORMAT_BC3_TYPELESS = 76,
		DXGI_FORMAT_BC3_UNORM = 77,
		DXGI_FORMAT_BC3_UNORM_SRGB = 78,
		DXGI_FORMAT_BC4_TYPELESS = 79,
		DXGI_FORMAT_BC4_UNORM = 80,
		DXGI_FORMAT_BC4_SNORM = 81,
		DXGI_FORMAT_BC5_TYPELESS = 82,
		DXGI_FORMAT_BC5_UNORM = 83,
		DXGI_FORMAT_BC5_SNORM = 84,
		DXGI_FORMAT_B5G6R5_UNORM = 85,
		DXGI_FORMAT_B5G5R5A1_UNORM = 86,
		DXGI_FORMAT_B8G8R8A8_UNORM = 87,
		DXGI_FORMAT_B8G8R8X8_UNORM = 88,
		DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM = 89,
		DXGI_FORMAT_B8G8R8A8_TYPELESS = 90,
		DXGI_FORMAT_B8G8R8A8_UNORM_SRGB = 91,
		DXGI_FORMAT_B8G8R8X8_TYPELESS = 92,
		DXGI_FORMAT_B8G8R8X8_UNORM_SRGB = 93,
		DXGI_FORMAT_BC6H_TYPELESS = 94,
		DXGI_FORMAT_BC6H_UF16 = 95,
		DXGI_FORMAT_BC6H_SF16 = 96,
		DXGI_FORMAT_BC7_TYPELESS = 97,
		DXGI_FORMAT_BC7_UNORM = 98,
		DXGI_FORMAT_BC7_UNORM_SRGB = 99,
		DXGI_FORMAT_AYUV = 100,
		DXGI_FORMAT_Y410 = 101,
		DXGI_FORMAT_Y416 = 102,
		DXGI_FORMAT_NV12 = 103,
		DXGI_FORMAT_P010 = 104,
		DXGI_FORMAT_P016 = 105,
		DXGI_FORMAT_420_OPAQUE = 106,
		DXGI_FORMAT_YUY2 = 107,
		DXGI_FORMAT_Y210 = 108,
		DXGI_FORMAT_Y216 = 109,
		DXGI_FORMAT_NV11 = 110,
		DXGI_FORMAT_AI44 = 111,
		DXGI_FORMAT_IA44 = 112,
		DXGI_FORMAT_P8 = 113,
		DXGI_FORMAT_A8P8 = 114,
		DXGI_FORMAT_B4G4R4A4_UNORM = 115,

		DXGI_FORMAT_P208 = 130,
		DXGI_FORMAT_V208 = 131,
		DXGI_FORMAT_V408 = 132,


		DXGI_FORMAT_FORCE_UINT = 0xffffffff
	};
	// 控制所有 DirectX 设备资源。
	public ref class DeviceResources sealed
	{
	public:
		DeviceResources();
		void CreateDeviceResources();

		void SetSwapChainPanel(Windows::UI::Xaml::Controls::SwapChainPanel^ window);
		void SetLogicalSize(Windows::Foundation::Size logicalSize);
		// 呈现器目标的大小，以像素为单位。
		Windows::Foundation::Size	GetOutputSize() { return m_outputSize; }
		// 呈现器目标的大小，以 dip 为单位。
		Windows::Foundation::Size	GetLogicalSize() { return m_logicalSize; }
		void SetCurrentOrientation(Windows::Graphics::Display::DisplayOrientations currentOrientation);
		void SetDpi(float dpi);
		float GetDpi() { return m_effectiveDpi; }
		void ValidateDevice();
		void Present(bool vsync);
		void WaitForGpu();
		bool IsRayTracingSupport();
		DxgiFormat GetBackBufferFormat1();
		static UINT BitsPerPixel(DxgiFormat format);
	internal:
		bool						IsDeviceRemoved() const { return m_deviceRemoved; }

		// D3D 访问器。
		ID3D12Device*				GetD3DDevice() const { return m_d3dDevice.Get(); }
		ID3D12Device5*				GetD3DDevice5() const { return m_d3dDevice5.Get(); }
		IDXGISwapChain3*			GetSwapChain() const { return m_swapChain.Get(); }
		ID3D12Resource*				GetRenderTarget() const { return m_renderTargets[m_currentFrame].Get(); }
		ID3D12Resource*				GetDepthStencil() const { return m_depthStencil.Get(); }
		ID3D12CommandQueue*			GetCommandQueue() const { return m_commandQueue.Get(); }
		ID3D12CommandAllocator*		GetCommandAllocator() const { return m_commandAllocators[m_currentFrame].Get(); }
		DXGI_FORMAT					GetBackBufferFormat() const { return m_backBufferFormat; }
		DXGI_FORMAT					GetDepthBufferFormat() const { return m_depthBufferFormat; }
		D3D12_VIEWPORT				GetScreenViewport() const { return m_screenViewport; }
		DirectX::XMFLOAT4X4			GetOrientationTransform3D() const { return m_orientationTransform3D; }
		UINT						GetCurrentFrameIndex() const { return m_currentFrame; }

		CD3DX12_CPU_DESCRIPTOR_HANDLE GetRenderTargetView() const
		{
			return CD3DX12_CPU_DESCRIPTOR_HANDLE(m_rtvHeap->GetCPUDescriptorHandleForHeapStart(), m_currentFrame, m_rtvDescriptorSize);
		}
		CD3DX12_CPU_DESCRIPTOR_HANDLE GetDepthStencilView() const
		{
			return CD3DX12_CPU_DESCRIPTOR_HANDLE(m_dsvHeap->GetCPUDescriptorHandleForHeapStart());
		}

		static UINT BitsPerPixel(DXGI_FORMAT format);

		Microsoft::WRL::ComPtr<ID3D12DescriptorHeap>	m_cbvSrvUavHeap;
		volatile UINT									m_cbvSrvUavHeapAllocCount;
		Microsoft::WRL::ComPtr<ID3D12DescriptorHeap>	m_rtvHeap;
		volatile UINT									m_rtvHeapAllocCount;
		Microsoft::WRL::ComPtr<ID3D12DescriptorHeap>	m_dsvHeap;
		volatile UINT									m_dsvHeapAllocCount;

	private:
		void CreateWindowSizeDependentResources();
		void UpdateRenderTargetSize();
		void MoveToNextFrame();
		DXGI_MODE_ROTATION ComputeDisplayRotation();
		void GetHardwareAdapter(IDXGIAdapter1** ppAdapter);

		UINT											m_currentFrame;

		// Direct3D 对象。
		Microsoft::WRL::ComPtr<ID3D12Device>			m_d3dDevice;
		Microsoft::WRL::ComPtr<ID3D12Device5>			m_d3dDevice5;
		Microsoft::WRL::ComPtr<IDXGIFactory4>			m_dxgiFactory;
		Microsoft::WRL::ComPtr<IDXGISwapChain3>			m_swapChain;
		Microsoft::WRL::ComPtr<ID3D12Resource>			m_renderTargets[c_frameCount];
		Microsoft::WRL::ComPtr<ID3D12Resource>			m_depthStencil;
		Microsoft::WRL::ComPtr<ID3D12CommandQueue>		m_commandQueue;
		Microsoft::WRL::ComPtr<ID3D12CommandAllocator>	m_commandAllocators[c_frameCount];
		DXGI_FORMAT										m_backBufferFormat;
		DXGI_FORMAT										m_depthBufferFormat;
		D3D12_VIEWPORT									m_screenViewport;
		UINT											m_rtvDescriptorSize;
		bool											m_deviceRemoved;

		bool											m_isRayTracingSupport;

		// CPU/GPU 同步。
		Microsoft::WRL::ComPtr<ID3D12Fence>				m_fence;
		UINT64											m_fenceValues[c_frameCount];
		HANDLE											m_fenceEvent;

		// 对窗口的缓存引用。
		Windows::UI::Xaml::Controls::SwapChainPanel^	m_window;

		// 缓存的设备属性。
		Windows::Foundation::Size						m_d3dRenderTargetSize;
		Windows::Foundation::Size						m_outputSize;
		Windows::Foundation::Size						m_logicalSize;
		Windows::Graphics::Display::DisplayOrientations	m_nativeOrientation;
		Windows::Graphics::Display::DisplayOrientations	m_currentOrientation;
		float											m_dpi;

		float											m_compositionScaleX;
		float											m_compositionScaleY;

		// 这是将向应用传回的 DPI。它考虑了应用是否支持高分辨率屏幕。
		float											m_effectiveDpi;

		// 用于显示方向的转换。
		DirectX::XMFLOAT4X4								m_orientationTransform3D;

		bool											m_ResolutionChange = false;
	};
}
