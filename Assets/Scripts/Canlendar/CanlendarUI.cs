using TMPro;
using UnityEngine;

/// <summary>
/// 메인 화면에 현재 날짜를 표시하고 하루 종료 버튼의 입력을 받는다.
/// </summary>
public class CanlendarUI : MonoBehaviour
{
    private static readonly string[] WeekdatLabels = { "월", "화", "수", "목", "금", "토", "일" };

    [SerializeField] private TMP_Text dayText;

    private readonly GameCanlendar calendar = new GameCanlendar();

    public GameCanlendar Calendar => calendar;
    public bool DayAdvanceBlocked { get; set; }

    /// <summary>
    /// 화면을 처음 열거나 다시 활성화하면 현재 날짜를 표시
    /// </summary>
    private void OnEnable()
    {
        if (dayText == null)
        {
            Debug.LogError("CalendarUI의 Day Text에 날짜 텍스트를 연결하세요.", this);
            return;
        }

        RefreshDayText();
    }

    /// <summary>
    /// 하루 종료 버튼의 On Click()에서 호출
    /// 날짜를 먼저 바꾼 뒤 화면을 갱신
    /// </summary>
    public void OnEndDayClicked()
    {
        if (dayText == null || DayAdvanceBlocked) return;

        calendar.AdvanceDay();
        RefreshDayText();
    }

    /// <summary>
    /// 화면이 활성화되거나 하루가 지난 직후 날짜를 'DAY1' 형식으로 표시
    /// </summary>
    private void RefreshDayText()
    {
        int day = calendar.DayOfWeekNumber;
        dayText.text = $"Week{calendar.CurrentWeek}/Day{day}/{WeekdatLabels[day -1]}";
    }
}
