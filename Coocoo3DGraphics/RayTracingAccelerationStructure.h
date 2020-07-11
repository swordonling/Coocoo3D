#pragma once
#include "DeviceResources.h"
#include "MMDMesh.h"
namespace Coocoo3DGraphics
{
	public ref class RayTracingAccelerationStructure sealed
	{
	public:
		void Reload(DeviceResources^ deviceResources, UINT size, UINT instanceCount);
		void AddMeshToThisFrameRayTracingList(MMDMesh^ mesh);
	internal:

		Microsoft::WRL::ComPtr<ID3D12Resource> m_topLevelAccelerationStructure[c_frameCount];
		UINT m_topLevelAccelerationStructureSize[c_frameCount] = {};
		std::vector< Microsoft::WRL::ComPtr<ID3D12Resource>>m_bottomLevelASs[c_frameCount];

		Microsoft::WRL::ComPtr<ID3D12Resource> instanceDescs[c_frameCount];
		Microsoft::WRL::ComPtr<ID3D12Resource> m_scratchResource[c_frameCount];
		int lastUpdateIndex;
		std::vector<MMDMesh^> m_rayTracingMeshes;
	};
}

