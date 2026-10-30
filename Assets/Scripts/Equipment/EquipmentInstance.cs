using System;
using UnityEngine;
using UnityEngine.Experimental.AI;

/// <summary>
/// 장비 한 개의 프리셋 참조, 현재 강화 단계와 파괴 여부를 관리
/// 같은 프리셋을 사용하는 장비도 객체를 따로 만들면 각자의 강화 상태를 가진다.
/// </summary>
public class EquipmentInstance
{
    public const int MaxEchancementLevel = 25;

    public EquipmentData Data {get;}
    public int EnhacementLevel { get; private set; }
    public bool IsDestroyed { get; private set; }

    public bool CanEnhance => !IsDestroyed && EnhacementLevel < MaxEchancementLevel;

    /// <summary>
    /// new EquipmentInstance로 장비 객체를 만들 때 프리셋과 시작 강화 단계를 설정
    /// 시작 단계를 생략하면 파괴되지 않은 +0 장비로 생성
    /// </summary>
    public EquipmentInstance(EquipmentData data, int enhancementLevel = 0)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data), "장비 프리셋이 필요합니다.");
        }

        if (enhancementLevel < 0 || enhancementLevel > MaxEchancementLevel)
        {
            throw new ArgumentOutOfRangeException(nameof(enhancementLevel), enhancementLevel,
                $"강화 단계는 0부터 {MaxEchancementLevel}까지 설정할 수 있습니다.");
        }

        Data = data;
        EnhacementLevel = enhancementLevel;
        IsDestroyed = false;
    }

    /// <summary>
    /// 강화 단계를 1올리고, 실제로 올렸는지 반환
    /// 성공 결과를 반영하는 함수, 현재는 연결된 호출 없음
    /// </summary>
    public bool TryIncreaseEnhancementLevel()
    {
        if (!CanEnhance)
        {
            return false;
        }

        EnhacementLevel++;
        return true;
    }

    /// <summary>
    /// 장비를 파괴 상태로 변경
    /// 파괴 결과를 반영하는 함수, 현재는 연결된 호출 없음
    /// </summary>
    public void MarkDestroyed()
    {
        IsDestroyed = true;
    }
}
