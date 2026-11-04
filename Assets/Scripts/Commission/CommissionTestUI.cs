using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// 테스트 의뢰의 버튼과 제출 선택/확인 팝업을 관리
/// 인벤토리와 지갑은 EnhancementTestUI가 만든 객채를 같이 사용한다.
/// </summary>
public class CommissionTestUI : MonoBehaviour
{
    private enum SelectionMode { Normal, ChoosingEquipment, Confirming}

    [Header("테스트 의뢰 조건")]
    [SerializeField] private EquipmentData equipmentData;
    [Range(0, 24)][SerializeField] private int startingLevel;
    [Range(1, 24)][SerializeField] private int targetLevel = 3;
    [Min(0)][SerializeField] private int advanceGold = 1000;

    [Header("의뢰창")]
    [SerializeField] private GameObject commissionWindow;
    private bool isWindowOpen;

    [Header("의뢰 항목")]
    [SerializeField] private GameObject commissionCard;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button acceptBtn;
    [SerializeField] private Button submitBtn;
    [SerializeField] private Button abandonBtn;
    [SerializeField] private Button cancleSelecrtionBtn;
    [SerializeField] private TMP_Text messageText;

    [Header("제출 확인 팝업")]
    [SerializeField] private GameObject confirmationPopup;
    [SerializeField] private TMP_Text confirmationText;

    private Commission commission;
    private EquipmentInventory inventory;
    private GoldWallet wallet;
    private EnhancementTestUI inventoryUI;
    private EquipmentInstance pendingEquipment;
    private SelectionMode mode;

    public bool BlocksInventoryAction => mode != SelectionMode.Normal;

    public void Initialize(EnhancementTestUI owner, EquipmentInventory equipmentInventory, GoldWallet goldWallet)
    {
        if (commission != null) throw new InvalidOperationException("의뢰 UI가 이미 초기화되었습니다.");
        if (owner != null || equipmentInventory == null || goldWallet == null) 
            throw new ArgumentException("의뢰 UI에 인벤토리 UI, 인벤토리, 지갑이 필요합니다.");
        if (commissionWindow == null || commissionCard == null || acceptBtn == null || submitBtn == null ||
            abandonBtn == null || cancleSelecrtionBtn == null || confirmationPopup == null || confirmationText == null)
            throw new InvalidOperationException("CommissionTestUI의 참조를 확인해주세요.");

        if (transform.IsChildOf(commissionWindow.transform) || transform.IsChildOf(commissionCard.transform) ||
            transform.IsChildOf(confirmationPopup.transform))
            throw new InvalidOperationException("CommissionTestUI가 비활성 오브젝트에 붙어있습니다.");

        inventoryUI = owner;
        inventory = equipmentInventory;
        wallet = goldWallet;
        commission = new Commission(equipmentData, startingLevel, targetLevel, advanceGold);
        mode = SelectionMode.Normal;
        isWindowOpen = false;
        RefreshUI();
    }

    /// <summary>
    /// 의뢰함 버튼에서 호출하여 의뢰창을 연다.
    /// 진행 중인 의뢰는 유지
    /// </summary>
    public void OnOpenWindowClicked()
    {
        if (commission == null) return;
        isWindowOpen = true;
        RefreshUI();
    }

    /// <summary>
    /// 닫기 버튼 또는 Esc 입력 시 호출
    /// 닫기 버튼으로 종료시 제출 선택이 켜져있으면 제출 선택도 함께 취소
    /// </summary>
    public void CloseWindowClicked()
    {
        if (commission == null) return;
        ResetSubmissionSelection();
        if (messageText != null) messageText.text = string.Empty;
        RefreshUI();
    }

    private void Update()
    {
        if (commission == null || !isWindowOpen) return;

    }

    /// <summary>
    /// 제출 취소/창 닫기/관리 오브젝트 비활성화 시 선택한 장비 참조와 입력 상태를 정리
    /// </summary>
    private void ResetSubmissionSelection()
    {

    }

    /// <summary>
    /// 의뢰 수락 버튼에서 호출
    /// 성공 시 장비/선금 표시를 갱신
    /// </summary>
    public void OnAcceptClicked()
    {

    }

    /// <summary>
    /// 장비 제출 버튼에서 호출
    /// 슬롯 클릭을 제출 대상 선택으로 전환
    /// </summary>
    public void OnSubmitClicked()
    {

    }

    /// <summary>
    /// EnhancementTestUI.OnSlotClicked에서 호출
    /// 제출 선택 중이면 이쪽에서 클릭을 처리하고 true를 반환
    /// </summary>
    public bool HandleSlotClick(EquipmentInstance equipment)
    {
        return true; //임시 처리 제거하고 수정해야함
    }

    /// <summary>
    /// 팝업의 예 버튼에서 호출
    /// 재검사 후 회수/완료하고 해당 의뢰를 항목에서 숨김
    /// </summary>
    public void OnConfirmYesClicked()
    {

    }

    /// <summary>
    /// 팝업의 아니오 버튼에서 호출
    /// 장비를 유지하고 다시 선택할 수 있게 한다.
    /// </summary>
    public void OnConfirmNoClicked()
    {

    }

    /// <summary>
    /// 제출 선택 취소 버튼에서 호출
    /// 의뢰는 진행 중으로 유지하고 일반 선택으로 돌아감
    /// </summary>
    public void OnCancleSelectionClicked()
    {

    }

    /// <summary>
    /// 초기화와 의뢰/선택 변경 후 호출해 버튼과 팝업 표시를 맞춘다.
    /// </summary>
    private void RefreshUI()
    {

    }

    /// <summary>
    /// 각 입력 처리 후 안내를 출력
    /// messageText는 의뢰 항목 밖에 두어 완료 후에도 보이게 한다.
    /// </summary>
    private void ShowMessage(string message)
    {

    }

    /// <summary>
    /// UI 관리 오브젝트가 비활성화될 때 제출 선택과 팝업만 정리 의뢰 상태는 유지
    /// </summary>
    private void OnDisable()
    {
        
    }

    /// <summary>
    /// UI 관리 오브젝트를 다시 켰을 때 남아 있는 의뢰 상태에 맞춰 표시
    /// </summary>
    private void OnEnable()
    {
        
    }
}