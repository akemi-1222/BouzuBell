using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BellHit : MonoBehaviour
{
    [Header("表示するText")]
    [SerializeField] private TMP_Text _remainingText;

    [Header("ダメージ全体の倍率")]
    [SerializeField, Min(0f)]
    private float _damageScale = 0.01f;

    [Header("強弱の差")]
    [SerializeField, Range(0.5f, 2f)]
    private float _damageExponent = 1f;

    [Header("これより弱い威力は無視する")]
    [SerializeField, Min(0f)]
    private float _minimumImpact = 0.1f;

    [Header("連続命中を防ぐ間隔")]
    [SerializeField, Min(0f)]
    private float _hitInterval = 0.15f;

    [SerializeField] private GameObject _damageObject;
    [SerializeField] private Transform _damageTextPos;

    [Header("鐘のSE")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _hitSE;

    [Header("残り煩悩のバー")]
    [SerializeField] private BonnouBar _bonnouBar;

    [Header("タイマー")]
    [SerializeField] private GameTimer _timer;

    [Header("ご利益の画像Prefab")]
    [SerializeField] private GameObject _benefitPrefab;

    [Header("ご利益タイムの画面揺れ")]
    [SerializeField] private CameraShake _cameraShake;

    private int _remaining = 108;
    private int _benefit;
    private float _nextHitTime;

    private bool _canReceiveHit = true;
    private bool _isBenefitTime;

    // 生成した画像だけを、停止時に消すためのリスト
    private readonly List<GameObject> _benefitObjects =
        new List<GameObject>();

    public int Remaining => _remaining;
    public int Benefit => _benefit;
    public bool IsBenefitTime => _isBenefitTime;

    private void Awake()
    {
        ResetBell();
    }

    public void ResetBell()
    {
        ClearBenefits();

        _remaining = 108;
        _benefit = 0;
        _nextHitTime = 0f;

        _canReceiveHit = true;
        _isBenefitTime = false;

        if (_bonnouBar != null)
            _bonnouBar.ResetBar();

        UpdateText();
    }

    public void StopReceivingHits()
    {
        _canReceiveHit = false;
    }

    public bool ReceiveHit(float power)
    {
        if (!_canReceiveHit || _timer == null || !_timer.IsRunning)
            return false;

        if (power <= 0f || power < _minimumImpact)
            return false;

        if (Time.time < _nextHitTime)
            return false;

        _nextHitTime = Time.time + _hitInterval;

        if (_audioSource != null && _hitSE != null)
            _audioSource.PlayOneShot(_hitSE);

        float baseDamage = power * _damageScale;
        float calculatedDamage =
            Mathf.Pow(baseDamage, _damageExponent);

        int damage =
            Mathf.Max(1, Mathf.CeilToInt(calculatedDamage));

        // この一撃が当たった時点の状態で表示する
        ShowDamage(damage, _isBenefitTime);

        if (_isBenefitTime)
        {
            _benefit += damage;

            SpawnBenefit(damage);

            if (_cameraShake != null)
                _cameraShake.Shake(damage);
        }
        else
        {
            _remaining = Mathf.Max(0, _remaining - damage);

            if (_bonnouBar != null)
                _bonnouBar.SetRemaining(_remaining);

            if (_remaining == 0 && _timer.StartBenefitTime())
            {
                _isBenefitTime = true;

                // ご利益は108から開始
                _benefit = 108;

                if (_bonnouBar != null)
                    _bonnouBar.SetBenefitMode(true);
            }
        }

        UpdateText();
        return true;
    }

    private void ShowDamage(int damage, bool isBenefit)
    {
        if (_damageObject == null || _damageTextPos == null)
            return;

        GameObject obj =
            Instantiate(_damageObject, _damageTextPos);

        RectTransform rect = obj.GetComponent<RectTransform>();

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition3D = Vector3.zero;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;

        obj.GetComponent<DamageText>().Show(damage, isBenefit);
    }

    private void SpawnBenefit(int benefit)
    {
        if (_benefitPrefab == null)
            return;

        // 鐘の位置に、ご利益アイテムを生成
        GameObject obj = Instantiate(
            _benefitPrefab,
            transform.position,
            Quaternion.identity
        );

        // 1ご利益で0、16ご利益以上で1になる
        float strength = Mathf.InverseLerp(1f, 16f, benefit);

        // Prefabの大きさを基準に、1～2倍にする
        float sizeMultiplier = Mathf.Lerp(1f, 2f, strength);

        obj.transform.localScale =
            _benefitPrefab.transform.localScale * sizeMultiplier;

        // 停止時に消せるように記録
        _benefitObjects.Add(obj);
    }

    public void ClearBenefits()
    {
        foreach (GameObject obj in _benefitObjects)
        {
            if (obj == null)
                continue;

            obj.SetActive(false);
            Destroy(obj);
        }

        _benefitObjects.Clear();
    }

    private void UpdateText()
    {
        if (_remainingText == null)
            return;

        _remainingText.richText = true;

        if (_isBenefitTime)
        {
            _remainingText.text =
                $"<color=#00CC66>取得したご利益 {_benefit}</color>";
        }
        else
        {
            _remainingText.text = $"残り煩悩{_remaining}";
        }
    }

    private void OnDestroy()
    {
        ClearBenefits();
    }
}