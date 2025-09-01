using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEditor.Rendering;
using LitDetailGUI = HarlensLastStand.ShaderGUI.LitDetailGUI;

namespace HarlensLastStand.ShaderGUI
{
    internal class VATLitShader : BaseShaderGUI
    {
        protected MaterialProperty vertexPositionProperty { get; set; }
        protected MaterialProperty vertexNormalProperty { get; set; }
        protected MaterialProperty vatAnimMeta { get; set; }
        protected MaterialProperty vatInvAnimCount { get; set; }
        protected MaterialProperty vatAnimIndex { get; set; }


        /*
        	_TotalFrames ("Total Frames", Float) = 1.0
		    _TextureHeight ("Texture Height", Float) = 0.0
		    _TextureWidth ("Texture Width", Float) = 0.0
		    _RowsPerFrame ("Rows Per Frame", Float) = 0.0
        */


        static readonly string[] workflowModeNames = Enum.GetNames(typeof(LitGUI.WorkflowMode));

        private LitGUI.LitProperties litProperties;
        private LitDetailGUI.LitProperties litDetailProperties;

        public override void OnGUI(MaterialEditor materialEditorIn, MaterialProperty[] properties)
        {
            DrawVertexPositionProperties(materialEditorIn.target as Material);
            base.OnGUI(materialEditorIn, properties);
            
        }


        public override void FillAdditionalFoldouts(MaterialHeaderScopeList materialScopesList)
        {
            materialScopesList.RegisterHeaderScope(LitDetailGUI.Styles.detailInputs, Expandable.Details, _ => LitDetailGUI.DoDetailArea(litDetailProperties, materialEditor));
        }

        // collect properties from the material properties
        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);
            litProperties = new LitGUI.LitProperties(properties);
            litDetailProperties = new LitDetailGUI.LitProperties(properties);
            vertexPositionProperty = FindProperty("_PositionTexture", properties, propertyIsMandatory: false);
            vertexNormalProperty = FindProperty("_NormalTexture", properties, propertyIsMandatory: false);
            vatAnimMeta = FindProperty("_AnimMeta", properties, propertyIsMandatory: false);
            vatInvAnimCount = FindProperty("_InvAnimCount", properties, propertyIsMandatory: false);
            vatAnimIndex = FindProperty("_AnimIndex", properties, propertyIsMandatory: false);
        }

        // material changed check
        public override void ValidateMaterial(Material material)
        {
            SetMaterialKeywords(material, LitGUI.SetMaterialKeywords, LitDetailGUI.SetMaterialKeywords);
        }

        // material main surface options
        public override void DrawSurfaceOptions(Material material)
        {
            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;

            if (litProperties.workflowMode != null)
                DoPopup(LitGUI.Styles.workflowModeText, litProperties.workflowMode, workflowModeNames);

            base.DrawSurfaceOptions(material);
        }

        // material main surface inputs
        public override void DrawSurfaceInputs(Material material)
        {
            base.DrawSurfaceInputs(material);
            LitGUI.Inputs(litProperties, materialEditor, material);
            DrawEmissionProperties(material, true);
            DrawTileOffset(materialEditor, baseMapProp);
        }

        // material main advanced options
        public override void DrawAdvancedOptions(Material material)
        {
            if (litProperties.reflections != null && litProperties.highlights != null)
            {
                materialEditor.ShaderProperty(litProperties.highlights, LitGUI.Styles.highlightsText);
                materialEditor.ShaderProperty(litProperties.reflections, LitGUI.Styles.reflectionsText);
            }

            base.DrawAdvancedOptions(material);
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            // _Emission property is lost after assigning Standard shader to the material
            // thus transfer it before assigning the new shader
            if (material.HasProperty("_Emission"))
            {
                material.SetColor("_EmissionColor", material.GetColor("_Emission"));
            }

            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            if (oldShader == null || !oldShader.name.Contains("Legacy Shaders/"))
            {
                SetupMaterialBlendMode(material);
                return;
            }

            SurfaceType surfaceType = SurfaceType.Opaque;
            BlendMode blendMode = BlendMode.Alpha;
            if (oldShader.name.Contains("/Transparent/Cutout/"))
            {
                surfaceType = SurfaceType.Opaque;
                material.SetFloat("_AlphaClip", 1);
            }
            else if (oldShader.name.Contains("/Transparent/"))
            {
                // NOTE: legacy shaders did not provide physically based transparency
                // therefore Fade mode
                surfaceType = SurfaceType.Transparent;
                blendMode = BlendMode.Alpha;
            }
            material.SetFloat("_Blend", (float)blendMode);

            material.SetFloat("_Surface", (float)surfaceType);
            if (surfaceType == SurfaceType.Opaque)
            {
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else
            {
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            if (oldShader.name.Equals("Standard (Specular setup)"))
            {
                material.SetFloat("_WorkflowMode", (float)LitGUI.WorkflowMode.Specular);
                Texture texture = material.GetTexture("_SpecGlossMap");
                if (texture != null)
                    material.SetTexture("_MetallicSpecGlossMap", texture);
            }
            else
            {
                material.SetFloat("_WorkflowMode", (float)LitGUI.WorkflowMode.Metallic);
                Texture texture = material.GetTexture("_MetallicGlossMap");
                if (texture != null)
                    material.SetTexture("_MetallicSpecGlossMap", texture);
            }

        }

        public static readonly GUIContent vertexPositionMap = EditorGUIUtility.TrTextContent("Vertex Position Texture", "Assign vertex positions texture.");
        public static readonly GUIContent vertexNormalMap = EditorGUIUtility.TrTextContent("Vertex Normal Texture", "Assign vertex normals texture.");

        public static readonly GUIContent vertexAnimMeta = EditorGUIUtility.TrTextContent("AnimMeta", "Animation Meta");
        //public static readonly GUIContent vatFrameRateContent = EditorGUIUtility.("Vertex Normal Texture", "Assign vertex normals texture.");
        public void DrawVertexPositionProperties(Material material)
        {
            if (vertexPositionProperty != null)
            {
                var cur = vertexPositionProperty.textureValue as Texture2DArray;
                var next = (Texture2DArray)EditorGUILayout.ObjectField(
                    new GUIContent("Vertex Position Texture (2DArray)", "Assign the VAT position Texture2DArray"),
                    cur, typeof(Texture2DArray), false);

                if (next != cur)
                    vertexPositionProperty.textureValue = next;
            }
            if (vertexNormalProperty != null)
            {

                var cur = vertexNormalProperty.textureValue as Texture2DArray;
                var next = (Texture2DArray)EditorGUILayout.ObjectField(
                    new GUIContent("Vertex Normal Texture (2DArray)", "Assign the VAT normal Texture2DArray"),
                    cur, typeof(Texture2DArray), false);

                if (next != cur)
                    vertexNormalProperty.textureValue = next;
            }

            if (vatAnimMeta != null)
            {
                materialEditor.TexturePropertySingleLine(vertexAnimMeta, vatAnimMeta);
            }

            if (vatInvAnimCount != null)
                materialEditor.ShaderProperty(vatInvAnimCount, new GUIContent("Inverted Anim Count"), 0);

            if (vatAnimIndex != null)
                materialEditor.ShaderProperty(vatAnimIndex, new GUIContent("Animation Index"), 0);

        }

    }
}
