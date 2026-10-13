using UnityEngine;

//장비 종류는 기존 값이 바뀌지 않도록 번호를 유지하며 추가
public enum EquipmentType
{
    Sword = 0,
    Bow = 1,
    Staff = 2
}

/// <summary>
/// 같은 종류의 장비가 공유하는 이름, 아이콘, 기본 능력치를 저장하는 프리셋
/// 현재 강화 단계와 파괴 여부는 개별 장비마다 달라지므로 여기에는 저장하지 않음
/// </summary>
[CreateAssetMenu(fileName = "Equipment_New", menuName = "Enhancement Master/Equipment Data")]
public class EquipmentData : ScriptableObject
{
    [Header("장비 식별 정보")]
    [Tooltip("장비 종류별 고유 ID. 예: weapon_iron_sword. 다른 프리셋과 중복되지 않게 입력")]
    [SerializeField] private string equipmentId;

    [SerializeField] private string displayName;
    [SerializeField] private EquipmentType equipmentType;
    [SerializeField] private Sprite icon;

    [Header("강화 전 기본 능력치")]
    [Min(0)]
    [SerializeField] private int baseAttack = 10;

    //다른 스크립트는 값을 읽기만 하고, 공통 설정은 프리셋의 Inspector에서 관리
    public string EquipmentId => equipmentId;
    public string DisplayName => displayName;
    public EquipmentType Type => equipmentType;
    public Sprite Icon => icon;
    public int BaseAttack => baseAttack;
}
