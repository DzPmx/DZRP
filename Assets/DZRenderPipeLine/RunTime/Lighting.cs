using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
public class Lighting
{
    private const int maxDirLightCount = 4;
    private const int maxOtherLightCount = 64;
    /// 平行灯ID
    private static int
        dirLightCountID = Shader.PropertyToID("_DirectionalLightCount"),
        dirLightColID = Shader.PropertyToID("_DirectionalLightCols"),
        dirLightDirID = Shader.PropertyToID("_DirectionalLightDirs"),
        dirLightShadowDataID = Shader.PropertyToID("_DirectionalLightShadowData");

    ///其他灯光ID
    private static int
        otherLightCountID = Shader.PropertyToID("_OtherLightCount"),
        otherLightColID = Shader.PropertyToID("_OtherLightCols"),
        otherLightPosID = Shader.PropertyToID("_OtherLightPos"),
        otherLightDirID = Shader.PropertyToID("_OtherLightDir"),
        ohterLightSpotAnglesID = Shader.PropertyToID("_OtherLightSpotAngles"),
        otherLightShadowDataID = Shader.PropertyToID("_OtherLightShadowData");

    ///平行灯
    private static Vector4[]
        dirLightCols = new Vector4[maxDirLightCount],
        dirLightDirs = new Vector4[maxDirLightCount],
        dirLightShadowData = new Vector4[maxDirLightCount]; //灯光阴影信息，x：阴影强度 y:级联阴影的数量*有阴影灯光的数量 z:Light组件里的normalBias

    ///其他灯光
    private static Vector4[]
        otherLightCols = new Vector4[maxOtherLightCount],
        otherLightPos = new Vector4[maxOtherLightCount],
        otherLightDirs = new Vector4[maxOtherLightCount],
        otherLightSpotAngles = new Vector4[maxOtherLightCount],
        otherLightShadowData = new Vector4[maxOtherLightCount];

    private const string bufferName = "Lighting";
    private CommandBuffer buffer = new CommandBuffer{ name =bufferName };
    private CullingResults cullingResults;

    private Shadows shadows = new Shadows();



    ///<summary>
    ///主方法  所有与光源相关以及阴影相关在这里实现
    ///</summary>
    public void Setup(ScriptableRenderContext context,CullingResults cullingResults,ShadowSettings shadowSettings,bool useLightsPerObject)
    {
        this.cullingResults = cullingResults;
        buffer.BeginSample(bufferName);
        shadows.Setup(context,cullingResults,shadowSettings);
        SetupLights(useLightsPerObject);
        shadows.Render();
        buffer.EndSample(bufferName);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }


    /// <summary>
    /// 设置单例平行灯信息进入灯光颜色、方向、阴影信息的数组
    /// </summary>
    void SetupDirectionalLight(int index,int visibleIndex,ref VisibleLight visibleLight)
    {
        dirLightCols[index] = visibleLight.finalColor;
        dirLightDirs[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        dirLightShadowData[index]=shadows.ReserveDirectionalShadows(visibleLight.light,visibleIndex);
    }

    void SetupPointsLight(int index, int visibleIndex,ref VisibleLight visibleLight)
    {
        otherLightCols[index] = visibleLight.finalColor;
        Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
        position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
        otherLightPos[index] = position;
        otherLightSpotAngles[index] = new Vector4(0f, 1f);
        Light light = visibleLight.light;
        otherLightShadowData[index] = shadows.ReserveOtherShadows(light, visibleIndex);
        

    }
    
    void SetupSpotLight(int index, int visibleIndex,ref VisibleLight visibleLight)
    {
        otherLightCols[index] = visibleLight.finalColor;
        Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
        position.w = 1f / Mathf.Max(visibleLight.range*visibleLight.range, 0.00001f);
        otherLightPos[index] = position;
        otherLightDirs[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        
        Light light = visibleLight.light;
        float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
        float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
        float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);
        otherLightSpotAngles[index] = new Vector4(angleRangeInv, -outerCos * angleRangeInv);
        otherLightShadowData[index] = shadows.ReserveOtherShadows(light, visibleIndex);

    }
    
    /// <summary>
    /// 同步单例灯光信息进入GPU 灯光数量、方向、颜色、阴影信息
    /// </summary>
    void SetupLights(bool useLightsPerObject)
    {
        NativeArray<int> indexMap = useLightsPerObject ? cullingResults.GetLightIndexMap(Allocator.Temp) : default;
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
        int dirLightCount = 0,otherLightCount = 0;
        int i ;
        for (i=0; i < visibleLights.Length; i++)
        {
            int newIndex = -1;
            VisibleLight visibleLight = visibleLights[i];
            switch (visibleLight.lightType)
            {
                case LightType.Directional:
                    if (dirLightCount<maxDirLightCount)
                    {
                        SetupDirectionalLight(dirLightCount++,i,ref visibleLight);
                    }
                    break;
                case LightType.Point:
                    if (otherLightCount<maxOtherLightCount)
                    {
                        newIndex = otherLightCount;
                        SetupPointsLight(otherLightCount++,i,ref visibleLight);
                    }
                    break;
                case LightType.Spot:
                    if (otherLightCount<maxOtherLightCount)
                    {
                        newIndex = otherLightCount;
                        SetupSpotLight(otherLightCount++,i,ref visibleLight);
                    }
                    break;
            }
            
        }
        //消除除了Point和Spot外其他所有不可见灯光
        if (useLightsPerObject)
        {
            for (;i < indexMap.Length; i++)
            {
                indexMap[i] = -1;
            }
        }
        cullingResults.SetLightIndexMap(indexMap);
        indexMap.Dispose();
        
        buffer.SetGlobalInt(dirLightCountID,dirLightCount);
        if (dirLightCount>0)
        {
            buffer.SetGlobalVectorArray(dirLightColID,dirLightCols);
            buffer.SetGlobalVectorArray(dirLightDirID,dirLightDirs);
            buffer.SetGlobalVectorArray(dirLightShadowDataID,dirLightShadowData);
        }
        buffer.SetGlobalInt(otherLightCountID,otherLightCount);
        if (otherLightCount>0)
        {
            buffer.SetGlobalVectorArray(otherLightColID,otherLightCols);
            buffer.SetGlobalVectorArray(otherLightPosID,otherLightPos);
            buffer.SetGlobalVectorArray(otherLightDirID,otherLightDirs);
            buffer.SetGlobalVectorArray(ohterLightSpotAnglesID,otherLightSpotAngles);
            buffer.SetGlobalVectorArray(otherLightShadowDataID,otherLightShadowData);
        }
    }

    public void CleanUp()
    {
        shadows.CleanUp();
    }
    
}
