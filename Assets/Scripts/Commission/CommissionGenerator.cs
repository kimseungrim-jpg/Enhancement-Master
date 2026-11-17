using System;
using System.Collections.Generic;
using Random = UnityEngine.Random;

/// <summary>
/// 후보 장비에서 하나를 뽑아 시작 단계, 목표 단계, 선금이 정해진 의뢰 한 건을 만든다.
/// </summary>
public class CommissionGenerator
{
    public const int MaxCommissionTargetLevel = 23;

    private readonly List<EquipmentData> candidates;
    private readonly int maxStart;
    private readonly int maxIncrease;
    private const decimal AdvanceRate = 1.10m;

    /// <summary>
    /// 의뢰 UI를 초기화할 때 후보 목록과 강화 단계 범위를 확인한다.
    /// </summary>
    public CommissionGenerator(IReadOnlyList<EquipmentData> equipment, int maxStart, int maxIncrease)
    {
        if (equipment == null || equipment.Count == 0)
            throw new ArgumentException("의뢰 후보 장비를 하나 이상 등록하세요.");
        if (maxStart < 0 || maxStart >= MaxCommissionTargetLevel)
            throw new ArgumentOutOfRangeException(nameof(maxStart),
                $"의뢰 시작 단계 상한은 0~{MaxCommissionTargetLevel - 1}로 지정하세요.");

        if (maxIncrease < 1 || maxIncrease > MaxCommissionTargetLevel)
            throw new ArgumentOutOfRangeException(nameof(maxIncrease),
                $"의뢰 목표 상승 폭은 1~{MaxCommissionTargetLevel}으로 지정하세요.");

        candidates = new List<EquipmentData>(equipment.Count);
        var ids = new HashSet<string>();
        foreach (var item in equipment)
        {
            if (item == null || item.EnhancementTable == null || string.IsNullOrWhiteSpace(item.EquipmentId))
                throw new ArgumentException("후보 장비의 장비 ID와 강화표를 확인하세요.");
            if (!ids.Add(item.EquipmentId))
                throw new ArgumentException($"의뢰 후보 장비 ID가 중복됩니다: {item.EquipmentId}");
            candidates.Add(item);
        }
        this.maxStart = maxStart;
        this.maxIncrease = maxIncrease;
    }

    /// <summary>
    /// 월요일 게시 또는 새로고침 때 카드 한 장의 조건만 만든다. 장비와 골드는 수락할 때 지급한다.
    /// </summary>
    public Commission Generate()
    {
        int equipmentIndex = Random.Range(0, candidates.Count);
        var item = candidates[equipmentIndex];
        int start = Random.Range(0, maxStart + 1);
        int upper = Math.Min(start + maxIncrease, MaxCommissionTargetLevel);
        int target = Random.Range(start + 1, upper + 1);

        decimal[] costs = EnhancementExperctedCost.Build(item);
        decimal expected = costs[target] - costs[start];
        decimal roundedGold = decimal.Round(expected * AdvanceRate, 0, MidpointRounding.AwayFromZero);

        if (roundedGold > long.MaxValue)
        {
            throw new InvalidOperationException(
                $"{item.name}: +{start} → +{target} 선금이 저장 한도를 넘습니다. " +
                $"계산된 선금: {roundedGold:N0}G / 한도: {long.MaxValue:N0}G");
        }

        return new Commission(item, start, target, (long)roundedGold);
    }
}
