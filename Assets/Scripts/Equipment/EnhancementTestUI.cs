using JetBrains.Annotations;
using UnityEngine;

/// <summary>
/// Inspector에서 지정한 프리셋으로 테스트 장비 한 개를 만들고
/// 버튼으로 강화 요청을 보내 결과를 Console에 표시한다.
/// </summary>
public sealed class EnhancementTestUI : MonoBehaviour
{
    [Header("테스트할 장비와 강화표")]
    [SerializeField] private EquipmentData equipmentData;
    [SerializeField] private EnhancementTable enhancementTable;

    [Range(0, EquipmentInstance.MaxEchancementLevel)]
    [SerializeField] private int startingLevel;

    private EquipmentInstance equipment;
    private EnhancementService enhancementService;

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

        EnhancementResult result = enhancementService.TryEnhance(equipment, enhancementTable);

        if (result == EnhancementResult.NotAttempted)
        {
            string reason = equipment.IsDestroyed ? "Destroyed Equipment" : "MaxLevel Equipment";

            Debug.Log($"강화 불가: {reason}", this);
            return;
        }

        Debug.Log($"{equipment.Data.DisplayName}: {result} | " +
            $"+{previousLevle} -> +{equipment.EnhacementLevel} | " +
            $"파괴 여부: {equipment.IsDestroyed}", this);
    }
}
