using System;
using UnityEngine;

/// <summary>
/// 강화 한 단계에 필요한 골드와 성공/파괴 확률을 저장
/// 일반 실패 확률은 전체 100%에서 성공/파괴 확률을 뺀 값
/// </summary>
[Serializable]
public class EnhancemenetStepData
{
    [Min(0)]
    [SerializeField] private int goldCost = 100;

    [Range(0f, 100f)]
    [SerializeField] private float successChance = 100f;

    [Range(0f, 100f)]
    [SerializeField] private float destructionChance = 100f;

    public int GoldCost => goldCost;
    public float SuccessChance => successChance;
    public float DestructionChance => destructionChance;
    public float FailureChance => 100f - successChance - destructionChance;

    public bool IsValid => goldCost == 0
        && successChance >= 0f && successChance <= 100f
        && destructionChance >= 0f && destructionChance <= 100f
        && successChance + destructionChance <= 100f;
}

/// <summary>
/// 강화 단계별 비용과 확률을 저장하는 공통 설정 에셋
/// 배열의 0번은 +0에서 +1, 24번은 +24에서 +25로 강화할 때 사용
/// </summary>
[CreateAssetMenu(fileName = "EnhancementTable_New",
    menuName = "Enhancement Master/Enhancement Table")]
public class EnhancementTable : ScriptableObject
{
    [Tooltip("25개 항목을 유지한다. Element 번호는 강화 시도 ")]
    [SerializeField] private EnhancemenetStepData[] steps = CreateDefaultSteps();

    /// <summary>
    /// 현재 단계에서 다음 단계로 강화할 때 사용할 설정을 반환
    /// 현재 연결된 호출처는 없으며, 잘못된 설정을 읽으면 예외를 발생시킴
    /// </summary>
    public EnhancemenetStepData GetStep(int currentLevel)
    {
        if (currentLevel < 0 || currentLevel >= EquipmentInstance.MaxEchancementLevel)
        {
            throw new ArgumentOutOfRangeException(nameof(currentLevel), currentLevel,
                "강화 설정은 현재 단계 0~24에서 조회할 수 있습니다.");
        }

        if (steps == null || steps.Length != EquipmentInstance.MaxEchancementLevel)
        {
            throw new InvalidOperationException(
                $"{name}: 강화 설정은 25개가 필요합니다.");
        }

        EnhancemenetStepData step = steps[currentLevel];

        if (step == null || !step.IsValid)
        {
            throw new InvalidOperationException(
                $"{name}: + {currentLevel} 강화 설정의 비용과 확률을 확인하세요. " +
                "비용은 0 이상, 각 확률은 0~100, " +
                "성공/파괴 확률 합계는 100 이하여야 합니다.");
        }

        return step;
    }

    /// <summary>
    /// 강화표 객체의 필드 초기화 시 사용할 25개 설정을 생성
    /// 모든 단계의 초기 수치는 테스트용이며 Inspector에서 조정
    /// </summary>
    private static EnhancemenetStepData[] CreateDefaultSteps()
    {
        var defaultSteps = new EnhancemenetStepData[EquipmentInstance.MaxEchancementLevel];

        for (int i = 0; i < defaultSteps.Length; i++)
        {
            defaultSteps[i] = new EnhancemenetStepData();
        }

        return defaultSteps;
    }
}
