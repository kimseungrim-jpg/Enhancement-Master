using System;
using UnityEngine;

/// <summary>
/// 강화 결과를 나열한 열거형
/// </summary>
public enum EnhancementResult
{
    NotAttempted,   //이미 파괴된 장비 혹은 강화 최대치인 상태라 강화 시도 x
    Success,    //성공
    Failure,    //실패
    Destroyed,  //파괴됨
    InsufficientGold //골드 불충분으로 강화 시도 x
}

/// <summary>
/// 강화표로 결과를 판정하고 전달받은 장비 한 개의 상태를 변경
/// 강화 가능한 장비와 설정을 확인하고 비용 지불에 성공한 경우에만 판정
/// </summary>
public sealed class EnhancementService
{
    /// <summary>
    /// EnhancementTestUI의 강화 버튼 함수에서 호출
    /// 강화할 수 있으면 현재 단계의 설정으로 판정하고 결과를 반영
    /// </summary>
    public EnhancementResult TryEnhance(EquipmentInstance equipment, EnhancementTable table, GoldWallet wallet)
    {
        if (equipment == null)
        {
            throw new ArgumentNullException(nameof(equipment));
        }

        if (table == null)
        {
            throw new ArgumentNullException(nameof(table));
        }

        if (wallet == null)
        {
            throw new ArgumentNullException(nameof(wallet));
        }

        //강화 불가는 확률 판정에 따른 일반 실패와 구분
        if(!equipment.CanEnhance)
            return EnhancementResult.NotAttempted;

        EnhancemenetStepData step = table.GetStep(equipment.EnhacementLevel);

        //골드가 부족하면 장비 상태를 변경하거나 난수를 뽑지 않는다.
        if (!wallet.TrySpend(step.GoldCost))
            return EnhancementResult.InsufficientGold;

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
