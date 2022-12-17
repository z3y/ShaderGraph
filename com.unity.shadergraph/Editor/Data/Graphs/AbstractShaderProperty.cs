using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    public abstract class AbstractShaderProperty : ShaderInput
    {
        public abstract PropertyType propertyType { get; }

        internal override ConcreteSlotValueType concreteShaderValueType => propertyType.ToConcreteShaderValueType();

        [SerializeField]
        Precision m_Precision = Precision.Inherit;
        
        [SerializeField]
        private bool m_GPUInstanced = false;
        
        [SerializeField]
        private string m_Attributes = string.Empty;

        public string attributes
        {
            get { return m_Attributes; }
            set => m_Attributes = value;
        }

        public bool gpuInstanced
        {
            get { return m_GPUInstanced; }
            set { m_GPUInstanced = value; }
        }

        ConcretePrecision m_ConcretePrecision = ConcretePrecision.Float;

        internal Precision precision
        {
            get => m_Precision;
            set => m_Precision = value;
        }

        public ConcretePrecision concretePrecision => m_ConcretePrecision;

        internal void ValidateConcretePrecision(ConcretePrecision graphPrecision)
        {
            m_ConcretePrecision = (precision == Precision.Inherit) ? graphPrecision : precision.ToConcrete();
        }

        internal abstract bool isBatchable { get; }

        [SerializeField]
        bool m_Hidden = false;

        public bool hidden
        {
            get => m_Hidden;
            set => m_Hidden = value;
        }

        // hijack the hideTagString to add attributes to all properties that already implement this
        internal string hideTagString
        {
            get
            {
                var att = attributes;
                if (hidden) att += "[HideInInspector]";
                return att;
            }
        }

        internal virtual string GetPropertyBlockString()
        {
            return string.Empty;
        }

        internal virtual string GetPropertyDeclarationString(string delimiter = ";")
        {
            SlotValueType type = ConcreteSlotValueType.Vector4.ToSlotValueType();
            return $"{concreteShaderValueType.ToShaderString(concretePrecision.ToShaderString())} {referenceName}{delimiter}";
        }

        internal virtual string GetPropertyAsArgumentString()
        {
            return GetPropertyDeclarationString(string.Empty);
        }
        
        internal abstract AbstractMaterialNode ToConcreteNode();
        internal abstract PreviewProperty GetPreviewMaterialProperty();
        internal virtual bool isGpuInstanceable => false;
    }
    
    [Serializable]
    public abstract class AbstractShaderProperty<T> : AbstractShaderProperty
    {
        [SerializeField]
        T m_Value;

        public virtual T value
        {
            get => m_Value;
            set => m_Value = value;
        }
    }
}
