using System;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Rendering/DZRP Post FX Settings")]
public class PostFXSettings : ScriptableObject
{
    [SerializeField] private Shader shader = default;
    
    [NonSerialized] private Material material;
    [SerializeField] private BloomSettings bloomSettings = default;
    public BloomSettings Bloom => bloomSettings;
    [Serializable] public struct BloomSettings
    {
        [FormerlySerializedAs("maxIteractions")] [FormerlySerializedAs("MaxIteractions")] [Range(0,16)]public int maxIterations;
        [Min(1f)] public int downScaleLimit;
        public bool bicubicUpsampling;
        [Min(0f)] public float threshold;
        [Range(0f, 1f)] public float thresholdKnee;
        [Min(0f)] public float Intensity;
    }
    public Material Material
    {
        get
        {
            if (material==null && shader!=null)
            {
                material = new Material(shader);
                material.hideFlags = HideFlags.HideAndDontSave;
            }

            return material;
        }
    }

}