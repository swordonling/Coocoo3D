#pragma once
#include "DeviceResources.h"
#include "VertexShader.h"
#include "PixelShader.h"
#include "GeometryShader.h"
namespace Coocoo3DGraphics
{
	public enum struct CullMode
	{
		none = 0,
		front = 1,
		back = 2,
	};
	public enum struct BlendState
	{
		none = 0,
		alpha = 1,
	};
	public enum struct PObjectType
	{
		mmd = 0,
		ui = 1,
	};
	public ref class PObject sealed
	{
	public:
		property bool Ready;
		property Platform::Object^ LoadTask;
		property Platform::String^ Path;
		void Reload(DeviceResources^ deviceResources, PObjectType type, VertexShader^ vertexShader, GeometryShader^ geometryShader, PixelShader^ pixelShader);
		void Reload(PObject^ pObject);
	internal:
		PixelShader^ m_pixelShader;
		GeometryShader^ m_geometryShader;
		Microsoft::WRL::ComPtr<ID3D11BlendState> m_blendStateAlpha;
		Microsoft::WRL::ComPtr<ID3D11BlendState> m_blendStateOqaque;
		Microsoft::WRL::ComPtr<ID3D11RasterizerState>			m_RasterizerStateCullBack;
		Microsoft::WRL::ComPtr<ID3D11RasterizerState>			m_RasterizerStateCullFront;
		Microsoft::WRL::ComPtr<ID3D11RasterizerState>			m_RasterizerStateCullNone;
		Microsoft::WRL::ComPtr<ID3D11VertexShader> m_vertexShader;
		Microsoft::WRL::ComPtr<ID3D11InputLayout> m_inputLayout;
	private:
		void Other1(DeviceResources^ deviceResources);
	};
}
