#include "../CameraDataDefine.hlsli"
struct LightInfo
{
	float3 LightDir;
	uint LightType;
	float4 LightColor;
	float4x4 LightSpaceMatrix;
};
cbuffer cb0 : register(b0)
{
	CAMERA_DATA_DEFINE//is a macro
		uint g_enableAO;
	uint g_enableShadow;
	uint g_quality;
};
struct VSNearTriangleIndexs
{
	int indexs[16];
};
StructuredBuffer<VSNearTriangleIndexs> NTIndex :register(t0);

struct VSSkinnedIn
{
	float4 Pos;
	float3 Norm;
	float2 Tex;
	float3 Tan;
	float EdgeScale;
};
StructuredBuffer<VSSkinnedIn> vertex :register(t1);


Texture2D SceneNormal :register(t2);
Texture2D SceneDepth :register(t3);
SamplerState s0 : register(s0);

void GetPoints(int index, out float3 p1, out float3 p2, out float3 p3)
{
	p1 = vertex[index * 3].Pos.xyz;
	p2 = vertex[index * 3 + 1].Pos.xyz;
	p3 = vertex[index * 3 + 2].Pos.xyz;
}

float GetArea(float3 p1, float3 p2, float3 p3)
{
	float a = distance(p1, p2);
	float b = distance(p2, p3);
	float c = distance(p3, p1);
	return 0.25 * sqrt((a + b + c) * (a + b - c) * (a - b + c) * (-a + b + c));
}

float GetVolume(float3 pos, float3 p1, float3 p2, float3 p3)
{
	float3 a = p1 - pos;
	float3 b = p2 - pos;
	float3 c = p3 - pos;
	return dot(cross(a, b), c) / 6;
}

float GetChance(float3 pos, float3 p1, float3 p2, float3 p3)
{
	float3 a = p1 - pos;
	float3 b = p2 - pos;
	float3 c = p3 - pos;

	float3 a1 = p1 - p2;
	float3 b1 = p2 - p3;
	float3 c1 = p3 - p1;

	float3 ax = normalize(cross(cross(a, a1), a));
	float3 bx = normalize(cross(cross(b, b1), b));
	float3 cx = normalize(cross(cross(c, c1), c));

	float3 ay = normalize(cross(cross(a, -c1), a));
	float3 by = normalize(cross(cross(b, -a1), b));
	float3 cy = normalize(cross(cross(b, -b1), b));
	return (acos(dot(ax, ay)) + acos(dot(bx, by)) + acos(dot(cx, cy)) - 3.1415926535897932385) / 6.2831853071795865;
}

struct PSSkinnedIn
{
	float4 Pos	: SV_POSITION;		//Position
	float4 wPos	: POSITION;			//world space Pos
	float3 Norm : NORMAL;			//Normal
	float2 TexCoord	: TEXCOORD;		//Texture coordinate
	float3 Tangent : TANGENT;		//Normalized Tangent vector
	uint PrimitiveID:SV_PrimitiveID;
};
float4 main(PSSkinnedIn input) : SV_TARGET
{
	float4 pos1 = mul(input.wPos,g_mWorldToProj);
	float2 texCoords;
	texCoords.x = 0.5f + (pos1.x / pos1.w * 0.5f);
	texCoords.y = 0.5f - (pos1.y / pos1.w * 0.5f);
	float3 Norm = SceneNormal.Sample(s0, texCoords).rgb;
	float depth = SceneDepth.Sample(s0, texCoords).r;



	float4 world = mul(float4(pos1.xy / pos1.w, depth, 1), g_mProjToWorld);
	world.xyz /= world.w;


	uint pIndex = input.PrimitiveID;

	VSNearTriangleIndexs ntis = NTIndex[pIndex];
	float AOFactory = 0.0f;

	{
		int nti = pIndex;
		float3 p1;
		float3 p2;
		float3 p3;
		GetPoints(nti, p1, p2, p3);
		float volume = GetVolume(world.xyz, p1, p2, p3);
		float volume2 = GetArea(p1, p2, p3) * 32 / 3;
		AOFactory += /*saturate((volume2 - volume) **/  saturate(GetChance(world.xyz, p1, p2, p3));
	}

	for (int i = 0; i < 16; i++)
	{
		int nti = ntis.indexs[i];
		if (nti == -1)break;
		float3 p1;
		float3 p2;
		float3 p3;
		GetPoints(nti, p1, p2, p3);
		float volume = GetVolume(world.xyz, p1, p2, p3);
		float volume2 = GetArea(p1, p2, p3) * 32 / 3;
		AOFactory += /*saturate((volume2 - volume) / volume2) **/ saturate(GetChance(world.xyz, p1, p2, p3));
	}

	return float4(saturate(float3(AOFactory, AOFactory, AOFactory)),1);
	//return float4(world.xyz / 32,1);
	//return float4(input.wPos.xyz / 32,1);
	//return float4(SceneNormal.Sample(s0, texCoords).xyz,1);
}