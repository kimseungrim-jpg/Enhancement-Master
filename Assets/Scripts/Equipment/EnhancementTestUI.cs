using JetBrains.Annotations;
using TMPro;
using UnityEngine;

/// <summary>
/// 테스트 장비와 지갑을 생성해 보관하고 버튼 입력을 통해 강화를 진행
/// 강화 결과는 Console에, 보유 골드 현황은 연결된 text에 표시한다.
/// </summary>
public sealed class EnhancementTestUI : MonoBehaviour
{
    [Header("테스트할 장비와 강화표")]
    [SerializeField] private EquipmentData equipmentData;
    [SerializeField] private EnhancementTable enhancementTable;

    [Range(0, EquipmentInstance.MaxEchancementLevel)]
    [SerializeField] private int startingLevel;

    [Header("골드 테스트")]
    [Min(0)][SerializeField] private int startingGold = 1000;
    [Min(0)][SerializeField] private int goldToAdd = 500;
    [SerializeField] private TMP_Text goldText;

    private EquipmentInstance equipment;
    private EnhancementService enhancementService;
    private GoldWallet wallet;

    private void Awake()
    {
        if (equipmentData == null || enhancementTable == null)
        {
            Debug.LogError("테스트할 장비 데이터와 강화표를 모두 지정하세요");
            return;
        }

        // 버튼을 누를 때마다 새 장비를 만들면 강화 단계가 초기화됨
        // 처음 만든 장비를 보관하고 계속 같은 장비를 강화
        equipment = new EquipmentInstance(equipmentData, startingLevel);
        enhancementService = new EnhancementService();

        wallet = new GoldWallet(startingGold);
        RefreshGoldUI();
    }

    /// <summary>
    /// Inspector에서 Button.onClick에 연결하여 클릭할 때 호출
    /// 보관 중인 장비의 강화를 요청하고 전후 단계와 결과를 출력
    /// </summary>
    public void OnEnhanceButtonClikced()
    {
        if (equipment == null || enhancementService == null || enhancementTable == null)
        {
            Debug.LogWarning("강화 테스트가 준비되지 않았습니다. SO 설정을 확인하세요.",this);
            return;
        }

        int previousLevle = equipment.EnhacementLevel;

        EnhancementResult result = enhancementService.TryEnhance(equipment, enhancementTable, wallet);

        RefreshGoldUI();

        if (result == EnhancementResult.NotAttempted)
        {
            string reason = equipment.IsDestroyed ? "Destroyed Equipment" : "MaxLevel Equipment";

            Debug.Log($"강화 불가: {reason}", this);
            return;
        }

        if (result == EnhancementResult.InsufficientGold)
        {
            Debug.Log($"골드 부족: 현재 {wallet.Gold:N0} 골드", this);
            return;
        }

        Debug.Log($"{equipment.Data.DisplayName}: {result} | " +
            $"+{previousLevle} -> +{equipment.EnhacementLevel} | " +
            $"파괴 여부: {equipment.IsDestroyed} | " + 
            $"잔액: {wallet.Gold:N0}",
            this);
    }

    /// <summary>
    /// Inspector에서 골드 추가 버튼의 On Click에 연결하여 호출
    /// 테스트 금액을 지급한 뒤 표시를 갱신
    /// </summary>
    public void OnAddGoldButtonClicked()
    {
        if (wallet == null)
        {
            Debug.Log("테스트 지갑이 생성되지 않았습니다.", this);
            return;
        }

        wallet.AddGold(goldToAdd);
        RefreshGoldUI();

        Debug.Log($"골드 추가 후 잔액: {wallet.Gold:N0}", this);
    }

    /// <summary>
    /// 초기화 및 각 버튼 처리 후 호출하여 현재 잔액을 표시
    /// 지금은 이 UI에서만 골드가 바뀌므로 해당 시점에서 직접 갱신
    /// </summary>
    private void RefreshGoldUI()
    {
        if (goldText != null && wallet != null)
            goldText.text = $"Gold: {wallet.Gold:N0}";
    }
}
