using UnityEngine;
using UnityEditor;

public class TerrainShaderGUI : MyBaseShaderGUI {

    public override void OnGUI(
        MaterialEditor editor, MaterialProperty[] properties
    ) {
        base.OnGUI(editor, properties);
        editor.ShaderProperty(FindProperty("_MapScale"), MakeLabel("Map Scale"));
        DoMaps();
        DoRimLighting();
        DoBlending();
        DoMOHS();
        DoOtherSettings();
    }

    void DoMaps() {
        GUILayout.Label("Flat Maps", EditorStyles.boldLabel);
        editor.TexturePropertySingleLine(MakeLabel("Albedo"), FindProperty("_FlatTex"));
        editor.TexturePropertySingleLine(MakeLabel("Normals"), FindProperty("_FlatNorm"));
        editor.TexturePropertySingleLine(MakeLabel("MOHS", "Metallic (R) Occlusion (G) Height (B) Smoothness (A)"), FindProperty("_FlatMOHS"));
        editor.ShaderProperty(FindProperty("_FlatTexScale"), MakeLabel("Texture Scale"));
        editor.ShaderProperty(FindProperty("_FlatBumpScale"), MakeLabel("Bump Scale"));
        editor.ShaderProperty(FindProperty("_FlatSlopeThreshold"), MakeLabel("Slope Threshold"));
        editor.ShaderProperty(FindProperty("_FlatBlendAmount"), MakeLabel("Blend Amount"));

        GUILayout.Label("Main Maps", EditorStyles.boldLabel);
        editor.TexturePropertySingleLine(MakeLabel("Albedo"), FindProperty("_MainTex"));
        editor.TexturePropertySingleLine(MakeLabel("Normals"), FindProperty("_MainNorm"));
        editor.TexturePropertySingleLine(MakeLabel("MOHS", "Metallic (R) Occlusion (G) Height (B) Smoothness (A)"), FindProperty("_MainMOHS"));
        editor.ShaderProperty(FindProperty("_MainTexScale"), MakeLabel("Texture Scale"));
        editor.ShaderProperty(FindProperty("_MainBumpScale"), MakeLabel("Bump Scale"));
        editor.ShaderProperty(FindProperty("_MainSlopeThreshold"), MakeLabel("Slope Threshold"));
        editor.ShaderProperty(FindProperty("_MainBlendAmount"), MakeLabel("Blend Amount"));

        GUILayout.Label("Steep Maps", EditorStyles.boldLabel);
        editor.TexturePropertySingleLine(MakeLabel("Albedo"), FindProperty("_SteepTex"));
        editor.TexturePropertySingleLine(MakeLabel("Normals"), FindProperty("_SteepNorm"));
        editor.TexturePropertySingleLine(MakeLabel("MOHS", "Metallic (R) Occlusion (G) Height (B) Smoothness (A)"), FindProperty("_SteepMOHS"));
        editor.ShaderProperty(FindProperty("_SteepTexScale"), MakeLabel("Texture Scale"));
        editor.ShaderProperty(FindProperty("_SteepBumpScale"), MakeLabel("Bump Scale"));
        editor.ShaderProperty(FindProperty("_SteepSlopeThreshold"), MakeLabel("Slope Threshold"));
        editor.ShaderProperty(FindProperty("_SteepBlendAmount"), MakeLabel("Blend Amount"));

        GUILayout.Label("Overflow Maps", EditorStyles.boldLabel);
        editor.TexturePropertySingleLine(MakeLabel("Albedo"), FindProperty("_OverTex"));
        editor.TexturePropertySingleLine(MakeLabel("Normals"), FindProperty("_OverNorm"));
        editor.TexturePropertySingleLine(MakeLabel("MOHS", "Metallic (R) Occlusion (G) Height (B) Smoothness (A)"), FindProperty("_OverMOHS"));
        editor.ShaderProperty(FindProperty("_OverTexScale"), MakeLabel("Texture Scale"));
        editor.ShaderProperty(FindProperty("_OverBumpScale"), MakeLabel("Bump Scale"));
    }

    void DoRimLighting() {
        GUILayout.Label("Rim Lighting", EditorStyles.boldLabel);

        editor.ColorProperty(FindProperty("_RimColor"), "Rim Color");
        editor.ShaderProperty(FindProperty("_RimPower"), MakeLabel("Rim Power"));
        editor.ShaderProperty(FindProperty("_RimFac"), MakeLabel("Rim Factor"));
    }

    void DoBlending() {
        GUILayout.Label("Blending", EditorStyles.boldLabel);

        editor.ShaderProperty(FindProperty("_BlendOffset"), MakeLabel("Blend Offset"));
        editor.ShaderProperty(FindProperty("_BlendExponent"), MakeLabel("Blend Exponent"));
        editor.ShaderProperty(FindProperty("_BlendHeightStrength"), MakeLabel("Blend Height Strength"));
    }

    void DoMOHS() {
        GUILayout.Label("MOHS", EditorStyles.boldLabel);

        editor.ShaderProperty(FindProperty("_BaseMetallic"), MakeLabel("Metallic"));
        editor.ShaderProperty(FindProperty("_BaseOcclusion"), MakeLabel("Occlusion"));
        editor.ShaderProperty(FindProperty("_BaseSmoothness"), MakeLabel("Smoothness"));
    }

    void DoOtherSettings() {
        GUILayout.Label("Other Settings", EditorStyles.boldLabel);

        editor.RenderQueueField();
        editor.EnableInstancingField();
    }
}