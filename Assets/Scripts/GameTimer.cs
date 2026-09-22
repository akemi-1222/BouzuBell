using TMPro;
using UnityEngine;

public class GameTimer : MonoBehaviour
{
    [Header("残り時間のText")]
    [SerializeField] private TMP_Text _timerText;

    [Header("結果画面の管理")]
    [SerializeField] private ResultController _resultController;

    private const float LimitTime = 30f;

    private float _remainingTime = LimitTime;
    private bool _isRunning;

    private void Awake()
    {
        if (_timerText == null)
            _timerText = GetComponent<TMP_Text>();

        UpdateText();
        _timerText.enabled = false;
    }

    private void Update()
    {
        if (!_isRunning)
            return;

        _remainingTime = Mathf.Max(
            0f,
            _remainingTime - Time.deltaTime
        );

        UpdateText();

        if (_remainingTime > 0f)
            return;

        // 時間切れの処理は一度だけ行う
        _isRunning = false;
        _timerText.enabled = false;

        _resultController.FinishGame();
    }

    public void StartTimer()
    {
        _resultController.ResetResult();

        _remainingTime = LimitTime;
        _isRunning = true;

        _timerText.enabled = true;
        UpdateText();
    }

    public void ResetTimer()
    {
        _isRunning = false;
        _remainingTime = LimitTime;

        UpdateText();
        _timerText.enabled = false;

        _resultController.ResetResult();
    }

    private void UpdateText()
    {
        int seconds = Mathf.CeilToInt(_remainingTime);

        _timerText.text = $"年明けまで残り{seconds}秒";
    }
}