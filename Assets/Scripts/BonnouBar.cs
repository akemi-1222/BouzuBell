using UnityEngine;
using UnityEngine.UI;

public class BonnouBar : MonoBehaviour
{
    [Header("残り煩悩：緑")]
    [SerializeField] private Image _remainingBar;

    [Header("減った分：赤")]
    [SerializeField] private Image _damageBar;

    [Header("赤い部分を残す時間")]
    [SerializeField, Min(0f)] private float _delay = 0.3f;

    [Header("赤いバーが縮む速さ")]
    [SerializeField, Min(0.01f)] private float _shrinkSpeed = 0.5f;

    [Header("バーの背景画像")]
    [SerializeField] private Image _backgroundImage;

    private const float MaxBonnou = 108f;

    private float _targetFill = 1f;
    private float _waitTime;

    // ゲーム開始時に満タンへ戻す
    public void ResetBar()
    {
        _targetFill = 1f;
        _waitTime = 0f;

        _remainingBar.fillAmount = 1f;
        _damageBar.fillAmount = 1f;

        SetBenefitMode(false);
    }

    // ダメージを受けた後の残り煩悩を渡す
    public void SetRemaining(int remaining)
    {
        float nextFill = Mathf.Clamp01(remaining / MaxBonnou);

        // 値が増えた場合も正しく表示する
        if (nextFill >= _targetFill)
            _damageBar.fillAmount = nextFill;

        _targetFill = nextFill;

        // 緑はすぐ減らす
        _remainingBar.fillAmount = _targetFill;

        // 赤は少し待ってから減らす
        _waitTime = _delay;
    }

    private void Update()
    {
        if (_waitTime > 0f)
        {
            _waitTime -= Time.deltaTime;
            return;
        }

        _damageBar.fillAmount = Mathf.MoveTowards(
            _damageBar.fillAmount,
            _targetFill,
            _shrinkSpeed * Time.deltaTime
        );
    }

    public void SetBenefitMode(bool isBenefit)
    {
        bool showBar = !isBenefit;

        _remainingBar.enabled = showBar;
        _damageBar.enabled = showBar;

        if (_backgroundImage != null)
            _backgroundImage.enabled = showBar;
    }
}