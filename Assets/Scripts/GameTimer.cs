using TMPro;
using UnityEngine;

public class GameTimer : MonoBehaviour
{
    [Header("残り時間を表示するText")]
    [SerializeField] private TMP_Text _timerText;

    private const float LimitTime = 30f;

    private float _remainingTime = LimitTime;
    private bool _isRunning;

    private void Awake()
    {
        if (_timerText == null)
        {
            _timerText = GetComponent<TMP_Text>();
        }

        if (_timerText == null)
        {
            Debug.LogError("タイマー用のTextを設定してください。", this);
            enabled = false;
            return;
        }

        ResetTimer();
    }

    private void Update()
    {
        if (!_isRunning)
            return;

        _remainingTime -= Time.deltaTime;

        if (_remainingTime <= 0f)
        {
            _remainingTime = 0f;
            _isRunning = false;
        }

        UpdateText();
    }

    // プレイ開始：表示してカウントする
    public void StartTimer()
    {
        if (_timerText == null)
            return;

        _remainingTime = LimitTime;
        _isRunning = true;

        _timerText.enabled = true;
        UpdateText();
    }

    // 停止：30秒に戻して非表示にする
    public void ResetTimer()
    {
        _isRunning = false;
        _remainingTime = LimitTime;

        if (_timerText == null)
            return;

        UpdateText();
        _timerText.enabled = false;
    }

    private void UpdateText()
    {
        int seconds = Mathf.CeilToInt(_remainingTime);
        _timerText.text = $"年明けまで残り{seconds}秒";
    }
}