#pragma once
#include "PObject.h"
#include "Texture2D.h"
namespace Coocoo3DGraphics
{

	public ref class Material sealed
	{
	public:
		static Material^ Load(PObject^ pobject);
		void Reload(PObject^ pobject);
		void SetTexture(Texture2D^ tex, int slot);
		Texture2D^ GetTexture(int slot);
		property PObject^ m_pObject;
		property CullMode cullMode;
	internal:
		static const int c_reference_max = 1;
		Texture2D^ references[c_reference_max] = {};
	};
}