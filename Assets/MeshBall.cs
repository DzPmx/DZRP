using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

public class MeshBall : MonoBehaviour
{
    private static int _baseColID = Shader.PropertyToID("_BaseCol");
    private static int _alphaClipID = Shader.PropertyToID("_AlphaClip");
    private static int _metallicID = Shader.PropertyToID("_Metallic");
    private static int _smoothnessID = Shader.PropertyToID("_Smoothness");
    [SerializeField]private Mesh _mesh = default;
    [SerializeField]private Material _material = default;
    
    private Matrix4x4[] _matrices = new Matrix4x4[1023];
    private Vector4[] _baseCols = new Vector4[1023];
    private float[] alphaClip = new float[1023];
    private float[] metallic = new float[1023];
    private float[] smoothness = new float[1023];

    private MaterialPropertyBlock _mpb;
    [SerializeField] private LightProbeProxyVolume lightProbeProxyVolume = null;

    private void Awake()
    {
        for (int i = 0; i < _matrices.Length; i++)
        {
            //随机的位置，旋转值默认，缩放默认
            _matrices[i] = Matrix4x4.TRS(Random.insideUnitSphere * 10f, Quaternion.Euler(Random.value*360f,Random.value*360f,Random.value*360f), Vector3.one*Random.Range(0.5f,1.5f));
            _baseCols[i] = new Vector4(Random.value, Random.value, Random.value, Random.Range(0.2f,1.0f));
            alphaClip[i] = Random.Range(0.2f, 1.0f);
            metallic[i] = Random.Range(0, 1);
            smoothness[i] = Random.Range(0, 1);
            
        }
    }

    private void Update()
    {
        if (_mpb is null)
        {
            _mpb = new MaterialPropertyBlock();
            _mpb.SetVectorArray(_baseColID,_baseCols);
            _mpb.SetFloatArray(_alphaClipID,alphaClip);
            _mpb.SetFloatArray(_smoothnessID,smoothness);
            _mpb.SetFloatArray(_metallicID,metallic);
            if (!lightProbeProxyVolume)
            {
                var positions = new Vector3[1023];
                for (int i = 0; i < _matrices.Length; i++)
                {
                    positions[i] = _matrices[i].GetColumn(3);//第四列是位置信息
                }

                var lightProbes = new SphericalHarmonicsL2[1023];
                var occlusionProbes = new Vector4[1023];
                LightProbes.CalculateInterpolatedLightAndOcclusionProbes(positions,lightProbes,occlusionProbes);
                _mpb.CopySHCoefficientArraysFrom(lightProbes);
                _mpb.CopyProbeOcclusionArrayFrom(occlusionProbes);
            }

        }
        Graphics.DrawMeshInstanced(_mesh,0,_material,_matrices,1023,_mpb,ShadowCastingMode.On,true,0,null,
            lightProbeProxyVolume? LightProbeUsage.UseProxyVolume: LightProbeUsage.CustomProvided,lightProbeProxyVolume);
    }
}
