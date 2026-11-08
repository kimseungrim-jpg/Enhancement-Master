using System;

/// <summary>
/// 현재 날짜를 보관하고 하루 단위로 진행
/// </summary>
public class GameCanlendar
{
    public event Action<int> DayChanged;

    private const int DaysPerWeek = 7;

    public int CurrentDay { get; private set; } = 1;

    public int CurrentWeek => (CurrentDay - 1) / DaysPerWeek + 1;
    public int DayOfWeekNumber => (CurrentDay - 1) % DaysPerWeek + 1;

    /// <summary>
    /// 하루 종료 버튼을 눌렀을 때 현재 날짜를 하루 늘린다.
    /// </summary>
    public void AdvanceDay()
    {
        CurrentDay++;

        DayChanged?.Invoke(CurrentDay);
    }
}
