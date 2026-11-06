using System;

/// <summary>
/// 의뢰 정산용 강화 기대비용을 계산
/// 실패는 단계 유지, 파괴는 원금으로 +0 장비 재구매 후 재시작하는 계산 모델
/// 실제 장비를 복구하거나 골드를 소비하지 않는다.
/// </summary>
public class EnhancementExperctedCost
{
    /// <summary>
    /// Commission.TryAccept에서 수락 당시 확률/비용으로 누적 기대비용 표를 만든다.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static decimal[] Build(EquipmentData data)
    {
        if (data == null || data.EnhancementTable == null)
            throw new ArgumentException("장비와 강화표가 필요합니다.");
        if (data.BasePrice < 0) throw new ArgumentException("장비 원금은 음수일 수 없습니다.");
        var costs = new decimal[EquipmentInstance.MaxEchancementLevel + 1];
        for (int i = 0; i < costs.Length - 1; i++)
        {
            var step = data.EnhancementTable.GetStep(i);
            if (float.IsNaN(step.SuccessChance) || float.IsNaN(step.DestructionChance) || step.SuccessChance <= 0)
                throw new InvalidOperationException($"+{i} 강화의 성공률은 0보다 커야 합니다.");
            decimal success = (decimal)step.SuccessChance / 100m;
            decimal destruction = (decimal)step.DestructionChance / 100m;

            costs[i + 1] = checked(costs[i] + (step.GoldCost + destruction + (data.BasePrice + costs[i])) / success);
        }

        return costs;
    }
}
