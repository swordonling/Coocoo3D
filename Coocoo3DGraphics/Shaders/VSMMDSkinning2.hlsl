#define MAX_BONE_MATRICES 1024
cbuffer cbAnimMatrices : register(b0)
{
	float4x4 g_mConstBoneWorld[MAX_BONE_MATRICES];
};
cbuffer cb1 : register(b1)
{
	float4x4 g_mWorld;
};
cbuffer cb3 : register(b3)
{
	float4x4 g_mWorldToProj;
	float3   g_vCamPos;
	float g_aspectRatio;
	float _Time;
	float _DeltaTime;
};

struct VSSkinnedIn
{
	float3 Pos	: POSITION;			//Position
	float4 Weights : WEIGHTS;		//Bone weights
	uint4  Bones : BONES;			//Bone indices
	float3 Norm : NORMAL;			//Normal
	float2 Tex	: TEXCOORD;		    //Texture coordinate
	float3 Tan : TANGENT;		    //Normalized Tangent vector
	float EdgeScale : EDGESCALE;
};

struct PSSkinnedIn
{
	float4 Pos	: SV_POSITION;		//Position
	float3 Norm : NORMAL;			//Normal
	float2 Tex	: TEXCOORD;		    //Texture coordinate
	float3 Tangent : TANGENT;		//Normalized Tangent vector
	float EdgeScale : EDGESCALE;
};


struct SkinnedInfo
{
	float4 Pos;
	float3 Norm;
	float3 Tan;
};

matrix FetchBoneTransform(uint iBone)
{
	matrix mret;
	mret = g_mConstBoneWorld[iBone];
	return mret;
}

SkinnedInfo SkinVert(VSSkinnedIn Input)
{
	SkinnedInfo Output = (SkinnedInfo)0;

	float4 Pos = float4(Input.Pos, 1);
	float3 Norm = Input.Norm;
	float3 Tan = Input.Tan;

	//Bone0
	uint iBone = Input.Bones.x;
	float fWeight = Input.Weights.x;
	matrix m = FetchBoneTransform(iBone);
	Output.Pos += fWeight * mul(Pos, m);
	Output.Norm += fWeight * mul(float4(Norm,0), m).xyz;
	Output.Tan += fWeight * mul(float4(Tan,0), m).xyz;

	//Bone1
	iBone = Input.Bones.y;
	fWeight = Input.Weights.y;
	m = FetchBoneTransform(iBone);
	Output.Pos += fWeight * mul(Pos, m);
	Output.Norm += fWeight * mul(float4(Norm, 0), m).xyz;
	Output.Tan += fWeight * mul(float4(Tan, 0), m).xyz;

	//Bone2
	iBone = Input.Bones.z;
	fWeight = Input.Weights.z;
	m = FetchBoneTransform(iBone);
	Output.Pos += fWeight * mul(Pos, m);
	Output.Norm += fWeight * mul(float4(Norm,0), m).xyz;
	Output.Tan += fWeight * mul(float4(Tan,0), m).xyz;

	//Bone3
	iBone = Input.Bones.w;
	fWeight = Input.Weights.w;
	m = FetchBoneTransform(iBone);
	Output.Pos += fWeight * mul(Pos, m);
	Output.Norm += fWeight * mul(float4(Norm, 0), m).xyz;
	Output.Tan += fWeight * mul(float4(Tan, 0), m).xyz;

	return Output;
}


PSSkinnedIn main(VSSkinnedIn input)
{
	PSSkinnedIn output;

	SkinnedInfo vSkinned = SkinVert(input);
	output.Pos = mul(vSkinned.Pos, g_mWorld);
	output.Norm = normalize(mul(vSkinned.Norm, (float3x3)g_mWorld));
	output.Tangent = normalize(mul(vSkinned.Tan, (float3x3)g_mWorld));
	output.Tex = input.Tex;

	return output;
}