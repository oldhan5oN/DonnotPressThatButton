// 太空舱模型材质重建工具
// 背景：FBX/Max 导入时第三方渲染器材质无法被 Unity 翻译，导致所有材质丢失颜色与贴图（白模）。
// 本工具按"贴图命名规律 + 材质语义"在 Unity 端重建 material/ 文件夹下的 URP Lit 材质。
// 用法：Unity 菜单栏 Tools/太空舱/重建材质
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class SpaceCapsuleMaterialRebuilder
{
    // material/ 与 textures/ 所在目录
    const string MatDir = "Assets/Res/太空舱/material";
    const string TexDir = "Assets/Res/太空舱/textures";

    // 贴图缓存：文件名(不含扩展名) -> Texture2D
    static Dictionary<string, Texture2D> _texByName;

    [MenuItem("Tools/太空舱/重建材质")]
    public static void Rebuild()
    {
        BuildTextureCache();

        int ok = 0, miss = 0;

        // ---------- PBR 组：按命名规律绑定贴图 ----------
        // Fabric：布料，basecolor + normal
        ok += SetupPBR("Fabric", baseMap: "Fabric_dots_basecolor", normalMap: "Fabric_dots_normal",
                       metallic: 0f, smoothness: 0.25f);
        // Fabric_orange：同一布料贴图 + 橙色染色
        ok += SetupPBR("Fabric_orange", baseMap: "Fabric_dots_basecolor", normalMap: "Fabric_dots_normal",
                       metallic: 0f, smoothness: 0.25f, baseColor: Hex("E8731E"));
        // Floor_01：地板 basecolor
        ok += SetupPBR("Floor_01", baseMap: "Floor_01_Base_Color", normalMap: null,
                       metallic: 0.1f, smoothness: 0.4f);
        // Rubber colbadot：橡胶防滑点，无 basecolor，只有 normal + height(视差)
        ok += SetupPBR("Rubber colbadot", baseMap: null, normalMap: "Rubber_Coldabot_normal",
                       parallaxMap: "Rubber_Coldabot_height", metallic: 0f, smoothness: 0.2f,
                       baseColor: Hex("1A1A1A"));
        // Rubber_plain：纯橡胶，只给深色，无花纹
        ok += SetupFlat("Rubber_plain", Hex("1C1C1C"), metallic: 0f, smoothness: 0.15f);
        // fingerprint light：指纹自发光贴图
        ok += SetupEmissive("fingerprint light", emissionMap: "emissive_fingerprint",
                            emissionColor: Hex("9FD8FF"), intensity: 1.5f, baseColor: Hex("202020"));

        // ---------- 纯色组：电线 ----------
        ok += SetupFlat("Black wire",  Hex("0A0A0A"), 0f, 0.3f);
        ok += SetupFlat("White wire",  Hex("E8E8E8"), 0f, 0.3f);
        ok += SetupFlat("Orange wire", Hex("E86A12"), 0f, 0.3f);

        // ---------- 纯色组：涂层 ----------
        ok += SetupFlat("Blue coating",  Hex("2E5C8A"), 0f, 0.5f);
        ok += SetupFlat("Gray coating",  Hex("8A8A8A"), 0f, 0.5f);
        ok += SetupFlat("White coating", Hex("DADADA"), 0f, 0.5f);
        ok += SetupFlat("black coating_", Hex("141414"), 0f, 0.5f);

        // ---------- 纯色组：灰度 / 金属 ----------
        ok += SetupFlat("Gray",      Hex("808080"), 0.2f, 0.4f);
        ok += SetupFlat("Gray_dark", Hex("3C3C3C"), 0.2f, 0.4f);
        ok += SetupFlat("white_",    Hex("E0E0E0"), 0.1f, 0.4f);
        ok += SetupFlat("Metal",     Hex("9A9A9A"), 1f, 0.6f);
        ok += SetupFlat("Metal.001", Hex("9A9A9A"), 1f, 0.6f);
        ok += SetupFlat("red metal", Hex("8A2020"), 1f, 0.6f);
        ok += SetupFlat("Orange Paint", Hex("E2701A"), 0f, 0.5f);

        // ---------- 自发光组：灯 / 屏幕 ----------
        ok += SetupEmissive("Little Green light", null, Hex("30FF50"), 3f, Hex("0A2010"));
        ok += SetupEmissive("Little red light",   null, Hex("FF2828"), 3f, Hex("200A0A"));
        ok += SetupEmissive("Fluorescent_01", null, Hex("FFFFFF"), 4f, Hex("FFFFFF"));
        ok += SetupEmissive("Fluorescent_02", null, Hex("FFFFFF"), 4f, Hex("FFFFFF"));
        ok += SetupEmissive("Fluorescent_03", null, Hex("FFFFFF"), 4f, Hex("FFFFFF"));
        ok += SetupEmissive("LCD screen", null, Hex("1E3A5F"), 1.2f, Hex("0A0A0A"));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[太空舱] 材质重建完成：成功处理 {ok} 个材质。缺失贴图计数 {miss}。" +
                  "贴花/标识类贴图(caution/serials/71/PILOT 等)需手动贴到对应部件。");
    }

    // ---------------- 内部方法 ----------------

    static int SetupPBR(string matName, string baseMap, string normalMap, float metallic, float smoothness,
                        string parallaxMap = null, Color? baseColor = null)
    {
        var mat = LoadMat(matName);
        if (mat == null) return 0;

        if (baseMap != null) mat.SetTexture("_BaseMap", LoadTex(baseMap));
        mat.SetColor("_BaseColor", baseColor ?? Color.white);
        mat.SetColor("_Color", baseColor ?? Color.white);
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Smoothness", smoothness);

        if (normalMap != null)
        {
            EnsureNormalMapType(normalMap);
            mat.SetTexture("_BumpMap", LoadTex(normalMap));
            mat.SetFloat("_BumpScale", 1f);
            mat.EnableKeyword("_NORMALMAP");
        }
        if (parallaxMap != null)
        {
            mat.SetTexture("_ParallaxMap", LoadTex(parallaxMap));
            mat.SetFloat("_Parallax", 0.02f);
            mat.EnableKeyword("_PARALLAXMAP");
        }
        EditorUtility.SetDirty(mat);
        return 1;
    }

    static int SetupFlat(string matName, Color color, float metallic, float smoothness)
    {
        var mat = LoadMat(matName);
        if (mat == null) return 0;
        mat.SetTexture("_BaseMap", null);
        mat.SetColor("_BaseColor", color);
        mat.SetColor("_Color", color);
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(mat);
        return 1;
    }

    static int SetupEmissive(string matName, string emissionMap, Color emissionColor, float intensity, Color baseColor)
    {
        var mat = LoadMat(matName);
        if (mat == null) return 0;
        mat.SetColor("_BaseColor", baseColor);
        mat.SetColor("_Color", baseColor);
        mat.SetFloat("_Metallic", 0f);
        mat.SetFloat("_Smoothness", 0.3f);

        if (emissionMap != null) mat.SetTexture("_EmissionMap", LoadTex(emissionMap));
        mat.SetColor("_EmissionColor", emissionColor * intensity);
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(mat);
        return 1;
    }

    static Material LoadMat(string name)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/{name}.mat");
        if (mat == null) Debug.LogWarning($"[太空舱] 找不到材质：{name}.mat");
        return mat;
    }

    static Texture2D LoadTex(string nameNoExt)
    {
        if (_texByName.TryGetValue(nameNoExt, out var t)) return t;
        Debug.LogWarning($"[太空舱] 找不到贴图：{nameNoExt}");
        return null;
    }

    static void BuildTextureCache()
    {
        _texByName = new Dictionary<string, Texture2D>();
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TexDir }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null) _texByName[System.IO.Path.GetFileNameWithoutExtension(path)] = tex;
        }
    }

    // 把法线贴图的导入类型设为 NormalMap，否则法线方向错误
    static void EnsureNormalMapType(string nameNoExt)
    {
        if (!_texByName.TryGetValue(nameNoExt, out var tex)) return;
        var path = AssetDatabase.GetAssetPath(tex);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.NormalMap)
        {
            importer.textureType = TextureImporterType.NormalMap;
            importer.SaveAndReimport();
        }
    }

    // 16进制(sRGB) -> Color
    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        return c;
    }
}
