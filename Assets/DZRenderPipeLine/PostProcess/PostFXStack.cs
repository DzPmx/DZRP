using System;
using UnityEngine;
using UnityEngine.Rendering;

partial class PostFXStack
{
    private const string bufferName = "Post FX";
    private CommandBuffer buffer = new CommandBuffer() { name = bufferName };
    private ScriptableRenderContext context;
    private Camera camera;
    private PostFXSettings postFXSettings;

    private int
        bloomBicubicUpsampingID = Shader.PropertyToID("_BloomBicubicUpsampling"),
        bloomPrefilterID=Shader.PropertyToID("_BloomPrefilter"),
        bloomThresholdID=Shader.PropertyToID("_BloomThreshold"),
        bloomIntensityID=Shader.PropertyToID("_BloomIntensity"),
        fxSourceID = Shader.PropertyToID("_PostFXSource"),
        fxSource2ID = Shader.PropertyToID("_PostFXSource2");

    public bool isActive => postFXSettings != null;

    private const int maxBloomPyramidLevels = 16;
    private int bloomPyramidID;

    public PostFXStack()
    {
        bloomPyramidID = Shader.PropertyToID("_BloomPyramid0");

        for (int i = 0; i < maxBloomPyramidLevels * 2; i++)
        {
            Shader.PropertyToID("_BloomPyramid" + i);
        }
    }

    enum Pass
    {
        BloomHorizontal,
        BloomVertical,
        BloomCombine,
        BloomPrefilter,
        Copy,
    }


    public void Setup(ScriptableRenderContext context, Camera camera, PostFXSettings postFXSettings)
    {
        this.context = context;
        this.camera = camera;
        this.postFXSettings = camera.cameraType <= CameraType.SceneView ? postFXSettings : null;
        ApplySceneViewState();
    }

    public void Render(int sourceID)
    {
        //TriangleBlit(sourceID,BuiltinRenderTextureType.CameraTarget,Pass.Copy);
        DoBloom(sourceID);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    void TriangleBlit(RenderTargetIdentifier source, RenderTargetIdentifier dest, Pass pass)
    {
        buffer.SetGlobalTexture(fxSourceID, source);
        buffer.SetRenderTarget(dest, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.DrawProcedural(Matrix4x4.identity, postFXSettings.Material, (int)pass, MeshTopology.Triangles, 3);
    }

    void DoBloom(int sourceID)
    {
        buffer.BeginSample("Bloom");
        PostFXSettings.BloomSettings bloomSettings = postFXSettings.Bloom;
        int width = camera.pixelWidth / 2, height = camera.pixelHeight / 2;

        //跳过bloom
        if (
            bloomSettings.maxIterations == 0 ||
            height < bloomSettings.downScaleLimit * 2 || width < bloomSettings.downScaleLimit * 2
            || bloomSettings.Intensity<=0
        )
        {
            TriangleBlit(sourceID, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
            buffer.EndSample("Bloom");
            return;
        }

        Vector4 threshold;
        threshold.x = Mathf.GammaToLinearSpace(bloomSettings.threshold);
        threshold.y = threshold.x * bloomSettings.thresholdKnee;
        threshold.z = 2f * threshold.y;
        threshold.w = 0.25f / (threshold.y + 0.00001f);
        threshold.y -= threshold.x;
        buffer.SetGlobalVector(bloomThresholdID,threshold);
        RenderTextureFormat format = RenderTextureFormat.Default;
        buffer.GetTemporaryRT(bloomPrefilterID, width, height, 0, FilterMode.Bilinear, format);
        TriangleBlit(sourceID, bloomPrefilterID, Pass.BloomPrefilter);
        width /= 2;
        height /= 2;
        int fromID = bloomPrefilterID, toID = bloomPyramidID + 1;
        int i;
        //降采样
        for (i = 0; i < bloomSettings.maxIterations; i++)
        {
            if (height < bloomSettings.downScaleLimit || width < bloomSettings.downScaleLimit)
            {
                break;
            }

            int midID = toID - 1;
            buffer.GetTemporaryRT(midID, width, height, 0, FilterMode.Bilinear, format);
            buffer.GetTemporaryRT(toID, width, height, 0, FilterMode.Bilinear, format);
            TriangleBlit(fromID, midID, Pass.BloomHorizontal);
            TriangleBlit(midID, toID, Pass.BloomVertical);
            fromID = toID;
            toID += 2;
            width /= 2;
            height /= 2;
        }

        buffer.ReleaseTemporaryRT(bloomPrefilterID);
        buffer.SetGlobalFloat(bloomBicubicUpsampingID, bloomSettings.bicubicUpsampling ? 1f : 0f);
        buffer.SetGlobalFloat(bloomIntensityID,1f);
        if (i > 1)
        {
            buffer.ReleaseTemporaryRT(fromID - 1);
            toID -= 5;
            for (i -= 1; i > 0; i--)
            {
                buffer.SetGlobalTexture(fxSource2ID, toID + 1);
                TriangleBlit(fromID, toID, Pass.BloomCombine);
                buffer.ReleaseTemporaryRT(fromID);
                buffer.ReleaseTemporaryRT(toID + 1);
                fromID = toID;
                toID -= 2;
            }
        }
        else
        {
            buffer.ReleaseTemporaryRT(bloomPyramidID);
        }
        buffer.SetGlobalFloat(bloomIntensityID,bloomSettings.Intensity);
        buffer.SetGlobalTexture(fxSource2ID, sourceID);
        TriangleBlit(fromID, BuiltinRenderTextureType.CameraTarget, Pass.BloomCombine);
        buffer.ReleaseTemporaryRT(fromID);
        buffer.EndSample("Bloom");
    }
}