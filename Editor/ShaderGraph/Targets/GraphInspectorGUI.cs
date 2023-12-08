using System;
using System.Linq;
using UnityEditor.ShaderGraph;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

namespace z3y.BuiltIn.ShaderGraph
{
    internal class GraphInspectorGUI
    {
        private TargetPropertyGUIContext _context;
        private Action _onChange;
        private Action<string> _registerUndo;

        public GraphInspectorGUI(TargetPropertyGUIContext context, Action onChange, Action<string> registerUndo)
        {
            _context = context;
            _onChange = onChange;
            _registerUndo = registerUndo;
        }

        public void Draw(BuiltInTarget target)
        {
            var renderStates = CreateFoldout(target, "Rendering Options");
            if (renderStates)
            {
                DrawRenderingOptions(target);
                DrawDefaultProperties(target);
            }
            DrawPassProperties(target);
            if (target.activeSubTarget is BuiltInLitSubTarget lit)
            {
                DrawLitProperties(lit, target);
            }
            DrawVRCTagsToggleGUI(target);

            AddSpace();
        }

        private void DrawRenderingOptions(BuiltInTarget target)
        {

            _context.AddProperty("Surface Type", new EnumField(SurfaceType.Opaque) { value = target.surfaceType }, (evt) =>
            {
                if (Equals(target.surfaceType, evt.newValue))
                    return;

                _registerUndo("Change Surface");
                target.surfaceType = (SurfaceType)evt.newValue;
                _onChange();
            });

            _context.AddProperty("Blending Mode", new EnumField(AlphaMode.Alpha) { value = target.alphaMode }, target.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                if (Equals(target.alphaMode, evt.newValue))
                    return;

                _registerUndo("Change Blend");
                target.alphaMode = (AlphaMode)evt.newValue;
                _onChange();
            });

            _context.AddProperty("Render Face", new EnumField(RenderFace.Front) { value = target.renderFace }, (evt) =>
            {
                if (Equals(target.renderFace, evt.newValue))
                    return;

                _registerUndo("Change Render Face");
                target.renderFace = (RenderFace)evt.newValue;
                _onChange();
            });

            _context.AddProperty("Depth Write", new EnumField(ZWriteControl.Auto) { value = target.zWriteControl }, (evt) =>
            {
                if (Equals(target.zWriteControl, evt.newValue))
                    return;

                _registerUndo("Change Depth Write Control");
                target.zWriteControl = (ZWriteControl)evt.newValue;
                _onChange();
            });

            _context.AddProperty("Depth Test", new EnumField(ZTestMode.LEqual) { value = target.zTestMode }, (evt) =>
            {
                if (Equals(target.zTestMode, evt.newValue))
                    return;

                _registerUndo("Change Depth Test");
                target.zTestMode = (ZTestMode)evt.newValue;
                _onChange();
            });

            _context.AddProperty("Alpha Clipping", new Toggle() { value = target.alphaClip }, (evt) =>
            {
                if (Equals(target.alphaClip, evt.newValue))
                    return;

                _registerUndo("Change Alpha Clip");
                target.alphaClip = evt.newValue;
                _onChange();
            });
        }

        private void DrawPassProperties(BuiltInTarget target)
        {
            var foldout = CreateFoldout(target, "Pass Options");
            if (!foldout) return;
            
            _context.AddProperty("Grab Pass", new Toggle() { value = target.grabPass }, (evt) =>
            {
                OnPropertyChangeValidate(ref target.grabPass, evt, "Change grab pass");
            });

            _context.AddProperty("Outline Pass", new Toggle() { value = target.generateOutlinePass }, (evt) =>
            {
                OnPropertyChangeValidate(ref target.generateOutlinePass, evt, "Change generateOutlinePass");
            });
            
            if (target.generateOutlinePass)
            {
                _context.globalIndentLevel++;
                _context.AddProperty("Outline Early", new Toggle() { value = target.generateOutlineEarly }, (evt) =>
                {
                    OnPropertyChangeValidate(ref target.generateOutlineEarly, evt, "Change generateOutlineEarly");
                });
                _context.AddProperty("Stencil Outline", new Toggle() { value = target.stencilOutlineEnabled }, (evt) =>
                {
                    OnPropertyChangeValidate(ref target.stencilOutlineEnabled, evt, "Change stencilOutlineEnabled");
                });
                _context.globalIndentLevel--;
            }

        }

        private void DrawDefaultProperties(BuiltInTarget target)
        {
            _context.AddProperty("Alpha To Coverage", new Toggle() { value = target.alphaToMask }, (evt) =>
            {
                OnPropertyChangeValidate(ref target.alphaToMask, evt, "Change alphaToMask");
            });

            _context.AddProperty("Stencil", new Toggle() { value = target.stencilEnabled }, (evt) =>
            {
                OnPropertyChangeValidate(ref target.stencilEnabled, evt, "Change stencilEnabled");
            });

            if (target.stencilEnabled)
            {
                _context.globalIndentLevel++;
                _context.AddProperty("Front Back", new Toggle() { value = target.stencilFrontBack }, (evt) =>
                {
                    OnPropertyChangeValidate(ref target.stencilFrontBack, evt, "Change stencilFrontBack");
                });
                _context.globalIndentLevel--;
            }
        }

        private void DrawVRCTagsToggleGUI(BuiltInTarget target)
        {
            var vrcfallback = CreateFoldout(target, "VRChat Fallback");
            if (!vrcfallback) return;
            _context.AddProperty("Mode", new EnumField(VRCFallbackTags.ShaderMode.Opaque) { value = target.vrcFallbackTags.mode }, (evt) =>
            {
                if (Equals(target.vrcFallbackTags.mode, evt.newValue))
                    return;

                _registerUndo("Change Fallback Mode");
                target.vrcFallbackTags.mode = (VRCFallbackTags.ShaderMode)evt.newValue;
                _onChange();
            });
            _context.AddProperty("Type", new EnumField(VRCFallbackTags.ShaderType.Standard) { value = target.vrcFallbackTags.type }, (evt) =>
            {
                if (Equals(target.vrcFallbackTags.type, evt.newValue))
                    return;

                _registerUndo("Change Fallback Type");
                target.vrcFallbackTags.type = (VRCFallbackTags.ShaderType)evt.newValue;
                _onChange();
            });
            _context.AddProperty("Double-Sided", new Toggle() { value = target.vrcFallbackTags.doubleSided }, (evt) =>
            {
                if (Equals(target.vrcFallbackTags.doubleSided, evt.newValue))
                    return;

                _registerUndo("Change Fallback Double-Sided");
                target.vrcFallbackTags.doubleSided = evt.newValue;
                _onChange();
            });
        }

        private void DrawLitProperties(BuiltInLitSubTarget subTarget, BuiltInTarget target)
        {
            var foldout = CreateFoldout(target, "Surface Options");
            if (!foldout) return;

            _context.AddProperty("Fragment Normal Space", new EnumField(NormalDropOffSpace.Tangent) { value = subTarget.normalDropOffSpace }, (evt) =>
            {
                if (Equals(subTarget.normalDropOffSpace, evt.newValue))
                    return;

                _registerUndo("Change Fragment Normal Space");
                subTarget.normalDropOffSpace = (NormalDropOffSpace)evt.newValue;
                _onChange();
            });


            _context.AddProperty("Flat Lit", new Toggle() { value = subTarget.flatLit }, (evt) =>
            {
                OnPropertyChangeValidate(ref subTarget.flatLit, evt, "Change flatLit");
            });

            _context.AddProperty("Specular", new Toggle() { value = subTarget.specular }, (evt) =>
            {
                OnPropertyChangeValidate(ref subTarget.specular, evt, "Change specular");
            });

            _context.AddProperty("Allow Surface Override", new Toggle() { value = subTarget.surfaceOverride }, (evt) =>
            {
                OnPropertyChangeValidate(ref subTarget.surfaceOverride, evt, "Change surfaceOverride");
            });
            _context.AddProperty("Bakery Mono SH", new Toggle() { value = subTarget.bakeryMonoSH }, (evt) =>
            {
                OnPropertyChangeValidate(ref subTarget.bakeryMonoSH, evt, "Change bakeryMonoSH");
            });
            _context.AddProperty("Bicubic Lightmap", new Toggle() { value = subTarget.bicubicLightmap }, (evt) =>
            {
                OnPropertyChangeValidate(ref subTarget.bicubicLightmap, evt, "Change bicubicLightmap");
            });
            _context.AddProperty("Lightmapped Specular", new Toggle() { value = subTarget.lightmappedSpecular }, (evt) =>
            {
                OnPropertyChangeValidate(ref subTarget.lightmappedSpecular, evt, "Change lightmappedSpecular");
            });
            _context.AddProperty("Non-Linear Lightmap SH", new Toggle() { value = subTarget.nonLinearLightMapSH }, (evt) =>
            {
                OnPropertyChangeValidate(ref subTarget.nonLinearLightMapSH, evt, "Change nonLinearLightMapSH");
            });
            _context.AddProperty("Non-Linear Lightprobe SH", new Toggle() { value = subTarget.nonLinearLightProbeSH }, (evt) =>
            {
                OnPropertyChangeValidate(ref subTarget.nonLinearLightProbeSH, evt, "Change nonLinearLightProbeSH");
            });
            _context.AddProperty("GSAA", new Toggle() { value = subTarget.gsaa }, (evt) =>
            {
                OnPropertyChangeValidate(ref subTarget.gsaa, evt, "Change gsaa");
            });
            _context.AddProperty("Anisotropy", new Toggle() { value = subTarget.anisotropy }, (evt) =>
            {
                OnPropertyChangeValidate(ref subTarget.anisotropy, evt, "Change anisotropy");
            });


            _context.AddProperty("SSR", new Toggle() { value = subTarget.screenSpaceReflections }, (evt) =>
            {
                OnPropertyChangeValidate(ref subTarget.screenSpaceReflections, evt, "Change screenSpaceReflections");
            });

            if (subTarget.screenSpaceReflections && !target.grabPass)
            {
                _context.AddHelpBox(UnityEditor.MessageType.Error, "Screen-Space Reflections require Grab Pass enabled");
            }
        }
        
        private void AddSpace()
        {
            _context.Add(new Label());
        }

        private void OnPropertyChangeValidate<T>(ref T toggleValue, ChangeEvent<T> evt, string undo)
        {
            if (Equals(toggleValue, evt.newValue))
                return;

            _registerUndo(undo);
            toggleValue = evt.newValue;
            _onChange();
        }
        private void OnPropertyChangeValidateEnum<T>(ref T toggleValue, ChangeEvent<Enum> evt, string undo) where T : Enum
        {
            if (Equals(toggleValue, evt.newValue))
                return;

            _registerUndo(undo);
            toggleValue = (T)evt.newValue;
            _onChange();
        }

        private bool CreateFoldout(BuiltInTarget target, string name)
        {
            int index = target.foldoutStates.FindIndex(x => x.name == name);
            if (index < 0)
            {
                index = target.foldoutStates.Count();
                target.foldoutStates.Add(new BuiltInTarget.FoldoutState(name));
            }
            bool initialValue = target.foldoutStates[index].value;
            var label = new Label();
            label.style.backgroundColor = new Color(0.125f, 0.125f, 0.125f);
            label.style.height = 1;
            var f = new Foldout
            {
                text = name,
                value = initialValue
            };
            _context.Add(label);
            f.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            _context.Add(f);
            f.RegisterValueChangedCallback((evt) => { target.foldoutStates[index].SetValue(evt.newValue); _onChange();});
            return f.value;
        }
    }
}