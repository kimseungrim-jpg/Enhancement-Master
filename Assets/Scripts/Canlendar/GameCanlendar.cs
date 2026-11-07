using UnityEngine;

/// <summary>
/// 현재 날짜를 보관하고 하루 단위로 진행
/// </summary>
public class GameCanlendar
{
    public int CurrentDay { get; private set; } = 1;

    /// <summary>
    /// 하루 종료 버튼을 눌렀을 때 현재 날짜를 하루 늘린다.
    /// </summary>
    public void AdvanceDay()
    {
        CurrentDay++;
    }
}
