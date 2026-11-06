/// <summary>
/// 포기 확인 화면에 표시할 회수금과 지불 방식을 보관
/// 생성만으로 자원을 변경하지 않는다.
/// </summary>
public class AbandonmentQuset
{
    public long RecoveryGold { get; }
    public long PayGold { get; }
    public long AddedDebt { get; }

    /// <summary>
    /// Commission.TryGetAbandonmentQuote가 현재 지갑 상태로 정산 미리보기를 만들 때 호출
    /// </summary>
    /// <param name="recoveryGold"></param>
    /// <param name="wallet"></param>
    public AbandonmentQuset(long recoveryGold, GoldWallet wallet)
    {
        RecoveryGold = recoveryGold;
        bool sufficient = wallet.Gold >= recoveryGold;
        PayGold = sufficient ? recoveryGold : 0;
        AddedDebt = sufficient ? 0 : recoveryGold;
    }
}
