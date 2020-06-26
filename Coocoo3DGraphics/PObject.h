#pragma once
#include "DeviceResources.h"
#include "VertexShader.h"
#include "PixelShader.h"
#include "GeometryShader.h"
namespace Coocoo3DGraphics
{
	public ref class PObject sealed
	{
	public:
		property bool Ready;
		property Platform::Object^ LoadTask;
		property Platform::String^ Path;
		void Reload(DeviceResources^ deviceResources,VertexShader^ vertexShader, PixelShader^ pixelShader);
		void Reload(DeviceResources^ deviceResources,VertexShader^ vertexShader, GeometryShader^ geometryShader, PixelShader^ pixelShader);
		void Reload(PObject^ pObject);
		property VertexShader^ m_vertexShader;
		property PixelShader^ m_pixelShader;
		property GeometryShader^ m_geometryShader;
	};
}
