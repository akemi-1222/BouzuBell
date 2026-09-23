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

    [Header("画面下の残り煩悩・ご利益テキスト")]
    [SerializeField] private TMP_Text _bottomText;

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

        _happyNewYearText.richText = true;
        _resultText.richText = true;

        if (_bell.IsBenefitTime)
        {
            // ご利益タイムの結果は緑色
            _happyNewYearText.text =
                "<color=#00CC66>HAPPY NEW YEAR</color>";

            _resultText.text =
                $"<color=#00CC66>取得したご利益 {_bell.Benefit}</color>";

            _background.sprite = _benefitBackground;

            // 画面下の取得数を隠す
            if (_bottomText != null)
                _bottomText.enabled = false;
        }
        else
        {
            // 通常の結果はInspectorで設定した色
            _happyNewYearText.text = "HAPPY NEW YEAR";

            _resultText.text =
                $"祓えなかった煩悩 {_bell.Remaining}";

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