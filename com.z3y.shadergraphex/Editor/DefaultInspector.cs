using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace z3y.ShaderGraphExtended
{
    public class DefaultInspector : ShaderGUI
    {
        
        private bool _firstTime = true;

        private int _Mode;
        private int _SrcBlend;
        private int _DstBlend;
        private int _ZWrite;
        private int _AlphaToMask;
        private int _Cull;

        private int _BakeryMonoSH;
        private int _LightmappedSpecular;
        private int _NonLinearLightProbeSH;

        
        private int _SpecularHighlights;
        private int _GlossyReflections;
        
        private int _LTCGI;
        private int _LTCGI_DIFFUSE_OFF;


        private static bool surfaceOptionsFoldout = true;
        private static bool surfaceInputsFoldout = true;
        private static bool additionalSettingsFoldout = true;

        private int propCount = 0;

        private (int, int) overridePropertiesRange;
        
        private static MaterialProperty DrawPropertyFromIndex(MaterialEditor materialEditor, MaterialProperty[] properties, int index)
        {
            if (index < 0 || index > properties.Length) return null;
            
            var prop = properties[index];
            materialEditor.ShaderProperty(prop, prop.displayName);
            return prop;
        }
        
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            if (_firstTime || propCount != properties.Length)
            {
                propCount = properties.Length;

                _Mode = Array.FindIndex(properties, x => x.name.Equals("_Mode", StringComparison.Ordinal));
                _SrcBlend = Array.FindIndex(properties, x => x.name.Equals("_SrcBlend", StringComparison.Ordinal));
                _DstBlend = Array.FindIndex(properties, x => x.name.Equals("_DstBlend", StringComparison.Ordinal));
                _ZWrite = Array.FindIndex(properties, x => x.name.Equals("_ZWrite", StringComparison.Ordinal));
                _AlphaToMask = Array.FindIndex(properties, x => x.name.Equals("_AlphaToMask", StringComparison.Ordinal));
                _Cull = Array.FindIndex(properties, x => x.name.Equals("_Cull", StringComparison.Ordinal));
                
                _BakeryMonoSH = Array.FindIndex(properties, x => x.name.Equals("_BakeryMonoSH", StringComparison.Ordinal));
                _LightmappedSpecular = Array.FindIndex(properties, x => x.name.Equals("_LightmappedSpecular", StringComparison.Ordinal));
                _NonLinearLightProbeSH = Array.FindIndex(properties, x => x.name.Equals("_NonLinearLightProbeSH", StringComparison.Ordinal));
                
                _SpecularHighlights = Array.FindIndex(properties, x => x.name.Equals("_SpecularHighlights", StringComparison.Ordinal));
                _GlossyReflections = Array.FindIndex(properties, x => x.name.Equals("_GlossyReflections", StringComparison.Ordinal));
                _GlossyReflections = Array.FindIndex(properties, x => x.name.Equals("_GlossyReflections", StringComparison.Ordinal));
                
                _LTCGI = Array.FindIndex(properties, x => x.name.Equals("_LTCGI", StringComparison.Ordinal));
                _LTCGI_DIFFUSE_OFF = Array.FindIndex(properties, x => x.name.Equals("_LTCGI_DIFFUSE_OFF", StringComparison.Ordinal));





                overridePropertiesRange = (_Mode, _Cull);

                _firstTime = false;
            }

            if (true)
            {
                if (surfaceOptionsFoldout = DrawHeaderFoldout(new GUIContent("Rendering Options"), surfaceOptionsFoldout))
                {
                    EditorGUILayout.Space();

                    EditorGUI.BeginChangeCheck();

                    var mode = DrawPropertyFromIndex(materialEditor, properties, _Mode);

                    if (EditorGUI.EndChangeCheck())
                    {
                        SetupBlendMode(materialEditor, mode);
                    }

                    DrawPropertyFromIndex(materialEditor, properties, _SrcBlend);
                    DrawPropertyFromIndex(materialEditor, properties, _DstBlend);
                    DrawPropertyFromIndex(materialEditor, properties, _ZWrite);
                    DrawPropertyFromIndex(materialEditor, properties, _Cull);
                    //DrawPropertyFromIndex(materialEditor, properties, _AlphaToMask);


                    EditorGUILayout.Space();

                }
            }

            if (surfaceInputsFoldout = DrawHeaderFoldout(new GUIContent("Properties"), surfaceInputsFoldout))
            {
                EditorGUILayout.Space();
                for (int i = 0; i < properties.Length; i++)
                {
                    var property = properties[i];

                    if (i >= overridePropertiesRange.Item1 && i <= overridePropertiesRange.Item2)
                    {
                        continue;
                    }

                    if ((property.flags & MaterialProperty.PropFlags.HideInInspector) != 0)
                    {
                        continue;
                    }

                    var content = new GUIContent(property.displayName);
                    switch (property.type)
                    {
                        case MaterialProperty.PropType.Texture:
                        {
                            float fieldWidth = EditorGUIUtility.fieldWidth;
                            float labelWidth = EditorGUIUtility.labelWidth;
                            materialEditor.SetDefaultGUIWidths();
                            materialEditor.TextureProperty(property, content.text);
                            EditorGUIUtility.fieldWidth = fieldWidth;
                            EditorGUIUtility.labelWidth = labelWidth;
                            break;
                        }
                        case MaterialProperty.PropType.Vector:
                        {
                            var vectorRect = EditorGUILayout.GetControlRect(true, MaterialEditor.GetDefaultPropertyHeight(property)/2, EditorStyles.layerMaskField);
                            materialEditor.VectorProperty(vectorRect, property, property.displayName);
                            break;
                        }
                        case MaterialProperty.PropType.Color:
                        case MaterialProperty.PropType.Float:
                        case MaterialProperty.PropType.Range:
                        default:
                            materialEditor.ShaderProperty(property, content);
                            break;
                    }
                }
                EditorGUILayout.Space();

            }



            if (additionalSettingsFoldout = DrawHeaderFoldout(new GUIContent("Additional Settings"), additionalSettingsFoldout))
            {
                EditorGUILayout.Space();
                DrawPropertyFromIndex(materialEditor, properties, _BakeryMonoSH);
                DrawPropertyFromIndex(materialEditor, properties, _LightmappedSpecular);
                DrawPropertyFromIndex(materialEditor, properties, _NonLinearLightProbeSH);

                DrawPropertyFromIndex(materialEditor, properties, _SpecularHighlights);
                DrawPropertyFromIndex(materialEditor, properties, _GlossyReflections);
                
                DrawPropertyFromIndex(materialEditor, properties, _LTCGI);
                DrawPropertyFromIndex(materialEditor, properties, _LTCGI_DIFFUSE_OFF);


                
                EditorGUILayout.Space();
                materialEditor.RenderQueueField();
                materialEditor.EnableInstancingField();
                materialEditor.DoubleSidedGIField();
                materialEditor.LightmapEmissionProperty();
            }

        }
        
        public static void ApplyChanges(Material m)
        {
            if (m.HasProperty("_Emission")) SetupGIFlags(m.GetFloat("_Emission"), m);

            int mode = (int)m.GetFloat("_Mode");
            m.ToggleKeyword("_ALPHATEST_ON", mode == 1);
            m.ToggleKeyword("_ALPHAFADE_ON", mode == 2);
            m.ToggleKeyword("_ALPHAPREMULTIPLY_ON", mode == 3);
            m.ToggleKeyword("_ALPHAMODULATE_ON", mode == 5);
        }
        
        public static void SetupGIFlags(float emissionEnabled, Material material)
        {
            MaterialGlobalIlluminationFlags flags = material.globalIlluminationFlags;
            if ((flags & (MaterialGlobalIlluminationFlags.BakedEmissive | MaterialGlobalIlluminationFlags.RealtimeEmissive)) != 0)
            {
                flags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                if (emissionEnabled != 1)
                    flags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;

                material.globalIlluminationFlags = flags;
            }
        }

        public void SetupBlendMode(MaterialEditor materialEditor, MaterialProperty mode)
        {
            foreach (var o in materialEditor.targets)
            {
                var m = (Material)o;
                SetupMaterialWithBlendMode(m, (int)mode.floatValue);
                ApplyChanges(m);
            }
        }

        public static void SetupMaterialWithBlendMode(Material material, int type)
        {
            switch (type)
            {
                case 0:
                    material.SetOverrideTag("RenderType", "");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.SetInt("_AlphaToMask", 0);
                    material.renderQueue = -1;
                    break;
                case 1: // cutout a2c
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                    material.SetInt("_AlphaToMask", 1);
                    break;
                case 2: // alpha fade
                    SetupTransparentMaterial(material);
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    break;
                case 3: // premultiply
                    SetupTransparentMaterial(material);
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    break;
                case 4: // additive
                    SetupTransparentMaterial(material);
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    break;
                case 5: // multiply
                    SetupTransparentMaterial(material);
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    break;
            }
        }

        private static void SetupTransparentMaterial(Material material)
        {
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.SetInt("_ZWrite", 0);
            material.SetInt("_AlphaToMask", 0);
        }


        #region CoreEditorUtils.cs
        /// <summary>Draw a header</summary>
        /// <param name="title">Title of the header</param>
        public static void DrawHeader(GUIContent title)
        {
            var backgroundRect = GUILayoutUtility.GetRect(1f, 17f);

            var labelRect = backgroundRect;
            labelRect.xMin += 16f;
            labelRect.xMax -= 20f;

            var foldoutRect = backgroundRect;
            foldoutRect.y += 1f;
            foldoutRect.width = 13f;
            foldoutRect.height = 13f;

            // Background rect should be full-width
            backgroundRect.xMin = 0f;
            backgroundRect.width += 4f;

            // Background
            float backgroundTint = EditorGUIUtility.isProSkin ? 0.1f : 1f;
            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));

            // Title
            EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);
        }

        public static bool DrawHeaderFoldout(GUIContent title, bool state, bool isBoxed = false)
        {
            DrawSplitter();
            int previousIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            const float height = 17f;
            var backgroundRect = GUILayoutUtility.GetRect(1f, height);
            float xMin = backgroundRect.xMin;

            var labelRect = backgroundRect;
            labelRect.xMin += 16f;
            labelRect.xMax -= 20f;

            var foldoutRect = backgroundRect;
            foldoutRect.y += 1f;
            foldoutRect.width = 13f;
            foldoutRect.height = 13f;
            foldoutRect.x = labelRect.xMin + 15 * (EditorGUI.indentLevel - 1); //fix for presset


            // Background rect should be full-width
            backgroundRect.xMin = 0f;
            backgroundRect.width += 4f;
            // Background
            float backgroundTint = EditorGUIUtility.isProSkin ? 0.1f : 1f;
            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));


            // Title
            EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

            // Active checkbox
            state = GUI.Toggle(foldoutRect, state, GUIContent.none, EditorStyles.foldout);

            var e = Event.current;
            if (e.type == EventType.MouseDown && backgroundRect.Contains(e.mousePosition) && e.button == 0)
            {
                state = !state;
                e.Use();
            }
            EditorGUI.indentLevel = previousIndent;
            return state;
        }

        /// <summary>Draw a splitter separator</summary>
        /// <param name="isBoxed">[Optional] add margin if the splitter is boxed</param>
        public static void DrawSplitter(bool isBoxed = false)
        {
            var rect = GUILayoutUtility.GetRect(1f, 1f);
            float xMin = rect.xMin;

            // Splitter rect should be full-width
            rect.xMin = 0f;
            rect.width += 4f;

            if (isBoxed)
            {
                rect.xMin = xMin == 7.0 ? 4.0f : EditorGUIUtility.singleLineHeight;
                rect.width -= 1;
            }

            if (Event.current.type != EventType.Repaint)
                return;

            EditorGUI.DrawRect(rect, !EditorGUIUtility.isProSkin
                ? new Color(0.6f, 0.6f, 0.6f, 1.333f)
                : new Color(0.12f, 0.12f, 0.12f, 1.333f));
        }
        #endregion

    }
}