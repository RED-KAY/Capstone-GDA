#ifndef VAT_INSTANCING_INCLUDED
#define VAT_INSTANCING_INCLUDED

// compile instanced + non-instanced variants for this pass
//#pragma multi_compile_instancing

// per-instance property used by Graphics.DrawMeshInstanced + MPB
UNITY_INSTANCING_BUFFER_START(PerInstance)
    UNITY_DEFINE_INSTANCED_PROP(float, _AnimIndex)
UNITY_INSTANCING_BUFFER_END(PerInstance)

// fallback so NON-INSTANCED variant (no INSTANCING_ON) also compiles
#if !defined(UNITY_INSTANCING_ENABLED) && !defined(UNITY_DOTS_INSTANCING_ENABLED) && !defined(UNITY_ANY_INSTANCING_ENABLED)
float _AnimIndex;
#endif

#endif // VAT_INSTANCING_INCLUDED
