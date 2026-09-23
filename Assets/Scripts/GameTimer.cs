using TMPro;
using UnityEngine;

public class GameTimer : MonoBehaviour
{
    [Header("残り時間のText")]
    [SerializeField] private TMP_Text _timerText;

    [Header("結果画面の管理")]
    [SerializeField] private ResultController _resultController;

    [Header("ご利益タイムのBGM")]
    [SerializeField] private AudioSource _benefitBGM;

    private Color _normalTimerColor;

    private const float LimitTime = 30f;

    private float _endTime;
    private bool _isRunning;
    private bool _isBenefitTime;

    public bool IsRunning =>
        _isRunning && Time.time < _endTime;

    private void Awake()
    {
        if (_timerText == null)
            _timerText = GetComponent<TMP_Text>();

        _normalTimerColor = _timerText.color;

        _timerText.text = "年明けまで残り30秒";
        _timerText.enabled = false;
    }

    private void Update()
    {
        if (!_isRunning)
            return;

        UpdateText();

        if (Time.time < _endTime)
            return;

        _isRunning = false;
        _timerText.enabled = false;

        _resultController.FinishGame();

        if (_benefitBGM != null)
            _benefitBGM.Stop();
    }

    public void StartTimer()
    {
        if (_benefitBGM != null)
            _benefitBGM.Stop();

        _resultController.ResetResult();

        _endTime = Time.time + LimitTime;
        _isRunning = true;
        _isBenefitTime = false;

        _timerText.enabled = true;
        UpdateText();
    }

    public bool StartBenefitTime()
    {
        // 時間切れ後や、2回目以降の加算はしない
        if (!IsRunning || _isBenefitTime)
            return false;

        _isBenefitTime = true;
        _endTime += 10f;

        if (_benefitBGM != null)
            _benefitBGM.Play();

        _resultController.ShowBenefitBackground();
        UpdateText();

        return true;
    }

    public void ResetTimer()
    {
        if (_benefitBGM != null)
            _benefitBGM.Stop();

        _isRunning = false;
        _isBenefitTime = false;

        _timerText.text = "年明けまで残り30秒";
        _timerText.color = _normalTimerColor;
        _timerText.enabled = false;

        _resultController.ResetResult();
    }

    private void UpdateText()
    {
        float remaining = Mathf.Max(0f, _endTime - Time.time);
        int seconds = Mathf.CeilToInt(remaining);

        // テキスト全体の色は通常に戻す
        _timerText.color = _normalTimerColor;
        _timerText.richText = true;

        string number = seconds.ToString();

        if (seconds >= 1 && seconds <= 5)
        {
            float elapsed = 5f - remaining;
            bool visible = Mathf.Repeat(elapsed, 0.5f) < 0.25f;

            // 数字だけ、赤色と透明を切り替える
            string color = visible ? "#FF0000FF" : "#FF000000";
            number = $"<color={color}>{seconds}</color>";
        }

        _timerText.text = $"年明けまで残り{number}秒";
    }
}