using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class DZRPLitGUI : ShaderGUI
{
    private MaterialEditor editor;
    private Object[] materials;
    private MaterialProperty[] properties;
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        EditorGUI.BeginChangeCheck();
        editor = materialEditor;
        materials = materialEditor.targets;
        this.properties = properties;
        ShowPresets();
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        base.OnGUI(editor,this.properties);
        BakedEmission();
        EditorGUILayout.EndVertical();
        if (EditorGUI.EndChangeCheck())
        {
            SetShadowCasterPass();
            CopyLightMappingProperties();
        }
        
    }

    enum ShadowMode
    {
        On,Clip,Dither,Off,
    }

    ShadowMode Shadows
    {
        set
        {
            if (SetProperty("_Shadows",(float)value))
            {
                Setkeyword("_SHADOWS_CLIP",value==ShadowMode.Clip);
                Setkeyword("_SHADOWS_DITHER",value==ShadowMode.Dither);
            }
        }
    }
    

    bool SetProperty(string name, float value)
    {
       MaterialProperty property= FindProperty(name, properties);
       if (property is not null)
       {
           property.floatValue = value;
           return true;
       }

       return false;
    }


    void Setkeyword(string keyword, bool enabled)
    {
        if (enabled)
        {
            foreach (Material m in materials)
            {
                m.EnableKeyword(keyword);
            }
        }
        else
        {
            foreach (Material m in materials)
            {
                m.DisableKeyword(keyword);
            }
        }
    }


    void SetProperty(string name, string keyword, bool value)
    {
        if (SetProperty(name, value ? 1f : 0f))
        {
            Setkeyword(keyword, value);
        }
        
    }


    private bool Clipping
    {
        set => SetProperty("_Clipping", "_CLIPPING", value);
        get => FindProperty("_Clipping", properties).floatValue == 1;
    }


    private bool PreMultiplyAlpha
    {
        set => SetProperty("_PremultiplyAlpha", "_PREMULTIPLY_ALPHA", value);
    }
    private BlendMode SrcBlend
    {
        set => SetProperty("_SrcBlend", (float)value);
    }
    private BlendMode DestBlend
    {
        set => SetProperty("_DestBlend", (float)value);
    }
    private bool Zwrite
    {
        set => SetProperty("_Zwrite", value ? 1f : 0f);
    }
    RenderQueue renderQueue
    {
        set
        {
            foreach (Material m in materials)
            {
                m.renderQueue = (int)value;
            }
        }
    }
    bool PresentButton(string name)
    {
        if (GUILayout.Button(name))
        {
            editor.RegisterPropertyChangeUndo(name); //注册撤回操作
            return true;
        }

        return false;
    }

    void OpauePresent()
    {
        if (PresentButton("Opaque"))
        {
            Clipping = false;
            PreMultiplyAlpha = false;
            SrcBlend = BlendMode.One;
            DestBlend = BlendMode.Zero;
            Zwrite = true;
            renderQueue = RenderQueue.Geometry;
            Shadows = ShadowMode.On;
        }

    }

    void ClipPresent()
    {
        if (PresentButton("Alpha Clip"))
        {
            Clipping = true;
            PreMultiplyAlpha = false;
            SrcBlend = BlendMode.One;
            DestBlend = BlendMode.Zero;
            Zwrite = true;
            renderQueue = RenderQueue.AlphaTest;
            Shadows = ShadowMode.Clip;
        }
    }

    void FadePresent()
    {
        if (PresentButton("Fade"))
        {
            Clipping = false;
            PreMultiplyAlpha = false;
            SrcBlend = BlendMode.SrcAlpha;
            DestBlend = BlendMode.OneMinusSrcAlpha;
            Zwrite = false;
            renderQueue = RenderQueue.Transparent;
            Shadows = ShadowMode.Dither;
        }
    }

    void TransparentPresent()
    {
        if (PresentButton("Transparent"))
        {
            Clipping = false;
            PreMultiplyAlpha = true;
            SrcBlend = BlendMode.One;
            DestBlend = BlendMode.OneMinusSrcAlpha;
            Zwrite = false;
            renderQueue = RenderQueue.Transparent;
            Shadows = ShadowMode.Dither;
        }
    }

    void SetShadowCasterPass()
    {
        MaterialProperty shadows = FindProperty("_Shadows", properties, false);
        if (shadows is null || shadows .hasMixedValue)
        {
            return;
        }

        bool enabled = shadows.floatValue < (float)ShadowMode.Off;
        foreach (Material m in materials)
        {
            m.SetShaderPassEnabled("ShadowCaster",enabled);
        }
    }
    void ShowPresets()
    {
        EditorGUILayout.LabelField("Selecet Mode", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        OpauePresent();
        ClipPresent();
        FadePresent();
        TransparentPresent();
        EditorGUILayout.EndVertical();
    }

    void BakedEmission()
    {
        EditorGUI.BeginChangeCheck();
        editor.LightmapEmissionProperty();
        if (EditorGUI.EndChangeCheck())
        {
            foreach (Material m in materials)
            {
                //按位取反在判断与操作
                m.globalIlluminationFlags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
        }
    }

    void CopyLightMappingProperties()
    {
        MaterialProperty mainTex = FindProperty("_MainTex", properties);
        MaterialProperty baseMap = FindProperty("_BaseMap", properties);
        if (mainTex is not null && baseMap is not null)
        {
            mainTex.textureValue = baseMap.textureValue;
            mainTex.textureScaleAndOffset = baseMap.textureScaleAndOffset;
        }
        
        MaterialProperty color = FindProperty("_Color", properties);
        MaterialProperty baseColor = FindProperty("_BaseCol", properties);
        if (color is not null && baseColor is not null)
        {
            color.colorValue = baseColor.colorValue;
        }
        
        MaterialProperty alphaClip = FindProperty("_AlphaClip", properties);
        MaterialProperty cutOff = FindProperty("_Cutoff", properties);
        if (alphaClip is not null && cutOff is not null)
        {
            cutOff.floatValue = alphaClip.floatValue;
        }
    }
}