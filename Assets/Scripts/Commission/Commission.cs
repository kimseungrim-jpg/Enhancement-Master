using NUnit.Framework.Constraints;
using System;

public enum CommissionState { Available, InProgress, Completed }
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

        EquipmentInstance equipment = new EquipmentInstance(EquipmentData, StartingLevel);
        if (!inventory.TryAdd(equipment)) return CommissionAcceptResult.InventoryFull;

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
            && equipment.EnhacementLevel >= TargetLevel;
            //&& inventory.FindIndex(equipment) >= 0; //아직 인벤토리 수정을 안해서 추가후 주석해제
    }

    /// <summary>
    /// 확인 팝업에서 예를 눌러 호출
    /// 조건을 다시 검색하고 선택한 장비 하나를 회수한 뒤 완료 상태로 변경
    /// </summary>
    public bool TryComplete(EquipmentInventory inventory, EquipmentInstance equipment)
    {
        if(!CanSubmit(inventory, equipment)) return false;

        //int slotIndex = inventory.FIndIndex(equipment);
        //if(!inventory.TryRemove(slotIndex)) return false; //인벤토리 수정 후 주석 해제

        State = CommissionState.Completed;
        return true;
    }
}
