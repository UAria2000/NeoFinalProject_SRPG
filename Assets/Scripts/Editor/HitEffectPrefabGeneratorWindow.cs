#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class HitEffectPrefabGeneratorWindow : EditorWindow
{
    private const string SourceFolder = "Assets/Image/Effects/Handoff";
    private const string PrefabFolder = "Assets/Prefabs/Effects/Common";
    private const string RegistryPath = "Assets/Resources/Battle/HitEffectRegistry.asset";

    [MenuItem("Tools/Battle/Effects/Generate Rich Hit Effect Prefabs")]
    public static void Generate()
    {
        EnsureFolder("Assets/Image/Effects");
        EnsureFolder(SourceFolder);
        EnsureFolder(PrefabFolder);
        EnsureFolder("Assets/Resources/Battle");

        ImportSprites();

        Dictionary<HitEffectType, GameObject> prefabs = new Dictionary<HitEffectType, GameObject>();
        prefabs[HitEffectType.Slashing] = CreateSlashingEffectPrefab();

        prefabs[HitEffectType.SlashingBlood] = CreateEffectPrefab(
            HitEffectType.SlashingBlood,
            "Common_SlashingBlood_HitEffect",
            new[] { "찍는 느낌2" },
            new Color(1f, 0f, 0.02f, 1f),
            new Color(0.45f, 0f, 0.02f, 0f),
            1.18f,
            430f,
            true);

        prefabs[HitEffectType.Piercing] = CreateEffectPrefab(
            HitEffectType.Piercing,
            "Common_Piercing_HitEffect",
            new[] { "피어싱2" },
            new Color(0.72f, 0.35f, 1f, 1f),
            new Color(0.18f, 0.04f, 1f, 0f),
            1.05f,
            480f,
            true);

        prefabs[HitEffectType.Blunt] = CreateEffectPrefab(
            HitEffectType.Blunt,
            "Common_Blunt_HitEffect",
            new[] { "타격2" },
            new Color(1f, 0.82f, 0.24f, 1f),
            new Color(1f, 0.42f, 0.04f, 0f),
            1.12f,
            470f,
            false);

        prefabs[HitEffectType.Blessing] = CreateEffectPrefab(
            HitEffectType.Blessing,
            "Common_Blessing_HitEffect",
            new[] { "블레싱1" },
            new Color(1f, 0.96f, 0.55f, 1f),
            new Color(1f, 0.92f, 0.2f, 0f),
            1.45f,
            440f,
            false);

        prefabs[HitEffectType.ArcaneMagic] = CreateEffectPrefab(
            HitEffectType.ArcaneMagic,
            "Common_ArcaneMagic_HitEffect",
            new[] { "마법2" },
            new Color(0.52f, 0.22f, 1f, 1f),
            new Color(0.02f, 0.28f, 1f, 0f),
            1.2f,
            500f,
            true);

        prefabs[HitEffectType.FireMagic] = CreateEffectPrefab(
            HitEffectType.FireMagic,
            "Common_FireMagic_HitEffect",
            new[] { "붉은계열 마법3" },
            new Color(1f, 0.24f, 0.04f, 1f),
            new Color(1f, 0.78f, 0.1f, 0f),
            1.22f,
            530f,
            true);

        prefabs[HitEffectType.HolyMagic] = CreateEffectPrefab(
            HitEffectType.HolyMagic,
            "Common_HolyMagic_HitEffect",
            new[] { "신성마법1" },
            new Color(1f, 0.94f, 0.52f, 1f),
            new Color(1f, 1f, 1f, 0f),
            1.28f,
            500f,
            true);

        prefabs[HitEffectType.Shield] = CreateEffectPrefab(
            HitEffectType.Shield,
            "Common_Shield_HitEffect",
            new[] { "보라색 보호막(아군 버프 및 보호막 부여)" },
            new Color(0.82f, 0.2f, 1f, 1f),
            new Color(0.38f, 0.02f, 0.78f, 0f),
            1.55f,
            430f,
            false);

        UpdateRegistry(prefabs);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[HitEffectPrefabGenerator] Rich hit effect prefabs generated.");
    }

    private static GameObject CreateEffectPrefab(
        HitEffectType type,
        string prefabName,
        string[] spriteNames,
        Color primaryColor,
        Color fadeColor,
        float duration,
        float baseSize,
        bool diagonal)
    {
        GameObject root = new GameObject(prefabName, typeof(RectTransform), typeof(CanvasGroup), typeof(BattleRichHitEffectUI));
        root.layer = 5;
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(620f, 420f);
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);

        List<BattleRichHitEffectUI.ImageLayer> layers = new List<BattleRichHitEffectUI.ImageLayer>();
        Sprite[] sprites = LoadSprites(spriteNames);

        if (sprites.Length > 0)
        {
            float startRotation = diagonal ? -7f : 0f;
            float endRotation = diagonal ? 5f : 0f;
            layers.Add(CreateImageLayer(root.transform, "Impact_Main", sprites[0], baseSize, 0f, Vector2.zero, Vector2.zero, primaryColor, fadeColor, startRotation, endRotation, 0f));
            layers.Add(CreateImageLayer(root.transform, "Impact_Glow", sprites[0], baseSize * 1.08f, 0f, Vector2.zero, Vector2.zero, new Color(primaryColor.r, primaryColor.g, primaryColor.b, 0.22f), fadeColor, 0f, 0f, 0f));
        }

        ParticleSystem sparks = CreateParticleSystem(root.transform, "SparkParticles", primaryColor, duration, 34, 34f, diagonal ? 28f : 360f, diagonal ? 10f : 0f);
        ParticleSystem motes = CreateParticleSystem(root.transform, "MoteParticles", new Color(1f, 1f, 1f, 0.85f), duration, 18, 18f, 360f, 0f);
        ParticleSystem embers = CreateParticleSystem(root.transform, "AfterParticles", fadeColor.a <= 0f ? primaryColor : fadeColor, duration, 22, 24f, diagonal ? 52f : 360f, diagonal ? -14f : 0f);

        BattleRichHitEffectUI effect = root.GetComponent<BattleRichHitEffectUI>();
        SerializedObject so = new SerializedObject(effect);
        so.FindProperty("canvasGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
        so.FindProperty("duration").floatValue = duration;
        so.FindProperty("destroyOnComplete").boolValue = true;
        WriteLayers(so.FindProperty("imageLayers"), layers);
        WriteParticles(so.FindProperty("particleSystems"), new[] { sparks, motes, embers });
        so.ApplyModifiedPropertiesWithoutUndo();

        string path = PrefabFolder + "/" + prefabName + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateSlashingEffectPrefab()
    {
        const string prefabName = "Common_Slashing_HitEffect";
        const float duration = 1.25f;
        const float baseSize = 560f;

        GameObject root = new GameObject(prefabName, typeof(RectTransform), typeof(CanvasGroup), typeof(BattleRichHitEffectUI));
        root.layer = 5;
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(620f, 420f);
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);

        Sprite[] sprites = LoadSprites(new[] { "슬래싱2", "슬래싱1", "슬래싱3" });
        Color primaryColor = new Color(1f, 0.06f, 0.04f, 1f);
        Color fadeColor = new Color(1f, 0.03f, 0.02f, 0f);

        List<BattleRichHitEffectUI.ImageLayer> layers = new List<BattleRichHitEffectUI.ImageLayer>();
        if (sprites.Length > 0)
        {
            BattleRichHitEffectUI.ImageLayer main = CreateImageLayer(
                root.transform,
                "Impact_Main_DownSlash",
                sprites[0],
                baseSize,
                210f,
                new Vector2(-28f, 78f),
                new Vector2(22f, -72f),
                primaryColor,
                fadeColor,
                212f,
                198f,
                0f);
            main.startScale = 0.32f;
            main.peakScale = 1.1f;
            main.endScale = 1.28f;
            main.fadeOutAt = 0.46f;
            layers.Add(main);

            BattleRichHitEffectUI.ImageLayer after1 = CreateImageLayer(
                root.transform,
                "Impact_Afterimage_A",
                sprites.Length > 1 ? sprites[1] : sprites[0],
                baseSize * 0.94f,
                206f,
                new Vector2(-42f, 88f),
                new Vector2(10f, -58f),
                new Color(1f, 0.05f, 0.03f, 0.34f),
                fadeColor,
                206f,
                194f,
                0.04f);
            after1.startScale = 0.26f;
            after1.peakScale = 0.98f;
            after1.endScale = 1.18f;
            after1.fadeOutAt = 0.36f;
            layers.Add(after1);

            BattleRichHitEffectUI.ImageLayer after2 = CreateImageLayer(
                root.transform,
                "Impact_Afterimage_B",
                sprites.Length > 2 ? sprites[2] : sprites[0],
                baseSize * 0.86f,
                216f,
                new Vector2(-12f, 58f),
                new Vector2(34f, -84f),
                new Color(1f, 0.1f, 0.06f, 0.24f),
                fadeColor,
                218f,
                202f,
                0.08f);
            after2.startScale = 0.22f;
            after2.peakScale = 0.9f;
            after2.endScale = 1.08f;
            after2.fadeOutAt = 0.32f;
            layers.Add(after2);

            BattleRichHitEffectUI.ImageLayer glow = CreateImageLayer(
                root.transform,
                "Impact_Red_Glow",
                sprites[0],
                baseSize * 1.08f,
                210f,
                new Vector2(-24f, 66f),
                new Vector2(18f, -64f),
                new Color(1f, 0.04f, 0.02f, 0.2f),
                fadeColor,
                210f,
                198f,
                0f);
            glow.startScale = 0.4f;
            glow.peakScale = 1.12f;
            glow.endScale = 1.36f;
            glow.fadeOutAt = 0.28f;
            layers.Add(glow);
        }

        ParticleSystem sparks = CreateParticleSystem(root.transform, "SparkParticles", primaryColor, duration, 42, 340f, 78f, -84f);
        ParticleSystem motes = CreateParticleSystem(root.transform, "MoteParticles", new Color(1f, 0.24f, 0.18f, 0.8f), duration, 24, 180f, 110f, -90f);
        ParticleSystem embers = CreateParticleSystem(root.transform, "AfterParticles", new Color(1f, 0.02f, 0.01f, 0.95f), duration, 30, 240f, 86f, -96f);

        BattleRichHitEffectUI effect = root.GetComponent<BattleRichHitEffectUI>();
        SerializedObject so = new SerializedObject(effect);
        so.FindProperty("canvasGroup").objectReferenceValue = root.GetComponent<CanvasGroup>();
        so.FindProperty("duration").floatValue = duration;
        so.FindProperty("destroyOnComplete").boolValue = true;
        WriteLayers(so.FindProperty("imageLayers"), layers);
        WriteParticles(so.FindProperty("particleSystems"), new[] { sparks, motes, embers });
        so.ApplyModifiedPropertiesWithoutUndo();

        string path = PrefabFolder + "/" + prefabName + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void AddUiParticleLayers(
        Transform parent,
        List<BattleRichHitEffectUI.ImageLayer> layers,
        Sprite[] sprites,
        Color primaryColor,
        Color fadeColor,
        float baseSize,
        bool directional)
    {
        if (sprites == null || sprites.Length == 0)
            return;

        int count = directional ? 14 : 18;
        float baseAngle = directional ? -12f : 0f;
        float spread = directional ? 92f : 360f;

        for (int i = 0; i < count; i++)
        {
            float t = count <= 1 ? 0f : i / (float)(count - 1);
            float angle = baseAngle + Mathf.Lerp(-spread * 0.5f, spread * 0.5f, t);
            if (!directional)
                angle = i * (360f / count) + (i % 2 == 0 ? 9f : -7f);

            float radians = angle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            float distance = Mathf.Lerp(baseSize * 0.24f, baseSize * 0.58f, (i % 5) / 4f);
            Vector2 start = direction * Mathf.Lerp(4f, 18f, (i % 3) / 2f);
            Vector2 end = new Vector2(direction.x * distance, direction.y * distance * 0.72f);
            float size = Mathf.Lerp(baseSize * 0.11f, baseSize * 0.2f, (i % 4) / 3f);
            Color startColor = new Color(primaryColor.r, primaryColor.g, primaryColor.b, Mathf.Lerp(0.38f, 0.72f, (i % 3) / 2f));

            BattleRichHitEffectUI.ImageLayer layer = CreateImageLayer(
                parent,
                "UiParticle_" + i.ToString("00"),
                sprites[i % sprites.Length],
                size,
                angle,
                start,
                end,
                startColor,
                fadeColor,
                angle - 25f,
                angle + 80f,
                Mathf.Lerp(0.03f, 0.18f, (i % 4) / 3f));

            layer.startScale = 0.08f;
            layer.peakScale = Mathf.Lerp(0.38f, 0.68f, (i % 5) / 4f);
            layer.endScale = 0.12f;
            layer.fadeOutAt = Mathf.Lerp(0.34f, 0.58f, (i % 4) / 3f);
            layers.Add(layer);
        }
    }

    private static BattleRichHitEffectUI.ImageLayer CreateImageLayer(
        Transform parent,
        string name,
        Sprite sprite,
        float size,
        float rotation,
        Vector2 startPosition,
        Vector2 endPosition,
        Color startColor,
        Color endColor,
        float startRotation,
        float endRotation,
        float appearAt)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = 5;
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);
        rect.anchoredPosition = startPosition;
        rect.localRotation = Quaternion.Euler(0f, 0f, rotation);

        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = startColor;

        return new BattleRichHitEffectUI.ImageLayer
        {
            graphic = image,
            rectTransform = rect,
            startPosition = startPosition,
            endPosition = endPosition,
            startScale = 0.18f,
            peakScale = 1.08f,
            endScale = 1.45f,
            startRotation = startRotation,
            endRotation = endRotation,
            appearAt = appearAt,
            fadeOutAt = 0.5f,
            startColor = startColor,
            endColor = endColor
        };
    }

    private static ParticleSystem CreateParticleSystem(
        Transform parent,
        string name,
        Color color,
        float duration,
        int burstCount,
        float startSize,
        float arc,
        float rotation)
    {
        GameObject go = new GameObject(name, typeof(ParticleSystem));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);

        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.duration = duration;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, duration * 0.72f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(90f, 260f);
        main.startSize = new ParticleSystem.MinMaxCurve(startSize * 0.25f, startSize);
        main.startRotation = new ParticleSystem.MinMaxCurve(-3.14f, 3.14f);
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = 128;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burstCount) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.arc = arc;
        shape.radius = 10f;

        ParticleSystem.ColorOverLifetimeModule colorLife = ps.colorOverLifetime;
        colorLife.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(color, 0f),
                new GradientColorKey(Color.white, 0.18f),
                new GradientColorKey(color, 1f)
            },
            new[]
            {
                new GradientAlphaKey(color.a, 0f),
                new GradientAlphaKey(color.a * 0.8f, 0.35f),
                new GradientAlphaKey(0f, 1f)
            });
        colorLife.color = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystem.SizeOverLifetimeModule sizeLife = ps.sizeOverLifetime;
        sizeLife.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.25f),
            new Keyframe(0.18f, 1f),
            new Keyframe(1f, 0f));
        sizeLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 120;
        Material particleMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
        if (particleMaterial != null)
            renderer.sharedMaterial = particleMaterial;

        return ps;
    }

    private static Sprite[] LoadSprites(string[] names)
    {
        List<Sprite> sprites = new List<Sprite>();
        for (int i = 0; i < names.Length; i++)
        {
            string path = SourceFolder + "/" + names[i] + ".png";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
                sprites.Add(sprite);
            else
                Debug.LogWarning("[HitEffectPrefabGenerator] Missing sprite: " + path);
        }

        return sprites.ToArray();
    }

    private static void WriteLayers(SerializedProperty property, List<BattleRichHitEffectUI.ImageLayer> layers)
    {
        property.arraySize = layers.Count;
        for (int i = 0; i < layers.Count; i++)
        {
            BattleRichHitEffectUI.ImageLayer layer = layers[i];
            SerializedProperty item = property.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("graphic").objectReferenceValue = layer.graphic;
            item.FindPropertyRelative("rectTransform").objectReferenceValue = layer.rectTransform;
            item.FindPropertyRelative("startPosition").vector2Value = layer.startPosition;
            item.FindPropertyRelative("endPosition").vector2Value = layer.endPosition;
            item.FindPropertyRelative("startScale").floatValue = layer.startScale;
            item.FindPropertyRelative("peakScale").floatValue = layer.peakScale;
            item.FindPropertyRelative("endScale").floatValue = layer.endScale;
            item.FindPropertyRelative("startRotation").floatValue = layer.startRotation;
            item.FindPropertyRelative("endRotation").floatValue = layer.endRotation;
            item.FindPropertyRelative("appearAt").floatValue = layer.appearAt;
            item.FindPropertyRelative("fadeOutAt").floatValue = layer.fadeOutAt;
            item.FindPropertyRelative("startColor").colorValue = layer.startColor;
            item.FindPropertyRelative("endColor").colorValue = layer.endColor;
        }
    }

    private static void WriteParticles(SerializedProperty property, ParticleSystem[] particles)
    {
        property.arraySize = particles.Length;
        for (int i = 0; i < particles.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = particles[i];
    }

    private static void UpdateRegistry(Dictionary<HitEffectType, GameObject> prefabs)
    {
        HitEffectRegistry registry = AssetDatabase.LoadAssetAtPath<HitEffectRegistry>(RegistryPath);
        if (registry == null)
        {
            registry = ScriptableObject.CreateInstance<HitEffectRegistry>();
            AssetDatabase.CreateAsset(registry, RegistryPath);
        }

        SerializedObject so = new SerializedObject(registry);
        SerializedProperty entries = so.FindProperty("entries");
        entries.arraySize = prefabs.Count;

        int index = 0;
        foreach (KeyValuePair<HitEffectType, GameObject> pair in prefabs)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(index++);
            entry.FindPropertyRelative("type").enumValueIndex = (int)pair.Key;
            entry.FindPropertyRelative("prefab").objectReferenceValue = pair.Value;
            entry.FindPropertyRelative("anchorType").enumValueIndex = GetAnchorType(pair.Key);
            entry.FindPropertyRelative("duration").floatValue = GetDuration(pair.Key);
            entry.FindPropertyRelative("offset").vector2Value = Vector2.zero;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(registry);
    }

    private static int GetAnchorType(HitEffectType type)
    {
        if (type == HitEffectType.Blessing || type == HitEffectType.Shield)
            return (int)HitEffectAnchorType.Center;
        if (type == HitEffectType.Blunt)
            return (int)HitEffectAnchorType.Center;
        return (int)HitEffectAnchorType.Default;
    }

    private static float GetDuration(HitEffectType type)
    {
        switch (type)
        {
            case HitEffectType.Blessing:
            case HitEffectType.Shield:
                return 1.55f;
            case HitEffectType.Piercing:
                return 1.05f;
            case HitEffectType.Blunt:
                return 1.12f;
            default:
                return 1.25f;
        }
    }

    private static void ImportSprites()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SourceFolder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                continue;

            bool dirty = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                dirty = true;
            }
            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                dirty = true;
            }
            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                dirty = true;
            }

            if (dirty)
                importer.SaveAndReimport();
        }
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
