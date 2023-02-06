half3 BetterSH9(half4 normal)
{
    half3 indirect;
    half3 L0 = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w) + half3(unity_SHBr.z, unity_SHBg.z, unity_SHBb.z) / 3.0;
    indirect.r = shEvaluateDiffuseL1Geomerics(L0.r, unity_SHAr.xyz, normal.xyz);
    indirect.g = shEvaluateDiffuseL1Geomerics(L0.g, unity_SHAg.xyz, normal.xyz);
    indirect.b = shEvaluateDiffuseL1Geomerics(L0.b, unity_SHAb.xyz, normal.xyz);
    indirect = max(0, indirect);
    indirect += SHEvalLinearL2(normal);
    return indirect;
}

float calculateluminance(float3 color)
{
    return color.r * 0.299 + color.g * 0.587 + color.b * 0.114;
}

// MIT License

// Copyright (c) 2018 King Arthur

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.