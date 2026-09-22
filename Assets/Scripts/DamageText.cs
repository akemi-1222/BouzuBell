using TMPro;
using UnityEngine;

public class DamageText : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;

    [Header("上に移動する速さ")]
    [SerializeField] private float _moveSpeed = 60f;

    [Header("表示する時間")]
    [SerializeField, Min(0.1f)] private float _deleteTime = 0.8f;

    private RectTransform _rect;
    private Vector2 _startPosition;
    private Color _startColor;
    private float _elapsed;
    private bool _showing;

    public void Show(int damage)
    {
        if (_text == null)
        {
            Debug.LogError("DamageTextのTextを設定してください。", this);
            Destroy(gameObject);
            return;
        }

        _rect = GetComponent<RectTransform>();
        _startPosition = _rect.anchoredPosition;
        _startColor = _text.color;
        _startColor.a = 1f;

        _elapsed = 0f;
        _showing = true;

        _text.enabled = true;
        _text.enableAutoSizing = false;
        _text.richText = true;
        _text.color = _startColor;

        //数字を大きく、「煩悩」を小さく表示
        _text.text = $"{damage}<size=55%>煩悩</size>";

        // 1～16の範囲で、強い打撃ほど少し大きくする
        float strength = Mathf.InverseLerp(1f, 16f, damage);
        _text.fontSize = Mathf.Lerp(60f, 120f, strength);

        transform.localScale = Vector3.one * 1.2f;
    }

    private void Update()
    {
        if (!_showing)
            return;

        _elapsed += Time.deltaTime;

        float lifetime = Mathf.Max(0.1f, _deleteTime);
        float progress = Mathf.Clamp01(_elapsed / lifetime);

        //UIの座標で上へ移動する
        _rect.anchoredPosition =
            _startPosition + Vector2.up * _moveSpeed * _elapsed;

        //出現直後だけ大きくし、通常サイズへ戻す
        float pop = Mathf.Clamp01(_elapsed / 0.12f);
        transform.localScale =
            Vector3.one * Mathf.Lerp(1.2f, 1f, pop);

        //前半は見せ、後半で消す
        float fade = Mathf.InverseLerp(0.5f, 1f, progress);
        Color color = _startColor;
        color.a = 1f - fade;
        _text.color = color;

        if (progress >= 1f)
            Destroy(gameObject);
    }
}