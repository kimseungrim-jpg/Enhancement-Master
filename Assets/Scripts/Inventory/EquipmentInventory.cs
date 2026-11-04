using UnityEngine;
using System;

/// <summary>
/// 슬롯별로 개별 장비의 참조를 보관
/// 빈칸은 Null, 장비를 제거해도 다른 장비의 슬롯 번호는 바뀌지 않는다.
/// </summary>
public class EquipmentInventory
{
    public readonly EquipmentInstance[] slots;

    public int Capacity => slots.Length;

    
    public EquipmentInventory(int capcity = 5)
    {
        if (capcity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capcity));

        slots = new EquipmentInstance[capcity];
    }

    /// <summary>
    /// 테스트 장비 생성 버튼에서 호출하여 첫 빈칸에 장비를 생성 
    /// 같은 객체가 이미 들어 있거나 빈칸이 없으면 false를 반환
    /// </summary>
    public bool TryAdd(EquipmentInstance equipment)
    {
        if (equipment == null)
            throw new ArgumentNullException(nameof(equipment));

        int emptyIndex = -1;

        for (int i = 0; i < slots.Length; i++)
        {
            if (ReferenceEquals(slots[i], equipment))
                return false;

            if (slots[i] == null && emptyIndex == -1)
                emptyIndex = i;
        }

        if (emptyIndex == -1)
        {
            return false;
        }

        slots[emptyIndex] = equipment;
        return true;
    }

    /// <summary>
    /// TestUI가 슬롯을 선택하거나 화면을 갱신할 때 호출
    /// 빈칸이면 null을 반환
    /// </summary>
    public EquipmentInstance GetEquipment(int slotIndex)
    {
        ValidateIndex(slotIndex);
        return slots[slotIndex];
    }

    /// <summary>
    /// TestUI의 선택 장비 제거 버튼에서 호출하여 해당 칸을 비운다.
    /// 이미 빈칸이면 false를 반환
    /// </summary>
    public bool TryRemove(int slotIndex)
    {
        ValidateIndex(slotIndex);

        if (slots[slotIndex] == null)
            return false;

        slots[slotIndex] = null;
        return true;
    }


    public int FindIndex(EquipmentInstance equipment)
    {
        if (equipment == null) return -1;
        for (int i = 0; i < slots.Length; i++)
        {
            if (ReferenceEquals(slots[i], equipment)) return i;
        }

        return -1;
    }

    /// <summary>
    /// 조회/제거 전에 호출하여 잘못 전달된 슬롯 번호를 확인한다.
    /// </summary>
    private void ValidateIndex(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length)
            throw new ArgumentOutOfRangeException(nameof(slotIndex));
    }
}
