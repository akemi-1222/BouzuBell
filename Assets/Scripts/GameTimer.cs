using TMPro;
using UnityEngine;

public class GameTimer : MonoBehaviour
{
    [Header("Žc‚èŽžŠÔ‚ÌText")]
    [SerializeField] private TMP_Text _timerText;

    [Header("Œ‹‰Ê‰æ–Ê‚ÌŠÇ—")]
    [SerializeField] private ResultController _resultController;

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

        _timerText.text = "”N–¾‚¯‚Ü‚ÅŽc‚è30•b";
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
    }

    public void StartTimer()
    {
        _resultController.ResetResult();

        _endTime = Time.time + LimitTime;
        _isRunning = true;
        _isBenefitTime = false;

        _timerText.enabled = true;
        UpdateText();
    }

    public bool StartBenefitTime()
    {
        // ŽžŠÔØ‚êŒã‚âA2‰ñ–ÚˆÈ~‚Ì‰ÁŽZ‚Í‚µ‚È‚¢
        if (!IsRunning || _isBenefitTime)
            return false;

        _isBenefitTime = true;
        _endTime += 10f;

        _resultController.ShowBenefitBackground();
        UpdateText();

        return true;
    }

    public void ResetTimer()
    {
        _isRunning = false;
        _isBenefitTime = false;

        _timerText.text = "”N–¾‚¯‚Ü‚ÅŽc‚è30•b";
        _timerText.enabled = false;

        _resultController.ResetResult();
    }

    private void UpdateText()
    {
        float remaining = Mathf.Max(0f, _endTime - Time.time);
        int seconds = Mathf.CeilToInt(remaining);

        _timerText.text = $"”N–¾‚¯‚Ü‚ÅŽc‚è{seconds}•b";
    }
}