using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 테스트 의뢰의 버튼과 제출 선택/확인 팝업을 관리
/// 인벤토리와 지갑은 EnhancementTestUI가 만든 객채를 같이 사용한다.
/// </summary>
public class CommissionTestUI : MonoBehaviour
{
    private enum SelectionMode { Normal, ChoosingEquipment, Confirming }

    [Header("테스트 의뢰 조건")]
    [SerializeField] private EquipmentData equipmentData;
    [Range(0, 24)][SerializeField] private int startingLevel;
    [Range(1, 24)][SerializeField] private int targetLevel = 3;
    [Min(0)][SerializeField] private int advanceGold = 1000;

    /// <summary>
    /// 추가 의뢰를 Inspector에서 설정한다. 각 항목은 별도의 진행 상태를 갖는다.
    /// </summary>
    [Serializable]
    private class ExtraOrder
    {
        public EquipmentData equipment;
        [Range(0, 24)] public int start;
        [Range(1, 25)] public int target = 3;
        [Min(0)] public int advance = 1000;
    }

    //목록이 비어 있으면 지금처럼 한 건만 사용
    [SerializeField] private ExtraOrder[] extraOrders;
    private readonly List<Commission> orders = new List<Commission>();

    [Header("의뢰창")]
    [SerializeField] private GameObject commissionWindow;
    private bool isWindowOpen;

    [Header("의뢰 항목")]
    private const int MaxOrders = 5;
    [SerializeField] private ScrollRect orderScroll;
    [SerializeField] private CommissionCardUI cardPrefab;
    private readonly Dictionary<Commission, CommissionCardUI> cards = new Dictionary<Commission, CommissionCardUI>();
    [SerializeField] private Button cancleSelectionBtn;
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
    private bool isAbandoning;
    private AbandonmentQuset pendingQuote;

    [Header("포기 항목")]
    [SerializeField] private Button abandonWithoutEquipmentButton;

    // 만료 팝업에서도 강화와 장비 제거 입력을 막는다.
    public bool BlocksInventoryAction => mode != SelectionMode.Normal || DeadlineActive;

    public void Initialize(EnhancementTestUI owner, EquipmentInventory equipmentInventory, GoldWallet goldWallet)
    {
        if (commission != null) throw new InvalidOperationException("의뢰 UI가 이미 초기화되었습니다.");
        if (owner == null || equipmentInventory == null || goldWallet == null)
            throw new ArgumentException("의뢰 UI에 인벤토리 UI, 인벤토리, 지갑이 필요합니다.");
        if (commissionWindow == null || cardPrefab == null || orderScroll == null || orderScroll.content == null ||
            cancleSelectionBtn == null || confirmationPopup == null || confirmationText == null
            || abandonWithoutEquipmentButton == null)
            throw new InvalidOperationException("CommissionTestUI의 참조를 확인해주세요.");

        if (transform.IsChildOf(commissionWindow.transform) ||
            transform.IsChildOf(confirmationPopup.transform))
            throw new InvalidOperationException("CommissionTestUI가 비활성 오브젝트에 붙어있습니다.");

        ValidateDeadlineUI();
        inventoryUI = owner;
        inventory = equipmentInventory;
        wallet = goldWallet;
        commission = new Commission(equipmentData, startingLevel, targetLevel, advanceGold);
        orders.Add(commission);
        if (extraOrders != null)
            foreach (var settings in extraOrders)
            {
                if (orders.Count >= MaxOrders) break;
                if (settings == null || settings.equipment == null)
                    throw new InvalidOperationException("Extra Orders의 각 항목에 장비를 지정하세요.");
                orders.Add(new Commission(settings.equipment, settings.start, settings.target, settings.advance));
            }

        for (int i = 0; i < orders.Count; i++)
        {
            var card = Instantiate(cardPrefab, orderScroll.content);
            card.Bind(this, orders[i], i + 1);
            cards.Add(orders[i], card);
        }
        mode = SelectionMode.Normal;
        if (isActiveAndEnabled) ConnectCalendar();
        SetDeadlineView(DeadlineView.None);
        isWindowOpen = false;
        RefreshUI();
    }

    /// <summary>
    /// 카드 안의 수락·제출·포기 버튼을 누르면, 그 카드의 의뢰를 처리 대상으로 저장한다.
    /// 지금 버튼 입력을 받을 수 없거나 목록에 없는 의뢰라면 대상을 바꾸지 않고 false를 반환한다
    /// </summary>
    private bool SelectCard(Commission item)
    {
        if (!isWindowOpen || DeadlineActive || mode != SelectionMode.Normal || !cards.ContainsKey(item)) return false;
        commission = item;
        return true;
    }

    /// <summary>
    /// 의뢰함 버튼에서 호출하여 의뢰창을 연다.
    /// 진행 중인 의뢰는 유지
    /// </summary>
    public void OnOpenWindowClicked()
    {
        if (DeadlineActive) return; 
        if (commission == null) return;
        isWindowOpen = true;
        RefreshUI();
    }

    /// <summary>
    /// 닫기 버튼 또는 Esc 입력 시 호출
    /// 닫기 버튼으로 종료시 제출 선택이 켜져있으면 제출 선택도 함께 취소
    /// </summary>
    public void OnCloseWindowClicked()
    {
        if (DeadlineActive) return;
        if (commission == null) return;
        ResetSubmissionSelection();
        isWindowOpen = false;
        if (messageText != null) messageText.text = string.Empty;
        RefreshUI();
    }

    private void Update()
    {
        if (DeadlineActive) return;
        if (commission == null || !isWindowOpen) return;

        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (mode != SelectionMode.Normal)
        {
            OnCancleSelectionClicked();
        }
        else
        {
            OnCloseWindowClicked();
        }

    }

    /// <summary>
    /// 제출 취소/창 닫기/관리 오브젝트 비활성화 시 선택한 장비 참조와 입력 상태를 정리
    /// </summary>
    private void ResetSubmissionSelection()
    {
        pendingEquipment = null;
        pendingQuote = null;
        isAbandoning = false;
        mode = SelectionMode.Normal;
    }

    /// <summary>
    /// 의뢰 수락 버튼에서 호출
    /// 성공 시 장비/선금 표시를 갱신
    /// </summary>
    public void OnAcceptClicked(Commission item)
    {
        if (!SelectCard(item)) return;
        if (DeadlineActive) return;
        if (commission == null || !isWindowOpen || mode != SelectionMode.Normal) return;
        CommissionAcceptResult result = commission.TryAccept(inventory, wallet, calendarUI.Calendar.CurrentDay);
        switch (result)
        {
            case CommissionAcceptResult.Accepted:
                ShowMessage("의뢰를 수락했습니다. 장비와 선금을 받았습니다.");
                inventoryUI.RefreshAfterCommissionChange();
                break;
            case CommissionAcceptResult.InventoryFull:
                ShowMessage("인벤토리가 가득 차서 수락할 수 없습니다.");
                break;
            case CommissionAcceptResult.GoldLimitReached:
                ShowMessage("보유 골드 한도를 초과하여 선금을 받을 수 없습니다.");
                break;
            default:
                ShowMessage("이미 수락했거나 완료한 의뢰입니다.");
                break;
        }

        RefreshUI();
    }

    /// <summary>
    /// 장비 제출 버튼에서 호출
    /// 슬롯 클릭을 제출 대상 선택으로 전환
    /// </summary>
    public void OnSubmitClicked(Commission item)
    {
        if (!SelectCard(item)) return;
        if (DeadlineActive) return;
        if (commission == null || !isWindowOpen || commission.State != CommissionState.InProgress || mode != SelectionMode.Normal)
            return;
        isAbandoning = false;
        mode = SelectionMode.ChoosingEquipment;
        ShowMessage("제출할 장비를 인벤토리에서 선택하세요.");
        RefreshUI();
    }

    /// <summary>
    /// 포기 버튼에서 호출하여 반환할 정상 장비 또는 흔적을 선택하게 한다.
    /// </summary>
    public void OnAbandonClicked(Commission item)
    {
        if (!SelectCard(item)) return;
        if (DeadlineActive) return;
        if (commission == null || !isWindowOpen || mode != SelectionMode.Normal || commission.State != CommissionState.InProgress)
            return;
        isAbandoning = true;
        mode = SelectionMode.ChoosingEquipment;
        ShowMessage("반환할 장비/흔적을 선택하거나 '제출하지 않는다.'를 누르세요");
        RefreshUI();
    }

    /// <summary>
    /// 장비 없이 포기 버튼에서 호출하여 미반환 회수금을 미리 보여준다.
    /// </summary>
    public void OnAbandonWithoutEquipmentClicked()
    {
        if (DeadlineActive) return;
        if (!isAbandoning || mode != SelectionMode.ChoosingEquipment) return;
        OpenAbandonmentConfirmation(null);
    }

    /// <summary>
    /// 포기 대상 선택 후 호출
    /// 확인 전에는 장비나 지갑을 변경하지 않는다.
    /// </summary>
    public void OpenAbandonmentConfirmation(EquipmentInstance equipment)
    {
        if (DeadlineActive) return;
        if (!commission.TryGetAbandonmentQuoto(inventory, wallet, equipment, out var quote))
        {
            ShowMessage("같은 종류의 장비 또는 흔적을 선택하세요. 완료 가능한 장비를 제출해 주세요.");
            return;
        }
        pendingEquipment = equipment;
        pendingQuote = quote;
        mode = SelectionMode.Confirming;
        string item = equipment == null ? "반환 없음" : equipment.IsDestroyed ? $"{equipment.Data.DisplayName} 흔적" :
            $"{equipment.Data.DisplayName} +{equipment.EnhacementLevel}";
        confirmationText.text = $"의뢰를 포기하시겠습니까? \n회수: {item}\n" +
            FormatQuote(quote);

        RefreshUI();
    }

    /// <summary>
    /// 처음 받은 장비인지와 관계없이 현재 제출할 수 있는 장비인지 검사
    /// EnhancementTestUI가 장비 선택/강화 후 확인할 때 호출
    /// </summary>
    public bool IsGoldEquipment(EquipmentInstance equipment)
    {
        foreach (var order in orders)
            if (order.CanSubmit(inventory, equipment)) return true;
        return false;
    }

    /// <summary>
    /// EnhancementTestUI.OnSlotClicked에서 호출
    /// 제출 선택 중이면 이쪽에서 클릭을 처리하고 true를 반환
    /// </summary>
    public bool HandleSlotClick(EquipmentInstance equipment)
    {
        if (DeadlineActive) return true;
        if (mode == SelectionMode.Normal) return false;
        if (mode == SelectionMode.Confirming) return true;

        if (isAbandoning)
        {
            if (equipment == null) ShowMessage("빈 슬롯입니다. 제출 하지 않음 버튼을 이용하세요.");
            else if (commission.CanSubmit(inventory, equipment)) ShowMessage("의뢰목표를 달성한 장비입니다. 제출해주세요!");
            else OpenAbandonmentConfirmation(equipment);
            return true;
        }

        if (!commission.CanSubmit(inventory, equipment))
        {
            ShowMessage("해당 장비는 의뢰를 충족하지 않습니다.");
            return true;
        }

        pendingEquipment = equipment;
        mode = SelectionMode.Confirming;
        confirmationText.text = $"{equipment.Data.DisplayName} + {equipment.EnhacementLevel}\n제출하시겠습니까?";
        RefreshUI();
        return true;
    }

    /// <summary>
    /// 팝업의 예 버튼에서 호출
    /// 재검사 후 회수/완료하고 해당 의뢰를 항목에서 숨김
    /// </summary>
    public void OnConfirmYesClicked()
    {
        if (DeadlineActive) return;
        if (commission == null || mode != SelectionMode.Confirming) return;

        if (isAbandoning)
        {
            bool abandoned = commission.TryAbandon(inventory, wallet, pendingEquipment, pendingQuote);
            pendingEquipment = null;
            pendingQuote = null;
            if (abandoned)
                ResetSubmissionSelection();
            else
                mode = SelectionMode.ChoosingEquipment;
            ShowMessage(abandoned ? "의뢰를 포기하고 정산했습니다." : "인벤토리, 지갑 변경 또는 정산 한도를 초과했습니다. 다시 선택하세요");
            inventoryUI.RefreshAfterCommissionChange();
            RefreshUI();
            return;
        }

        bool completed = commission.TryComplete(inventory, pendingEquipment);
        pendingEquipment = null;
        mode = completed ? SelectionMode.Normal : SelectionMode.ChoosingEquipment;
        ShowMessage(completed ? "장비를 제출하여 의뢰를 완료했습니다." : "조건에 맞는 장비가 아닙니다. 다시 선택하세요");
        inventoryUI.RefreshAfterCommissionChange();
        RefreshUI();
    }

    /// <summary>
    /// 팝업의 아니오 버튼에서 호출
    /// 장비를 유지하고 다시 선택할 수 있게 한다.
    /// </summary>
    public void OnConfirmNoClicked()
    {
        if (DeadlineActive) return;
        if (mode != SelectionMode.Confirming) return;
        pendingEquipment = null;
        mode = SelectionMode.ChoosingEquipment;
        ShowMessage("다른 장비를 선택하거나 제출 선택을 취소하세요.");
        RefreshUI();
    }

    /// <summary>
    /// 제출 선택 취소 버튼에서 호출
    /// 의뢰는 진행 중으로 유지하고 일반 선택으로 돌아감
    /// </summary>
    public void OnCancleSelectionClicked()
    {
        if (DeadlineActive) return;
        if (commission == null || mode == SelectionMode.Normal) return;
        ResetSubmissionSelection();
        ShowMessage("제출 선택을 취소했습니다.");
        RefreshUI();
    }

    /// <summary>
    /// 초기화와 의뢰/선택 변경 후 호출해 버튼과 팝업 표시를 맞춘다.
    /// </summary>
    private void RefreshUI()
    {
        commissionWindow.SetActive(isWindowOpen);
        bool active = commission.State == CommissionState.InProgress;
        for (int i = orders.Count - 1; i >= 0; i--)
        {
            Commission item = orders[i];
            var card = cards[item];
            if (item.State == CommissionState.Available || item.State == CommissionState.InProgress)
                card.Render(mode == SelectionMode.Normal && !DeadlineActive);
            else
            {
                orders.RemoveAt(i);
                cards.Remove(item);
                card.gameObject.SetActive(false);
                Destroy(card.gameObject);
            }
        }
        abandonWithoutEquipmentButton.gameObject.SetActive(active && isAbandoning && mode == SelectionMode.ChoosingEquipment);
        cancleSelectionBtn.gameObject.SetActive(mode == SelectionMode.ChoosingEquipment);
        confirmationPopup.SetActive(isWindowOpen && mode == SelectionMode.Confirming);
        if (mode == SelectionMode.Confirming)
            confirmationPopup.transform.SetAsLastSibling();

    }

    /// <summary>
    /// 각 입력 처리 후 안내를 출력
    /// messageText는 의뢰 항목 밖에 두어 완료 후에도 보이게 한다.
    /// </summary>
    private void ShowMessage(string message)
    {
        Debug.Log(message, this);
        if (messageText != null) messageText.text = message;
    }

    /// <summary>
    /// UI 관리 오브젝트가 비활성화될 때 제출 선택과 팝업만 정리 의뢰 상태는 유지
    /// </summary>
    private void OnDisable()
    {
        if (calendarUI != null) calendarUI.Calendar.DayChanged -= OnCalendarDayChanged;
        if (deadlinePopup != null) deadlinePopup.SetActive(false);
        ResetSubmissionSelection();
        isWindowOpen = false;
        if (commissionWindow != null) commissionWindow.SetActive(false);
        if (confirmationPopup != null) confirmationPopup.SetActive(false);
    }

    /// <summary>
    /// UI 관리 오브젝트를 다시 켰을 때 남아 있는 의뢰 상태에 맞춰 표시
    /// </summary>
    private void OnEnable()
    {
        if (commission == null) return;
        ConnectCalendar();
        SetDeadlineView(deadlineView);
        OnCalendarDayChanged(calendarUI.Calendar.CurrentDay);
        RefreshUI();
    }

    private enum DeadlineView { None, Warning, Result }
    [Header("마감 알림과 정산 (메인 Canvas)")]
    [SerializeField] private CanlendarUI calendarUI;
    [SerializeField] private GameObject deadlinePopup;
    [SerializeField] private TMP_Text deadlineText;
    [SerializeField] private TMP_Text warningText;
    [SerializeField] private Button noticeClose;
    [SerializeField] private ScrollRect noticeScroll;
    private DeadlineView deadlineView;
    private bool DeadlineActive => deadlineView != DeadlineView.None;

    /// <summary>
    /// 초기화할 때 날짜와 팝업 참조를 검사
    /// </summary>
    private void ValidateDeadlineUI()
    {
        if (!warningText || warningText == deadlineText ||
            (noticeScroll && warningText.transform.IsChildOf(noticeScroll.transform)))
            throw new InvalidOperationException("Warning Text에는 정산 스크롤 밖의 별도 TMP 텍스트를 연결하세요.");
        if (!calendarUI || !deadlinePopup || !deadlineText || !noticeClose || !noticeScroll ||
            !noticeScroll.content || !noticeScroll.verticalScrollbar)
            throw new InvalidOperationException("달력, 팝업 본문, 닫기 버튼, ScrollRect의 Content와 세로 Scrollbar를 연결하세요.");
        if (deadlinePopup.transform.IsChildOf(commissionWindow.transform) ||
            transform.IsChildOf(deadlinePopup.transform))
            throw new InvalidOperationException("마감 팝업과 의뢰 관리 객체를 메인 Canvas에서 독립적으로 배치하세요.");
    }

    /// <summary>
    /// 초기화되거나 다시 켜지면 날짜를 구독한다. 
    /// OnDisable에서 해제해 중복 호출을 막는다.
    /// </summary>
    private void ConnectCalendar()
    {
        calendarUI.Calendar.DayChanged -= OnCalendarDayChanged;
        calendarUI.Calendar.DayChanged += OnCalendarDayChanged;
    }

    /// <summary>
    /// 날짜 변경 직후, 또는 다시 활성화됐을 때 경고일과 만료일을 확인한다.
    /// </summary>
    private void OnCalendarDayChanged(int day)
    {
        const string warning = "내일 만료인 의뢰가 있습니다.";
        var rows = new StringBuilder();
        long total = 0;
        int count = 0;
        bool warn = false;
        for (int i = 0; i < orders.Count; i++)
        {
            Commission order = orders[i];
            if (order.TryExpire(day, wallet, out long debt))
            {
                count++;
                total = checked(total + debt);
                if (count > 1) rows.AppendLine();
                rows.AppendLine($"{order.EquipmentData.DisplayName}  +{order.StartingLevel} → +{order.TargetLevel}");
                rows.AppendLine($"추가된 빚: {debt:N0}G");
            }
            else if (order.TryMarkLastDayWarning(day)) warn = true;
        }
        if (count == 0 && !warn) return;

        ResetSubmissionSelection();
        if (count > 0)
        {
            isWindowOpen = false;
            inventoryUI.RefreshAfterCommissionChange();
            deadlineText.text = $"만료 의뢰 {count}건 추가된 빚 합계 {total:N0}G\n\n" + rows.ToString().TrimEnd();
            if (warn) deadlineText.text = warning + "\n\n" + deadlineText.text;
        }
        else warningText.text = warning;
        RefreshUI();
        SetDeadlineView(count > 0 ? DeadlineView.Result : DeadlineView.Warning);
        if (count == 0) return;
        noticeScroll.movementType = ScrollRect.MovementType.Clamped;
        Canvas.ForceUpdateCanvases();
        noticeScroll.StopMovement();
        noticeScroll.verticalNormalizedPosition = 1f;
    }

    /// <summary>
    /// 수동 포기 확인창에 회수금, 즉시 지불액과 추가될 빚을 표시한다.
    /// </summary>
    private static string FormatQuote(AbandonmentQuset q) =>
        $"총 회수금: {q.RecoveryGold:N0}G\n골드 차감: {q.PayGold:N0}G\n추가 빚: {q.AddedDebt:N0}G";

    /// <summary>
    /// 기한 알림을 열거나 닫을 때 팝업 표시와 의뢰 카드 입력, 날짜 진행 가능 여부를 갱신한다.
    /// </summary>
    private void SetDeadlineView(DeadlineView next)
    {
        deadlineView = next;
        foreach (var card in cards.Values) card.Render(mode == SelectionMode.Normal && !DeadlineActive);
        calendarUI.DayAdvanceBlocked = DeadlineActive;
        warningText.gameObject.SetActive(next == DeadlineView.Warning);
        noticeScroll.gameObject.SetActive(next == DeadlineView.Result);
        deadlinePopup.SetActive(DeadlineActive);
        if (DeadlineActive) deadlinePopup.transform.SetAsLastSibling();
        noticeClose.gameObject.SetActive(DeadlineActive);
    }

    /// <summary>
    /// 경고나 정산 결과를 닫는다.
    /// 닫을 때에는 골드를 차감하지 않는다.
    /// </summary>
    public void OnDeadlineNoticeCloseClicked()
    {
        if (deadlineView == DeadlineView.Warning || deadlineView == DeadlineView.Result)
            SetDeadlineView(DeadlineView.None);
    }
}
