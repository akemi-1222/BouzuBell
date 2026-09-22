using TMPro;
using UnityEngine;

public class BellHit : MonoBehaviour
{
    [Header("表示するText")]
    [SerializeField] private TMP_Text _remainingText;
    [SerializeField] private TMP_Text _damageText;

    [Header("ダメージ全体の倍率")]
    [SerializeField, Min(0f)]
    private float _damageScale = 0.01f;

    [Header("強弱の差：1なら威力に比例")]
    [SerializeField, Range(0.5f, 2f)]
    private float _damageExponent = 1f;

    [Header("これより弱い威力は無視する")]
    [SerializeField, Min(0f)]
    private float _minimumImpact = 0.1f;

    [Header("連続命中を防ぐ間隔：秒")]
    [SerializeField, Min(0f)]
    private float _hitInterval = 0.15f;

    [Header("調整用のログを表示する")]
    [SerializeField] private bool _showHitLog = true;

    private int _remaining = 108;
    private float _nextHitTime;

    private void Awake()
    {
        ResetBell();
    }

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

    public bool ReceiveHit(float power)
    {
        if (_remaining <= 0)
            return false;

        if (power <= 0f || power < _minimumImpact)
            return false;

        if (Time.time < _nextHitTime)
            return false;

        _nextHitTime = Time.time + _hitInterval;

        // まず、ダメージ全体の大きさを決める
        float baseDamage = power * _damageScale;

        // 強い打撃と弱い打撃の差を調整する
        float calculatedDamage =
            Mathf.Pow(baseDamage, _damageExponent);

        int damage = Mathf.Max(
            1,
            Mathf.CeilToInt(calculatedDamage)
        );

        _remaining = Mathf.Max(0, _remaining - damage);

        if (_damageText != null)
        {
            _damageText.text = $"{damage}煩悩";
        }

        UpdateText();

        if (_showHitLog)
        {
            Debug.Log(
                $"命中：威力={power:F2} / " +
                $"基本値={baseDamage:F2}/" +
                $"ダメージ={damage}",
                this
            );
        }

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