//����ļ�������Camera Data�Ľṹ
//��������Ⱦ����ʱ��Ҫʹ����
//It just define Camera Data Struct.
//Please use it in new render pipeline.
#define CAMERA_DATA_DEFINE \
	float4x4 g_mWorldToProj;\
	float4x4 g_mProjToWorld;\
	float3   g_vCamPos;\
	float g_aspectRatio;\
	float g_time;\
	float g_deltaTime;\
	uint2 g_camera_randomValue;\
	float4 g_camera_preserved2[6];