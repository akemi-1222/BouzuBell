using TMPro;
using UnityEngine;

public class BellHit : MonoBehaviour
{
    [Header("表示するText")]
    [SerializeField] private TMP_Text _remainingText;

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

    [SerializeField] private GameObject _damageObject;
    [SerializeField] private Transform _damageTextPos;

    [Header("鐘のSE")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _hitSE;

    [Header("残り煩悩のバー")]
    [SerializeField] private BonnouBar _bonnouBar;

    private int _remaining = 108;
    private float _nextHitTime;

    //結果画面から残り煩悩を読めるようにする
    public int Remaining => _remaining;

    //時間切れ後はダメージを受け付けない
    private bool _canReceiveHit = true;

    public void StopReceivingHits()
    {
        _canReceiveHit = false;
    }

    private void Awake()
    {
        ResetBell();
    }

    public void ResetBell()
    {
        _canReceiveHit = true;

        _remaining = 108;
        _nextHitTime = 0f;

        if (_bonnouBar != null)
            _bonnouBar.ResetBar();

        UpdateText();
    }

    public bool ReceiveHit(float power)
    {
        if (!_canReceiveHit)
            return false;

        if (_remaining <= 0)
            return false;

        if (power <= 0f || power < _minimumImpact)
            return false;

        if (Time.time < _nextHitTime)
            return false;

        _nextHitTime = Time.time + _hitInterval;

        //鐘のSEを鳴らす
        if (_audioSource != null && _hitSE != null)
            _audioSource.PlayOneShot(_hitSE);

        //まず、ダメージ全体の大きさを決める
        float baseDamage = power * _damageScale;

        //強い打撃と弱い打撃の差を調整する
        float calculatedDamage = Mathf.Pow(baseDamage, _damageExponent);

        int damage = Mathf.Max(1, Mathf.CeilToInt(calculatedDamage));

        _remaining = Mathf.Max(0, _remaining - damage);

        if (_bonnouBar != null)
            _bonnouBar.SetRemaining(_remaining);

        UpdateText();

        if (_damageObject != null && _damageTextPos != null)
        {
            //Canvas内の表示位置を親にして生成する
            GameObject obj = Instantiate(_damageObject, _damageTextPos);

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition3D = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;

            obj.GetComponent<DamageText>().Show(damage);
        }

        return true;
    }

    private void UpdateText()
    {
        if (_remainingText != null)
            _remainingText.text = $"残り煩悩{_remaining}";
    }
}