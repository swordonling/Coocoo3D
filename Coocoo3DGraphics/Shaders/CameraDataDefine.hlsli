//这个文件仅定义Camera Data的结构
//开辟新渲染管线时需要使用它
//It just define Camera Data Struct.
//Please use it in new render pipeline.
#define CAMERA_DATA_DEFINE \
	float4x4 g_mWorldToProj;\
	float4x4 g_mProjToWorld;\
	float3   g_vCamPos;\
	float g_aspectRatio;\
	float g_time;\
	float g_deltaTime;