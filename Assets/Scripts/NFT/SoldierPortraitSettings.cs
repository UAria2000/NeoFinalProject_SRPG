using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SoldierPortraitSettings", menuName = "Marketplace/Portrait Settings")]
public class SoldierPortraitSettings : ScriptableObject
{
    [System.Serializable]
    public struct PortraitEntry
    {
        public string viewDefinitionName; // RosterUnitSaveData의 unitViewDefinitionName과 일치해야 함
        public Sprite portrait;           // 표시될 초상화 스프라이트
    }

    public List<PortraitEntry> portraitList = new List<PortraitEntry>();

    // 빠른 검색을 위한 딕셔너리 변환
    private Dictionary<string, Sprite> _portraitDict;

    public Sprite GetPortrait(string viewName)
    {
        if (_portraitDict == null)
        {
            _portraitDict = new Dictionary<string, Sprite>();
            foreach (var entry in portraitList)
            {
                if (!string.IsNullOrEmpty(entry.viewDefinitionName) && !_portraitDict.ContainsKey(entry.viewDefinitionName))
                    _portraitDict.Add(entry.viewDefinitionName, entry.portrait);
            }
        }

        if (_portraitDict.TryGetValue(viewName, out Sprite sprite))
            return sprite;

        return null; // 못 찾을 경우 기본 이미지 혹은 null
    }
}