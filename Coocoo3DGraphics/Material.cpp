#include "pch.h"
#include "Material.h"
using namespace Coocoo3DGraphics;

Material ^ Material::Load(PObject ^ pobject)
{
	Material^ material = ref new Material();
	material->Reload(pobject);
	return material;
}

void Material::Reload(PObject ^ pobject)
{
	m_pObject = pobject;
	for (int i = 0; i < c_reference_max; i++)
		references[i] = nullptr;
}

void Material::SetTexture(Texture2D ^ tex, int slot)
{
	references[slot] = tex;
}

Texture2D ^ Coocoo3DGraphics::Material::GetTexture(int slot)
{
	return references[slot];
}
