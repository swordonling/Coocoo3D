#pragma once
#include "MMDEnum.h"
namespace Coocoo3DNativeInteroperable
{
	using namespace Windows::Foundation::Numerics;
	public ref class NMMD_MaterialDesc sealed
	{
	public:
		property Platform::String^ Name;
		property Platform::String^ NameEN;
		property float4 DiffuseColor;
		property float4 SpecularColor;
		property float3 AmbientColor;
		property NMMDE_DrawFlag DrawFlags;
		property float4 EdgeColor;
		property float EdgeScale;
		property int TextureIndex;
		property int secondTextureIndex;
		property byte secondTextureType;
		property bool UseToon;
		property int ToonIndex;
		property Platform::String^ Meta;
		property int TriangeIndexStartNum;
		property int TriangeIndexNum;
	};
}