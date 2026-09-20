using UnityEngine;
using UnityEngine.UI;

public class HammerSelectButtom : MonoBehaviour
{
    [Header("Hammerの画像表示")]
    [SerializeField] private SpriteRenderer _hammerRenderer;

    [Header("Hammerの画像")]
    [SerializeField] private Sprite _woodSprite;
    [SerializeField] private Sprite _metalSprite;

    [Header("ボタンの画像表示")]
    [SerializeField] private Image _buttonImage;

    [Header("ボタンの画像")]
    [SerializeField] private Sprite _woodButtonSprite;
    [SerializeField] private Sprite _metalButtonSprite;

    //最初は金属
    private bool _isWood = false;

    //ほかのスクリプトから、木材かどうか確認する
    public bool IsWood
    {
        get { return _isWood; }
    }

    private void Start()
    {
        if (_hammerRenderer == null ||
            _woodSprite == null ||
            _metalSprite == null ||
            _buttonImage == null ||
            _woodButtonSprite == null ||
            _metalButtonSprite == null)
        {
            Debug.LogError(
                "Hammerとボタンの画像を設定してください。",
                this
            );

            enabled = false;
            return;
        }

        UpdateImages();
    }

    //ボタンを押すたびに切り替える
    public void SwitchHammer()
    {
        if (!isActiveAndEnabled)
            return;

        _isWood = !_isWood;

        UpdateImages();
    }

    // Hammerとボタンの表示を更新する
    private void UpdateImages()
    {
        if (_isWood)
        {
            _hammerRenderer.sprite = _woodSprite;
            _buttonImage.sprite = _woodButtonSprite;
        }
        else
        {
            _hammerRenderer.sprite = _metalSprite;
            _buttonImage.sprite = _metalButtonSprite;
        }
    }
}