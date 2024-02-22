using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{
    private const string bufferName = "Shadows";
    private CommandBuffer buffer = new CommandBuffer { name = bufferName };
    private ScriptableRenderContext context;
    private CullingResults cullingResults;
    private ShadowSettings settings;
    private bool useShadowMask;
    private Vector4 atlasSize;

    ///阴影过滤模式
    private static string[] directionalFilterKeywords =
    {
        "_DIRECTIONAL_PCF3",
        "_DIRECTIONAL_PCF5",
        "_DIRECTIONAL_PCF7",
    };

    private static string[] otherFilterKeywords =
    {
        "_OTHER_PCF3",
        "_OTHER_PCF5",
        "_OTHER_PCF7",
    };

    ///级联边界的混合模式 
    private static string[] cascadeBlendKeywords =
    {
        "_CASCADE_BLEND_SOFT",
        "_CASCADE_BLEND_DITHER"
    };
    /// ShadowMask关键字
    private static string[] shadowMaskKeywords =
    {
        "_SHADOW_MASK_ALWAYS",
        "_SHADOW_MASK_DISTANCE"
    };

    private static int
        dirShadowAtlasID = Shader.PropertyToID("_DirectionalShadowAtlas"),
        dirShadowMatricesID = Shader.PropertyToID("_DirectionalShadowMatrices"),
        otherShadowAtlasID = Shader.PropertyToID("_OtherShadowAtlas"),
        otherShadowMatricesID = Shader.PropertyToID("_OtherShadowMatrices"),
        otherShadowTilesID=Shader.PropertyToID("_OtherShadowTiles"),
        cascadeCountID = Shader.PropertyToID("_CascadeCount"),
        cascadeCullingSpheresID = Shader.PropertyToID("_CascadeCullingSpheres"),
        cascadeDataID = Shader.PropertyToID("_CascadeData"),
        shadowAtlasSizeID = Shader.PropertyToID("_ShadowAtlasSize"),
        shadowDistaneFadeID = Shader.PropertyToID("_ShadowDistanceFade"),
        shadowPancakingID = Shader.PropertyToID("_ShadowPancaking");

    ///阴影矩阵
    private static Matrix4x4[]
        dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount * maxCascades],
        otherShadowMatrces = new Matrix4x4[maxShadowedOtherLightCount];
    
    private static Vector4[]
        cascadeCullingSpheres = new Vector4[maxCascades],
        cascadeData = new Vector4[maxCascades],
        otherShadowTiles = new Vector4[maxShadowedOtherLightCount];
        
        

    ///最大支持一盏平行光的阴影
    private const int maxShadowedDirectionalLightCount = 4, maxShadowedOtherLightCount = 16;
    private const int maxCascades = 4;

    /// <summary>
    /// 用于获取当前支持阴影的方向光源的信息 灯光索引,Bias值，近平面偏移 
    /// </summary>
    struct ShadowedDirectionalLight
    {
        public int visibleLightIndex;
        public float slopeScaleBias;
        public float nearPlaneOffset;
    }

    struct ShadowedOtherLight
    {
        public int visibleLightIndex;
        public float slopeScaleBias;
        public float normalBias;
        public bool isPoint;
    }
    
    private ShadowedDirectionalLight[] shadowedDirectionalLights =
        new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];

    private ShadowedOtherLight[] shadowedOtherLights = new ShadowedOtherLight[maxShadowedOtherLightCount];
    
    //当前已配置完毕的平行光
    private int shadowedDirectionalLightCount, shadowedOtherLightCount;


        

    /// <summary>
    /// 阴影字段初始化
    /// </summary>
    public void Setup(ScriptableRenderContext context,CullingResults cullingResults,ShadowSettings shadowSettings)
    {
        this.context = context;
        this.cullingResults = cullingResults;
        this.settings = shadowSettings;
        this.shadowedDirectionalLightCount=this.shadowedOtherLightCount = 0;
        this.useShadowMask = false;
    }

    void ExcuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
    
    /// <summary>
    /// 判断平行灯是否有阴影信息有就存储信息x：:Light组件里的阴影强度 y:级联阴影的数量*有阴影灯光的数量 z:Light组件里的normalBias
    /// 判断是否开启ShadowMap的使用
    /// </summary>
    public Vector4 ReserveDirectionalShadows(Light light,int visibleLightIndex)
    {
        if (shadowedDirectionalLightCount<maxShadowedDirectionalLightCount &&
            light.shadows is not LightShadows.None && light.shadowStrength>0f
            )
        {
            float maskChannel = -1f;
            LightBakingOutput lightBaking = light.bakingOutput;
            if (lightBaking.lightmapBakeType==LightmapBakeType.Mixed && lightBaking.mixedLightingMode==MixedLightingMode.Shadowmask )
            {
                useShadowMask = true;
                maskChannel = lightBaking.occlusionMaskChannel;
            }

            if (!cullingResults.GetShadowCasterBounds(visibleLightIndex,out Bounds b))
            {
                return new Vector4(-light.shadowStrength, 0.0f, 0.0f,maskChannel);
            }
            shadowedDirectionalLights[shadowedDirectionalLightCount] = new ShadowedDirectionalLight
                { visibleLightIndex = visibleLightIndex,slopeScaleBias = light.shadowBias,nearPlaneOffset = light.shadowNearPlane};
            return new Vector4(light.shadowStrength, settings.directional.cascadeCount*shadowedDirectionalLightCount++,light.shadowNormalBias,maskChannel);
        }
        return new Vector4(0f,0f,0f,-1f);
    }

    public Vector4 ReserveOtherShadows(Light light, int visibleLightIndex)
    {
        if (light.shadows == LightShadows.None || light.shadowStrength <= 0f)
        {
            return new Vector4(0f, 0f, 0f, -1f);
        }

        float maskChannel = -1f;
        LightBakingOutput lightBaking = light.bakingOutput;
        if (lightBaking.lightmapBakeType == LightmapBakeType.Mixed &&
            lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
        {
            useShadowMask = true;
            maskChannel = lightBaking.occlusionMaskChannel;
        }

        bool isPoint = light.type == LightType.Point;
        int newLightCount = shadowedOtherLightCount + (isPoint ? 6 : 1);
        if (newLightCount >= maxShadowedOtherLightCount ||
            !cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
        {
            return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);
        }

        shadowedOtherLights[shadowedOtherLightCount] = new ShadowedOtherLight()
        {
            visibleLightIndex = visibleLightIndex,
            slopeScaleBias = light.shadowBias,
            normalBias = light.shadowNormalBias,
            isPoint = isPoint,
        };
        Vector4 data=new Vector4(
            light.shadowStrength, shadowedOtherLightCount++,
            isPoint? 1f :0f, maskChannel);
        shadowedOtherLightCount = newLightCount;
        return data;
    }

    /// <summary>
    /// 渲染阴影 有阴影时，渲染，无阴影时仅生成一个1x1的Shadowmap
    /// </summary>
    public void Render()
    {
        if (shadowedDirectionalLightCount > 0)
        {
            RenderDirectionalShadows();
        }
        else //在无阴影时，生成1x1的texture 避免WebGL 2.0 的bug
        {
            buffer.GetTemporaryRT(dirShadowAtlasID,1,1,32,FilterMode.Bilinear,RenderTextureFormat.Shadowmap);
        }

        if (shadowedOtherLightCount>0)
        {
            //Debug.Log(shadowedOtherLightCount);
            RenderOtherShadows();
        }
        else
        {
            buffer.SetGlobalTexture(otherShadowAtlasID,dirShadowAtlasID);
        }
        
        buffer.BeginSample(bufferName);
        SetKeywords(shadowMaskKeywords, useShadowMask ? QualitySettings.shadowmaskMode==ShadowmaskMode.Shadowmask? 0 : 1 :-1);
        buffer.SetGlobalInt(cascadeCountID,shadowedDirectionalLightCount>0? settings.directional.cascadeCount:0);
        float f = 1f - settings.directional.cascadeFade;
        buffer.SetGlobalVector(shadowDistaneFadeID,new Vector4(1f/settings.maxDistance,1f/settings.distanceFade,1f/(1f-f*f)));
        buffer.SetGlobalVector(shadowAtlasSizeID,this.atlasSize);//图集大小传入Shader
        buffer.EndSample(bufferName);
        ExcuteBuffer();
    }
    /// <summary>
    /// 渲染所有直射光的阴影到ShadowAtlas
    /// </summary>
    void RenderDirectionalShadows()
    {
        int atlasSize = (int)settings.directional.atlasSize;
        this.atlasSize.x = atlasSize;
        this.atlasSize.y = 1f / atlasSize;
        buffer.GetTemporaryRT(dirShadowAtlasID,atlasSize,atlasSize,32,FilterMode.Bilinear,RenderTextureFormat.Shadowmap);//得到RT
        buffer.SetRenderTarget(dirShadowAtlasID,RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store);//设置RT为渲染目标并在渲染完后存储RT
        buffer.ClearRenderTarget(true,false,Color.clear);//清除RT内容
        buffer.SetGlobalFloat(shadowPancakingID,1f);
        buffer.BeginSample(bufferName);
        ExcuteBuffer();
        int tiles = shadowedDirectionalLightCount * settings.directional.cascadeCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;//split成1/2/4
        int tileSize = atlasSize / split;
        for (int i = 0; i <shadowedDirectionalLightCount; i++)  //针对每一个带阴影的方向光都渲染到Atlas里
        {
            RenderDirectionalShadows(i,split,tileSize);
        }
        buffer.SetGlobalVectorArray(cascadeCullingSpheresID,cascadeCullingSpheres);//级联阴影剔除球
        buffer.SetGlobalVectorArray(cascadeDataID,cascadeData);
        buffer.SetGlobalMatrixArray(dirShadowMatricesID,dirShadowMatrices);//阴影空间的变换矩阵
        SetKeywords(directionalFilterKeywords,(int) settings.directional.filterMode-1);//联动面板设置阴影
        SetKeywords(cascadeBlendKeywords,(int)settings.directional.cascadeBlend-1);//联动面板设置阴影
       
        buffer.EndSample(bufferName);
        ExcuteBuffer();
    }
    
    
    void RenderOtherShadows()
    {
        int atlasSize = (int)settings.other.atlasSize;
        this.atlasSize.z = atlasSize;
        this.atlasSize.w = 1f / atlasSize;
        buffer.GetTemporaryRT(otherShadowAtlasID,atlasSize,atlasSize,32,FilterMode.Bilinear,RenderTextureFormat.Shadowmap);//得到RT
        buffer.SetRenderTarget(otherShadowAtlasID,RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store);//设置RT为渲染目标并在渲染完后存储RT
        buffer.ClearRenderTarget(true,false,Color.clear);//清除RT内容
        buffer.SetGlobalFloat(shadowPancakingID,0f);
        buffer.BeginSample(bufferName);
        ExcuteBuffer();
        int tiles = shadowedOtherLightCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;//split成1/2/4
        int tileSize = atlasSize / split;
        for (int i = 0; i <shadowedOtherLightCount;)  
        {
            if (shadowedOtherLights[i].isPoint)
            {
                RenderPointShadows(i, split, tileSize);
                i += 6;
            }
            else
            {
                RenderSpotShadows(i,split,tileSize);
                i += 1;
            }
        }
        buffer.SetGlobalMatrixArray(otherShadowMatricesID,otherShadowMatrces);//阴影空间的变换矩阵
        buffer.SetGlobalVectorArray(otherShadowTilesID,otherShadowTiles);
        SetKeywords(otherFilterKeywords,(int) settings.other.filterMode-1);//联动面板设置阴影
        buffer.EndSample(bufferName);
        ExcuteBuffer();
    }
    
    /// <summary>
    /// 渲染单个光源阴影到ShadowAtlas
    /// </summary>
    void RenderDirectionalShadows(int index,int split,int tileSize)
    {
        ShadowedDirectionalLight light = shadowedDirectionalLights[index];
        var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex,BatchCullingProjectionType.Orthographic);
        int cascadeCount = settings.directional.cascadeCount;
        int tileOffset = index * cascadeCount;
        Vector3 ratios = settings.directional.CascaeRatios; //级联阴影每级的比率
        float cullingFactor = Mathf.Max(0f, 0.8f - settings.directional.cascadeFade);//剔除系数
        float tileScale = 1f / split;
        for (int i = 0; i < cascadeCount; i++)
        {
            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives
            (light.visibleLightIndex, i, cascadeCount, ratios, tileSize, light.nearPlaneOffset, out Matrix4x4 viewMatrix,
                out Matrix4x4 projMatrix, out ShadowSplitData splitData);//计算方向光的视图和投影矩阵以及阴影分割数据。
            splitData.shadowCascadeBlendCullingFactor = cullingFactor;
            shadowSettings.splitData = splitData;
            if (index==0)
            {
                SetCascadeData(i,splitData.cullingSphere,tileSize);
            }
            int tileIndex = tileOffset + i;
            dirShadowMatrices[tileIndex] = convertToAtlasMatrix(projMatrix * viewMatrix,SetTileViewport(tileIndex,split,tileSize),split,tileScale);   //得到阴影空间变换矩阵并设置视口
            buffer.SetViewProjectionMatrices(viewMatrix,projMatrix);
            buffer.SetGlobalDepthBias(0,light.slopeScaleBias); //绘制阴影前设置Light组件里的Depth Bias到全局深度偏移
            ExcuteBuffer();
            context.DrawShadows(ref shadowSettings);//绘制阴影
            buffer.SetGlobalDepthBias(0f,0f);//绘制完阴影后Depth Bias要归零
        }
    }
    
    void RenderSpotShadows(int index,int split,int tileSize)
    {
        ShadowedOtherLight light = shadowedOtherLights[index];
        var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex,
            BatchCullingProjectionType.Perspective);
        cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(light.visibleLightIndex, out Matrix4x4 viewMatrix,
            out Matrix4x4 projMatrix, out ShadowSplitData shadowSplitData);
        shadowSettings.splitData = shadowSplitData;
        float texelSize = 2f / (tileSize * projMatrix.m00);
        float filterSize = texelSize * ((float)settings.other.filterMode + 1);
        float bias = light.normalBias * filterSize * 1.4142136f;
        Vector2 offset = SetTileViewport(index, split, tileSize);
        float tileScale = 1f / split;
        SetOtherTileData(index,offset,1f/split,bias);
        otherShadowMatrces[index] =
            convertToAtlasMatrix(projMatrix * viewMatrix, offset, split,tileScale);
        buffer.SetViewProjectionMatrices(viewMatrix,projMatrix);
        buffer.SetGlobalDepthBias(0f,light.slopeScaleBias);
        ExcuteBuffer();
        context.DrawShadows(ref shadowSettings);
        buffer.SetGlobalDepthBias(0f,0f);
    }
    
    void RenderPointShadows(int index,int split,int tileSize)
    {
        ShadowedOtherLight light = shadowedOtherLights[index];
        var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex,
            BatchCullingProjectionType.Perspective);
        float texelSize = 2f / tileSize;
        float filterSize = texelSize * ((float)settings.other.filterMode + 1);
        float bias = light.normalBias * filterSize * 1.4142136f;
        float tileScale = 1f / split;
        float fovBias = Mathf.Atan(1f + bias + filterSize) * Mathf.Rad2Deg * 2f - 90f;
        for (int i = 0; i < 6; i++)
        {
            cullingResults.ComputePointShadowMatricesAndCullingPrimitives(light.visibleLightIndex, (CubemapFace)i,
                fovBias, out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix,
                out ShadowSplitData shadowSplitData);
            viewMatrix.m11 = -viewMatrix.m11;
            viewMatrix.m12 = -viewMatrix.m12;
            viewMatrix.m13 = -viewMatrix.m13;
            shadowSettings.splitData = shadowSplitData;
            int tileIndex = index + i;
            Vector2 offset = SetTileViewport(tileIndex, split, tileSize);
            SetOtherTileData(tileIndex, offset, 1f / split, bias);
            otherShadowMatrces[tileIndex] =
                convertToAtlasMatrix(projMatrix * viewMatrix, offset, split, tileScale);
            buffer.SetViewProjectionMatrices(viewMatrix, projMatrix);
            buffer.SetGlobalDepthBias(0f, light.slopeScaleBias);
            ExcuteBuffer();
            context.DrawShadows(ref shadowSettings);
            buffer.SetGlobalDepthBias(0f, 0f);
        }
    }

    
    
    /// <summary>
    /// 把方向光的VP矩阵（-1~1）转换到ShdowAtlas的纹理空间（0-1）并偏移矩阵的位置
    /// </summary>
    /// <param name="m">从cullingresult获得的vp矩阵</param>
    Matrix4x4 convertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split,float scale)
    {
        //是否反转ZBuffer
        if (SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }
        
        m.m00 = (0.5f * (m.m00 + m.m30)+offset.x*m.m30)*scale;
        m.m01 = (0.5f * (m.m01 + m.m31)+offset.x*m.m31)*scale;
        m.m02 = (0.5f * (m.m02 + m.m32)+offset.x*m.m32)*scale;
        m.m03 = (0.5f * (m.m03 + m.m33)+offset.x*m.m33)*scale;
        m.m10 = (0.5f * (m.m10 + m.m30)+offset.y*m.m30)*scale;
        m.m11 = (0.5f * (m.m11 + m.m31)+offset.y*m.m31)*scale;
        m.m12 = (0.5f * (m.m12 + m.m32)+offset.y*m.m32)*scale;
        m.m13 = (0.5f * (m.m13 + m.m33)+offset.y*m.m33)*scale;
        m.m20 = 0.5f * (m.m20 + m.m30);
        m.m21 = 0.5f * (m.m21 + m.m31);
        m.m22 = 0.5f * (m.m22 + m.m32);
        m.m23 = 0.5f * (m.m23 + m.m33);
        return m;
    }
    public void CleanUp()
    {
        buffer.ReleaseTemporaryRT(dirShadowAtlasID);
        if (shadowedOtherLightCount>0)
        {
            buffer.ReleaseTemporaryRT(otherShadowAtlasID);
        }
        ExcuteBuffer();
    }

    /// <summary>
    /// 设置级联阴影数据
    /// </summary>
    void SetCascadeData(int index,Vector4 cullingSphere,float tileSize)
    {
        float texelSize = 2f * cullingSphere.w / tileSize;
        float filterSize = texelSize * ((float)settings.directional.filterMode + 1f);
        cullingSphere.w -= filterSize;
        cullingSphere.w *= cullingSphere.w;
        cascadeCullingSpheres[index] = cullingSphere;
        cascadeData[index] = new Vector4(1f / cullingSphere.w, filterSize*1.4142136f);
    }
    
    /// <summary>
    /// 设置当前要渲染的tile区域到视口
    /// </summary>
    Vector2 SetTileViewport(int index,int split,float tileSize)
    {
        //计算当前块的列索引和行索引
        Vector2 offset = new Vector2(index % split, index / split);
        buffer.SetViewport(new Rect(offset.x*tileSize,offset.y*tileSize,tileSize,tileSize));
        return offset;
    }

    void SetOtherTileData(int index, Vector2 offset,float scale,float bias)
    {
        float border = atlasSize.w * 0.5f;
        Vector4 data=Vector4.zero;
        data.w = bias;
        data.x = offset.x * scale + border;
        data.y = offset.y * scale + border;
        data.z = scale - border - border;
        otherShadowTiles[index] = data;
    }

    /// <summary>
    /// 遍历关键字数组，对应索引的Shader关键字开启，其它索引的关键字关闭
    /// 因为使用buffer，场景中所有的Shader都会生效，比较适用于管线中的统一设置 
    /// </summary>
    void SetKeywords(string[] keywords,int enabledIndex)
    {
        for (int i = 0; i < keywords.Length; i++)
        {
            if (i == enabledIndex)
            {
                buffer.EnableShaderKeyword(keywords[i]);
            }
            else
            {
                buffer.DisableShaderKeyword(keywords[i]);
            }
        }
    }
}
