using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 의뢰 한 건의 조건과 버튼을 표시하고, 누른 카드의 의뢰를 의뢰창에 전달한다.
/// </summary>
public sealed class CommissionCardUI : MonoBehaviour
{
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button acceptBtn, submitBtn, abandonBtn;
    private CommissionTestUI owner;
    private Commission order;
    public int Number { get; private set; }

    /// <summary>
    /// 카드 생성 직후 한 번 호출
    /// 프리팹 버튼이 이 카드의 의뢰만 선택하게 연결한다.
    /// </summary>
    public void Bind(CommissionTestUI board, Commission item, int number)
    {
        owner = board;
        order = item;
        Number = number;
        acceptBtn.onClick.AddListener(Accept);
        submitBtn.onClick.AddListener(Submit);
        abandonBtn.onClick.AddListener(Abandon);
    }

    private void Accept() => owner.OnAcceptClicked(order);
    private void Submit() => owner.OnSubmitClicked(order);
    private void Abandon() => owner.OnAbandonClicked(order);

    /// <summary>
    /// 카드가 사라질 때 Bind에서 등록한 세 버튼의 연결을 해제한다.
    /// </summary>
    private void OnDestroy()
    {
        if (order == null) return;
        acceptBtn.onClick.RemoveListener(Accept);
        submitBtn.onClick.RemoveListener(Submit);
        abandonBtn.onClick.RemoveListener(Abandon);
    }

    /// <summary>
    /// 의뢰창을 갱신할 때 현재 상태에서 사용할 버튼과 마지막 제출일을 표시한다.
    /// </summary>
    public void Render(bool canClick)
    {
        bool active = order.State == CommissionState.InProgress;
        descriptionText.text = $"의뢰 #{Number}  {order.EquipmentData.DisplayName}\n지급 +{order.StartingLevel} / 목표 +{order.TargetLevel}\n선금 {order.AdvanceGold:N0} 골드";
        if (active) descriptionText.text += $"\n수락: {order.AcceptedDay}일 / 마지막 제출: {order.ExpiryDay - 1}일";
        acceptBtn.gameObject.SetActive(!active);
        submitBtn.gameObject.SetActive(active);
        abandonBtn.gameObject.SetActive(active);
        acceptBtn.interactable = submitBtn.interactable = abandonBtn.interactable = canClick;
    }
}
