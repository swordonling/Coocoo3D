#pragma once
namespace Coocoo3DGraphics
{
	public interface class ITexture
	{
	public:
		void ReleaseUploadHeapResource();
	};
	public interface class IRenderTexture
	{

	};
	struct ImageMipsData
	{
		UINT width;
		UINT height;
	};
}