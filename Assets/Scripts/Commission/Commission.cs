using NUnit.Framework.Constraints;
using System;

public enum CommissionState { Available, InProgress, Completed, Abandoned }
public enum CommissionAcceptResult { Accepted, NotAvailable, InventoryFull, GoldLimitReached}

/// <summary>
/// 의뢰 한 개의 조건과 진행 상태를 보관하고 수락/제출을 처리
/// 처음 지급한 장비 대신 같은 종류와 목표 단계를 만족한 장비도 제출 가능
/// </summary>
public class Commission
{
    public EquipmentData EquipmentData { get; }
    public int StartingLevel { get; }
    public int TargetLevel { get; }
    public int AdvanceGold { get; }
    public CommissionState State { get; private set; }
    private readonly string requiredEquipmentId;
    private decimal[] expectedCosts;
    private int acceptedBasePrice;

    /// <summary>
    /// CommissionTestUI.Initialize에서 테스트 의뢰를 만들 때 조건을 설정
    /// </summary>
    public Commission(EquipmentData data, int startingLevel, int targetLevel, int advanceGold)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (string.IsNullOrWhiteSpace(data.EquipmentId) || data.EnhancementTable == null)
            throw new ArgumentException("장비 SO에 장비 ID와 강화표를 지정하세요.", nameof(data));
        if (startingLevel < 0 || startingLevel >= EquipmentInstance.MaxEchancementLevel)
            throw new ArgumentOutOfRangeException(nameof(startingLevel));
        if(targetLevel <= startingLevel || targetLevel > EquipmentInstance.MaxEchancementLevel)
            throw new ArgumentOutOfRangeException(nameof(targetLevel));
        if (advanceGold < 0) throw new ArgumentOutOfRangeException(nameof(advanceGold));

        EquipmentData = data;
        requiredEquipmentId = data.EquipmentId;
        StartingLevel = startingLevel;
        TargetLevel = targetLevel;
        AdvanceGold = advanceGold;
        State = CommissionState.Available;
    }

    /// <summary>
    /// CommissionTestUI의 수락 버튼에서 호출
    /// 장비 추가가 가능한 경우에만 선금을 지급하고 진행 중으로 변경
    /// </summary>
    public CommissionAcceptResult TryAccept(EquipmentInventory inventory, GoldWallet wallet)
    {
        if (inventory == null) throw new ArgumentNullException(nameof(inventory));
        if (wallet == null) throw new ArgumentNullException(nameof(wallet));
        if (State != CommissionState.Available) return CommissionAcceptResult.NotAvailable;

        if (AdvanceGold > int.MaxValue - wallet.Gold)
            return CommissionAcceptResult.GoldLimitReached;

        decimal[] costs = EnhancementExperctedCost.Build(EquipmentData);
        EquipmentInstance equipment = new EquipmentInstance(EquipmentData, StartingLevel);
        if (!inventory.TryAdd(equipment)) return CommissionAcceptResult.InventoryFull;

        expectedCosts = costs;
        acceptedBasePrice = EquipmentData.BasePrice;
        wallet.AddGold(AdvanceGold);
        State = CommissionState.InProgress;
        return CommissionAcceptResult.Accepted;
    }

    /// <summary>
    /// 제출 모드에서 슬롯을 누를 때와 최종 제출 직전에 호출
    /// 진행 중인 의뢰이며 장비 종류/강화 단계/보유 여부를 모두 만족하는지 확인
    /// </summary>
    public bool CanSubmit(EquipmentInventory inventory, EquipmentInstance equipment)
    {
        if (inventory == null) throw new ArgumentNullException(nameof(inventory));
        return State == CommissionState.InProgress
            && equipment != null
            && !equipment.IsDestroyed
            && equipment.Data.EquipmentId == requiredEquipmentId
            && equipment.EnhacementLevel >= TargetLevel
            && inventory.FindIndex(equipment) >= 0; 
    }

    /// <summary>
    /// 확인 팝업에서 예를 눌러 호출
    /// 조건을 다시 검색하고 선택한 장비 하나를 회수한 뒤 완료 상태로 변경
    /// </summary>
    public bool TryComplete(EquipmentInventory inventory, EquipmentInstance equipment)
    {
        if(!CanSubmit(inventory, equipment)) return false;

        int slotIndex = inventory.FindIndex(equipment);
        if(!inventory.TryRemove(slotIndex)) return false;

        State = CommissionState.Completed;
        return true;
    }

    /// <summary>
    /// CommissionUI가 포기 장비 선택과 최종 확인 시 호출
    /// null은 명시적으로 선택한 '장비 없이 포기'이며 정상 장비나 흔적은 같은 ID여야 한다.
    /// </summary>
    public bool TryGetAbandonmentQuoto(EquipmentInventory inventory, GoldWallet wallet, EquipmentInstance equipment, out AbandonmentQuset quote)
    {
        quote = null;
        if (State != CommissionState.InProgress) return false;
        if (equipment != null && (inventory.FindIndex(equipment) < 0 || equipment.Data.EquipmentId != requiredEquipmentId))
            return false;
        if(equipment != null && !equipment.IsDestroyed && equipment.EnhacementLevel >= TargetLevel)
            return false;

        decimal amount;
        if (equipment == null || equipment.IsDestroyed)
        {
            amount = acceptedBasePrice + expectedCosts[StartingLevel] + AdvanceGold;
            if (equipment != null) amount -= acceptedBasePrice * 0.1m;
        }
        else
        {
            //누적 비용의 차이는 낮은 단계에서 높은 단계에 처음 도달할 때까지의 기대비용
            //높은 장비를 반환하면 차이가 음수이므로 선금에서 자동으로 공재
            amount = AdvanceGold + expectedCosts[StartingLevel] - expectedCosts[equipment.EnhacementLevel];
        }

        amount = decimal.Round(Math.Max(0m, amount), 0, MidpointRounding.AwayFromZero);
        if (amount > long.MaxValue) return false;
        quote = new AbandonmentQuset((long)amount, wallet);
        return true;
    }

    /// <summary>
    /// CommissionTestUI의 포기 확인에서 호출
    /// 미리보기와 금액/지불 방식이 같은지 재검사한 뒤 장비 회수와 정산을 한 번만 실행
    /// </summary>
    public bool TryAbandon(EquipmentInventory inventory, GoldWallet wallet, EquipmentInstance equipment, AbandonmentQuset approved)
    {
        if(approved == null || !TryGetAbandonmentQuoto(inventory, wallet, equipment, out var current))
            return false;
        if(approved.RecoveryGold != current.RecoveryGold || approved.PayGold != current.PayGold || approved.AddedDebt != current.AddedDebt ||
            approved.AddedDebt != current.AddedDebt || !wallet.CanSettle(current.RecoveryGold))
            return false;

        if (equipment != null && !inventory.TryRemove(inventory.FindIndex(equipment)))
            return false;
        wallet.TrySettle(current.RecoveryGold);
        State = CommissionState.Abandoned;
        return true;

    }
}
