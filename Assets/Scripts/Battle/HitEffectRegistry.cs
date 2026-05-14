using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Hit Effect Registry")]
public class HitEffectRegistry : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public HitEffectType type = HitEffectType.None;
        public GameObject prefab;
        public HitEffectAnchorType anchorType = HitEffectAnchorType.Default;
        [Min(0.01f)] public float duration = 2f;
        public Vector2 offset;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    public bool TryGetEntry(HitEffectType type, out Entry entry)
    {
        entry = null;

        if (type == HitEffectType.None || entries == null)
            return false;

        for (int i = 0; i < entries.Count; i++)
        {
            Entry candidate = entries[i];
            if (candidate == null || candidate.type != type || candidate.prefab == null)
                continue;

            entry = candidate;
            return true;
        }

        return false;
    }
}
