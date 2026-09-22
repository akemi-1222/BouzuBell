using TMPro;
using UnityEngine;

public class ResultController : MonoBehaviour
{
    [Header("残り煩悩を管理するBell")]
    [SerializeField] private BellHit _bell;

    [Header("結果画面")]
    [SerializeField] private GameObject _resultUI;
    [SerializeField] private TMP_Text _happyNewYearText;
    [SerializeField] private TMP_Text _resultText;

    [Header("鐘の画像")]
    [SerializeField] private SpriteRenderer _normalBell;
    [SerializeField] private GameObject _newYearBell;

    [Header("背景")]
    [SerializeField] private SpriteRenderer _background;
    [SerializeField] private Sprite _resultBackground;

    [Header("ご利益タイムの背景")]
    [SerializeField] private Sprite _benefitBackground;

    private Sprite _originalBackground;
    private bool _originalBellVisible;
    private bool _hasFinished;

    private void Awake()
    {
        //停止ボタンで元に戻せるように覚えておく
        _originalBackground = _background.sprite;
        _originalBellVisible = _normalBell.enabled;

        ResetResult();
    }

    //タイマーが0になったときに呼ぶ
    public void FinishGame()
    {
        if (_hasFinished)
            return;

        _hasFinished = true;

        _bell.StopReceivingHits();

        _happyNewYearText.text = "HAPPY NEW YEAR";

        if (_bell.IsBenefitTime)
        {
            _resultText.richText = true;
            _resultText.text =
                $"<color=#00CC66>取得したご利益 {_bell.Benefit}</color>";

            // ご利益タイムの背景を残す
            _background.sprite = _benefitBackground;
        }
        else
        {
            _resultText.text = $"祓えなかった煩悩 {_bell.Remaining}";
            _background.sprite = _resultBackground;
        }

        _resultUI.SetActive(true);

        _normalBell.enabled = false;
        _newYearBell.SetActive(true);
    }

    //停止時や再プレイ時に元の表示へ戻す
    public void ResetResult()
    {
        _hasFinished = false;

        _resultUI.SetActive(false);
        _newYearBell.SetActive(false);

        _normalBell.enabled = _originalBellVisible;
        _background.sprite = _originalBackground;

        _bell.ClearBenefits();
    }

    public void ShowBenefitBackground()
    {
        _background.sprite = _benefitBackground;
    }

    public void ShowBrokenResult()
    {
        // 折れた後にタイマーが終了しても、
        // 通常の結果画面を表示しない
        _hasFinished = true;

        // 既に表示されている結果も消す
        _resultUI.SetActive(false);
        _newYearBell.SetActive(false);

        _bell.StopReceivingHits();
    }
}