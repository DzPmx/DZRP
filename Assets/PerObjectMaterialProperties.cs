using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

public class PerObjectMaterialProperties : MonoBehaviour
{
    private static int baseTexID = Shader.PropertyToID("_BaseMap");
    private static int emissionTexID = Shader.PropertyToID("_EmissionMap");
    private static int baseColorID = Shader.PropertyToID("_BaseCol");
    private static int alphaClipID = Shader.PropertyToID("_AlphaClip");
    private static int metallicID = Shader.PropertyToID("_Metallic");
    private static int smoothnessID = Shader.PropertyToID("_Smoothness");
    private static int emissionColID = Shader.PropertyToID("_EmissionCol");
    [SerializeField] Color baseColor= Color.white;
    [SerializeField, Range(0f, 1f)] private float alphaClip = 0.5f;
    [SerializeField, Range(0f, 1f)] private float smoothness= 0.5f;
    [SerializeField, Range(0f, 1f)] private float metallic = 0.0f;
    [SerializeField, ColorUsage(false, true)]
    private Color emissionCol = Color.black; 
    private static MaterialPropertyBlock block;

    private void OnValidate()
    {
        if (block is null)
        {
            block = new MaterialPropertyBlock();
        }
        
        block.SetColor(baseColorID,baseColor);
        block.SetColor(emissionColID,emissionCol);
        block.SetFloat(alphaClipID,alphaClip);
        block.SetFloat(metallicID,metallic);
        block.SetFloat(smoothnessID,smoothness);
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].SetPropertyBlock(block);
        }
    }

    private void Awake()
    {
        OnValidate();
    }
}
