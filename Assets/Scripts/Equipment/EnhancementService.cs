using System;
using UnityEngine;

/// <summary>
/// 강화 결과를 나열한 열거형
/// </summary>
public enum EnhancementResult
{
    NotAttempted,
    Success,
    Failure,
    Destroyed
}

/// <summary>
/// 강화표로 결과를 판정하고 전달받은 장비 한 개의 상태를 변경
/// 현재는 확률과 상태 변경만 처리하며 골드를 차감하지 않음
/// </summary>
public sealed class EnhancementService
{
    /// <summary>
    /// EnhancementTestUI의 강화 버튼 함수에서 호출
    /// 강화할 수 있으면 현재 단계의 설정으로 판정하고 결과를 반영
    /// </summary>
    public EnhancementResult TryEnhance(EquipmentInstance equipment, EnhancementTable table)
    {
        if (equipment == null)
        {
            throw new ArgumentNullException(nameof(equipment));
        }

        if (table == null)
        {
            throw new ArgumentNullException(nameof(table));
        }

        //강화 불가는 확률 판정에 따른 일반 실패와 구분
        if(!equipment.CanEnhance)
            return EnhancementResult.NotAttempted;

        EnhancemenetStepData step = table.GetStep(equipment.EnhacementLevel);

        EnhancementResult result = RollResult(step);

        ApplyResult(equipment, result);

        return result;
    }

    /// <summary>
    /// TryEnhance에서 설정을 조회한 뒤 호출하여 이번 시도의 결과를 결정
    /// </summary>
    private EnhancementResult RollResult(EnhancemenetStepData step)
    {
        //정수  Random.Range의 상한은 제외, 소수 확률도 비교할 수 있도록
        // 0~999999를 0이상 100미만의 값으로 바꾼다 (간격 0.0001)
        float roll = UnityEngine.Random.Range(0, 1000000) / 10000f;

        if(roll < step.SuccessChance)
            return EnhancementResult.Success;

        if (roll >= 100f - step.DestructionChance)
            return EnhancementResult.Destroyed;

        return EnhancementResult.Failure;
    }

    /// <summary>
    /// TryEnhance에서 판정 직후 호출하여 장비에 결과를 반영
    /// 일반 실패는 강화 단계와 파괴 상태를 그대로 유지
    /// </summary>
    private void ApplyResult(EquipmentInstance equipment, EnhancementResult result)
    {
        switch (result)
        {
            case EnhancementResult.Success:
                equipment.TryIncreaseEnhancementLevel();
                break;

            case EnhancementResult.Destroyed:
                equipment.MarkDestroyed();
                break;
        }
    }
}
