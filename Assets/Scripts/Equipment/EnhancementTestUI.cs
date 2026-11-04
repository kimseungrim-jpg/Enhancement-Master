using TMPro;
using UnityEngine;

/// <summary>
/// 테스트 인벤토리와 지갑을 생성하고 장비 추가/제거/강화를 연결
/// 슬롯 버튼은 Inspector에서 연결하고 데이터가 바뀐 직후 화면을 갱신
/// </summary>
public sealed class EnhancementTestUI : MonoBehaviour
{
    private const int InventorySize = 5;

    [Header("테스트할 장비")]
    [SerializeField] private EquipmentData equipmentData;

    [Range(0, EquipmentInstance.MaxEchancementLevel)]
    [SerializeField] private int startingLevel;

    [Header("골드 테스트")]
    [Min(0)][SerializeField] private int startingGold = 1000;
    [Min(0)][SerializeField] private int goldToAdd = 500;
    [SerializeField] private TMP_Text goldText;

    [Header("인벤토리와 강화칸")]
    [SerializeField] private TMP_Text[] slotTexts = new TMP_Text[InventorySize];

    [SerializeField] private TMP_Text selectedEquipmentText;
    [SerializeField] private TMP_Text resultText;

    [Header("의뢰 테스트")]
    [SerializeField] private CommissionTestUI commissionUI;
    private bool CommissionBlocksActions => commissionUI != null && commissionUI.BlocksInventoryAction;

    private EquipmentInventory inventory;
    private EnhancementService enhancementService;
    private GoldWallet wallet;

    private int selectedSlotIndex = -1;

    private EquipmentInstance SelectedEquipment => selectedSlotIndex < 0 ? null : inventory.GetEquipment(selectedSlotIndex);

    private void Awake()
    {
        if (slotTexts == null || slotTexts.Length != InventorySize)
        {
            Debug.LogError("Slot Texts에 5개 슬롯의 텍스트를 지정하세요.", this);
            return;
        }

        for (int i = 0; i < slotTexts.Length; i++)
        {
            if (slotTexts[i] == null)
            {
                Debug.LogError($"Slot Texts의 {i}번 텍스트가 없습니다.", this);
                return;
            }
        }

        inventory = new EquipmentInventory(InventorySize);
        enhancementService = new EnhancementService();
        wallet = new GoldWallet(startingGold);

        RefreshUI();

        if (commissionUI != null)
            commissionUI.Initialize(this, inventory, wallet);
    }

    /// <summary>
    /// 테스트 장비 생성 버튼에서 호출
    /// 장비를 생성할 때마다 새 EquipmentInstance를 만들어 같은 종류의 장비라도 강화 단계와 파괴 여부를 따로 저장
    /// </summary>
    public void OnCreateEquipmentButtonClicked()
    {
        if (CommissionBlocksActions) return;

        if (inventory == null) return;

        if (equipmentData == null || equipmentData.EnhancementTable == null)
        {
            ShowMessage("테스트 장비 SO와 그 안의 강화표를 지정하세요");
            return;
        }

        EquipmentInstance newEquipment = new EquipmentInstance(equipmentData, startingLevel);

        bool added = inventory.TryAdd(newEquipment);

        ShowMessage(added ? "장비를 추가했습니다." : "장비 추가를 실패했습니다.");

        RefreshUI();
    }

    /// <summary>
    /// 각 슬롯 버튼에서 0~4 번호를 전달하여 호출
    /// 빈칸을 누르면 선택을 해제하고 장비가 있으면 그 칸을 선택
    /// </summary>
    public void OnSlotClicked(int slotIndex)
    {
        if (inventory == null) return;

        if (slotIndex < 0 || slotIndex >= inventory.Capacity)
        {
            ShowMessage("슬롯 버튼의 번호를 0~4로 지정하세요");
            return;
        }

        if (commissionUI != null && commissionUI.HandleSlotClick(inventory.GetEquipment(slotIndex))) return;

        selectedSlotIndex = inventory.GetEquipment(slotIndex) == null ? -1 : slotIndex;

        ShowMessage(selectedSlotIndex < 0 ? "선택을 해제했습니다." : "장비를 선택했습니다.");

        RefreshUI();
    }

    /// <summary>
    /// 강화 버튼에서 호출, 선택한 장비와 강화에 필요한 골드를 Service에 전달
    /// 결과 반영 후 슬롯과 강화칸, 잔액 표시를 갱신
    /// </summary>
    public void OnEnhanceButtonClicked()
    {
        if (CommissionBlocksActions) return;

        if (inventory == null) return;

        EquipmentInstance equipment = SelectedEquipment;

        if (equipment == null)
        {
            ShowMessage("강화할 장비를 선택하세요.");
            return;
        }

        EnhancementResult result = enhancementService.TryEnhance(equipment, wallet);

        switch (result)
        {
            case EnhancementResult.Success:
                ShowMessage("강화 성공");
                break;

            case EnhancementResult.Failure:
                ShowMessage("강화 실패 : 단계가 유지됩니다.");
                break;

            case EnhancementResult.Destroyed:
                ShowMessage("장비가 파괴되었습니다.");
                break;

            case EnhancementResult.InsufficientGold:
                ShowMessage("골드가 부족합니다.");
                break;

            case EnhancementResult.NotAttempted:
                ShowMessage(equipment.IsDestroyed ? "파괴된 장비입니다." : "최대 강화 단계입니다.");
                break;
        }

        RefreshUI();
    }

    /// <summary>
    /// 선택 장비 제거
    /// 제거한 장비를 계속 강화 대상으로 사용하지 않도록 선택도 해제
    /// </summary>
    public void OnRemoveSelectedButtonClicked()
    {
        if (CommissionBlocksActions) return;

        if (inventory == null) return;

        if (SelectedEquipment == null)
        {
            ShowMessage("제거할 장비를 선택하세요.");
            return;
        }

        if (!inventory.TryRemove(selectedSlotIndex))
        {
            ShowMessage("선택한 장비를 제거하지 못했습니다.");
        }

        selectedSlotIndex = -1;

        ShowMessage("선택한 장비를 제거했습니다.");

        RefreshUI();
    }

    /// <summary>
    /// 골드 추가 버튼에서 호출하여 같은 지갑에 테스트 금액을 지급
    /// </summary>
    public void OnAddGoldButtonClicked()
    {
        if (CommissionBlocksActions) return;

        if (wallet == null) return;

        wallet.AddGold(goldToAdd);

        ShowMessage($"{goldToAdd:N0} 골드를 추가했습니다.");
        RefreshUI();
    }

    /// <summary>
    /// 회수된 장비를 선택한 채로 남기지 않고 슬롯과 잔액을 갱신
    /// </summary>
    public void RefreshAfterCommissionChange()
    {
        selectedSlotIndex = -1;
        RefreshUI();
    }

    /// <summary>
    /// 초기화와 각 버튼 처리 후 호출
    /// 강화 후 변경된 장비 일므과 강화 단계를 인벤토리 슬롯과 강화칸에 다시 표시
    /// </summary>
    private void RefreshUI()
    {
        for (int i = 0; i < inventory.Capacity; i++)
        {
            EquipmentInstance item = inventory.GetEquipment(i);

            string marker = i == selectedSlotIndex ? "[선택]" : "";

            slotTexts[i].text = $"{marker}{i + 1}번 칸\n{GetEquipmentLabel(item)}";
        }

        if (selectedEquipmentText != null)
        {
            selectedEquipmentText.text = $"강화 대상: {GetEquipmentLabel(SelectedEquipment)}";
        }

        if (goldText != null)
            goldText.text = $"골드: {wallet.Gold:N0}";
    }

    /// <summary>
    /// RefreshUI에서 호출하여 장비 상태를 표시할 문자열을 생성
    /// </summary>
    private string GetEquipmentLabel(EquipmentInstance item)
    {
        if (item == null) return "비어 있음";

        string state = item.IsDestroyed ? "(파괴됨)" : "";

        return $"{item.Data.DisplayName} + {item.EnhacementLevel}{state}";
    }

    /// <summary>
    /// 각 버튼 처리 중 호출하여 안내와 결과를 Console 및 연결된 텍스트에 표시
    /// </summary>
    private void ShowMessage(string message)
    {
        Debug.Log(message, this);

        if(resultText != null)
            resultText.text = message;
    }
}
