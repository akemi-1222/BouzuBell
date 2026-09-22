using TMPro;
using UnityEngine;

public class ResultController : MonoBehaviour
{
    [Header("c‚è”Ï”Y‚ğŠÇ—‚·‚éBell")]
    [SerializeField] private BellHit _bell;

    [Header("Œ‹‰Ê‰æ–Ê")]
    [SerializeField] private GameObject _resultUI;
    [SerializeField] private TMP_Text _happyNewYearText;
    [SerializeField] private TMP_Text _resultText;

    [Header("à‚Ì‰æ‘œ")]
    [SerializeField] private SpriteRenderer _normalBell;
    [SerializeField] private GameObject _newYearBell;

    [Header("”wŒi")]
    [SerializeField] private SpriteRenderer _background;
    [SerializeField] private Sprite _resultBackground;

    private Sprite _originalBackground;
    private bool _originalBellVisible;
    private bool _hasFinished;

    private void Awake()
    {
        //’â~ƒ{ƒ^ƒ“‚ÅŒ³‚É–ß‚¹‚é‚æ‚¤‚ÉŠo‚¦‚Ä‚¨‚­
        _originalBackground = _background.sprite;
        _originalBellVisible = _normalBell.enabled;

        ResetResult();
    }

    //ƒ^ƒCƒ}[‚ª0‚É‚È‚Á‚½‚Æ‚«‚ÉŒÄ‚Ô
    public void FinishGame()
    {
        if (_hasFinished)
            return;

        _hasFinished = true;

        //Œ‹‰Ê‚Ì”’l‚ğŠm’è‚·‚é
        _bell.StopReceivingHits();

        int remaining = _bell.Remaining;

        //”Ï”Y‚ª0‚È‚çA¡‰ñ‚ÌŒ‹‰Ê‰æ–Ê‚Í•\¦‚µ‚È‚¢
        if (remaining <= 0)
            return;

        _happyNewYearText.text = "HAPPY NEW YEAR";
        _resultText.text = $"âP‚¦‚È‚©‚Á‚½”Ï”Y {remaining}";

        _resultUI.SetActive(true);

        //’Êí‚Ìà‚Ì‰æ‘œ‚ğ‰B‚µ‚ÄA‚¨³Œ‚Ì‰æ‘œ‚ğ•\¦
        _normalBell.enabled = false;
        _newYearBell.SetActive(true);

        //”wŒi‚ğØ‚è‘Ö‚¦‚é
        _background.sprite = _resultBackground;

        //U‚èq‚Í~‚ß‚È‚¢
    }

    //’â~‚âÄƒvƒŒƒC‚ÉŒ³‚Ì•\¦‚Ö–ß‚·
    public void ResetResult()
    {
        _hasFinished = false;

        _resultUI.SetActive(false);
        _newYearBell.SetActive(false);

        _normalBell.enabled = _originalBellVisible;
        _background.sprite = _originalBackground;
    }
}