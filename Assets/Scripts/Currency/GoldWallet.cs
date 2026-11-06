using System;

/// <summary>
/// 보유 골드와 흭득/지출을 관리, 잔액이 부족하면 지출하지 않는다.
/// 장비나 UI를 참조하지 않고 전달받은 금액만 처리
/// </summary>
public class GoldWallet
{
    public int Gold { get; private set; }

    public long Debt { get; private set; }

    /// <summary>
    /// 정산 처리에서 골드가 부족할 때 호출하여 빚을 누적
    /// </summary>
    public void AddDebt(long amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Debt = checked(Debt + amount);
    }

    /// <summary>
    /// Commission.TryAbandon에서 장비 회수 전에 금액과 빚 누적 한도를 검사
    /// </summary>
    public bool CanSettle(long amount)
    {
        return amount >= 0 && (amount <= Gold || amount <= long.MaxValue - Debt);
    }

    /// <summary>
    /// Commission.TryAbandon에서 포기를 확정할 때 호출
    /// 잔액이 부족하면 골드는 보존하고 회수금 전액을 빚으로 추가
    /// </summary>
    public bool TrySettle(long amount)
    {
        if(!CanSettle(amount)) return false;
        if (amount <= Gold)
            Gold -= (int)amount;
        else
            AddDebt(amount);
        return true;
    }

    /// <summary>
    /// EnhancementTestUI.Awake에서 테스트 지갑을 만들 때 시작 골드를 설정
    /// </summary
    public GoldWallet(int startingGold)
    {
        if(startingGold < 0)
            throw new ArgumentOutOfRangeException(nameof(startingGold));

        Gold = startingGold;
    }

    /// <summary>
    /// EnhancementService.TryEnhance에서 비용 지불을 요청할 때 호출
    /// 잔액이 충분하면 차감 후 true, 부족하면 잔액을 유지하고 false를 반환
    /// </summary>
    public bool TrySpend(int amount)
    {
        if(amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        if(Gold < amount)
            return false;

        Gold -= amount;
        return true;
    }

    /// <summary>
    /// EnhancementTestUI의 골드 추가 버튼에서 호출하여 잔액을 늘린다.
    /// </summary>
    public void AddGold(int amount)
    {
        if(amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        Gold = checked(Gold + amount);
    }
}
