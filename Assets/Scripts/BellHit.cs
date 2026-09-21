using TMPro;
using UnityEngine;

public class BellHit : MonoBehaviour
{
    [Header("表示するText")]
    [SerializeField] private TMP_Text _remainingText;
    [SerializeField] private TMP_Text _damageText;

    [Header("衝突の強さをダメージに変換する倍率")]
    [SerializeField, Min(0f)] private float _damageScale = 0.2f;

    [Header("これより弱い衝突は無視する")]
    [SerializeField, Min(0f)] private float _minimumImpact = 0.1f;

    [Header("連続命中を防ぐ間隔：秒")]
    [SerializeField, Min(0f)] private float _hitInterval = 0.15f;

    private int _remaining = 108;
    private float _nextHitTime;

    private void Awake()
    {
        ResetBell();
    }

    // プレイ開始時に呼ぶ
    public void ResetBell()
    {
        _remaining = 108;
        _nextHitTime = 0f;

        if (_damageText != null)
        {
            _damageText.text = "";
        }

        UpdateText();
    }

    // 命中を受け付けた場合はtrueを返す
    public bool ReceiveHit(float impact)
    {
        if (_remaining <= 0)
            return false;

        if (impact < _minimumImpact)
            return false;

        if (Time.time < _nextHitTime)
            return false;

        _nextHitTime = Time.time + _hitInterval;

        // 衝突が強いほどダメージを増やす
        int damage = Mathf.Max(
            1,
            Mathf.CeilToInt(impact * _damageScale)
        );

        _remaining = Mathf.Max(0, _remaining - damage);

        if (_damageText != null)
        {
            _damageText.text = $"{damage}煩悩";
        }

        UpdateText();

        return true;
    }

    private void UpdateText()
    {
        if (_remainingText != null)
        {
            _remainingText.text = $"残り煩悩{_remaining}";
        }
    }
}