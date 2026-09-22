using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BellHit : MonoBehaviour
{
    [Header("•\¦‚·‚éText")]
    [SerializeField] private TMP_Text _remainingText;

    [Header("ƒ_ƒ[ƒW‘S‘Ì‚Ì”{—¦")]
    [SerializeField, Min(0f)]
    private float _damageScale = 0.01f;

    [Header("‹­ã‚Ì·")]
    [SerializeField, Range(0.5f, 2f)]
    private float _damageExponent = 1f;

    [Header("‚±‚ê‚æ‚èã‚¢ˆĞ—Í‚Í–³‹‚·‚é")]
    [SerializeField, Min(0f)]
    private float _minimumImpact = 0.1f;

    [Header("˜A‘±–½’†‚ğ–h‚®ŠÔŠu")]
    [SerializeField, Min(0f)]
    private float _hitInterval = 0.15f;

    [SerializeField] private GameObject _damageObject;
    [SerializeField] private Transform _damageTextPos;

    [Header("à‚ÌSE")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _hitSE;

    [Header("c‚è”Ï”Y‚Ìƒo[")]
    [SerializeField] private BonnouBar _bonnouBar;

    [Header("ƒ^ƒCƒ}[")]
    [SerializeField] private GameTimer _timer;

    [Header("‚²—˜‰v‚Ì‰æ‘œPrefab")]
    [SerializeField] private GameObject _benefitPrefab;

    private int _remaining = 108;
    private int _benefit;
    private float _nextHitTime;

    private bool _canReceiveHit = true;
    private bool _isBenefitTime;

    // ¶¬‚µ‚½‰æ‘œ‚¾‚¯‚ğA’â~‚ÉÁ‚·‚½‚ß‚ÌƒŠƒXƒg
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

        // ‚±‚ÌˆêŒ‚‚ª“–‚½‚Á‚½“_‚Ìó‘Ô‚Å•\¦‚·‚é
        ShowDamage(damage, _isBenefitTime);

        if (_isBenefitTime)
        {
            _benefit += damage;

            // ¡‰ñŠl“¾‚µ‚½‚²—˜‰v‚ğ“n‚·
            SpawnBenefit(damage);
        }
        else
        {
            _remaining = Mathf.Max(0, _remaining - damage);

            if (_bonnouBar != null)
                _bonnouBar.SetRemaining(_remaining);

            if (_remaining == 0 && _timer.StartBenefitTime())
            {
                _isBenefitTime = true;

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

        // à‚ÌˆÊ’u‚ÉA‚²—˜‰vƒAƒCƒeƒ€‚ğ¶¬
        GameObject obj = Instantiate(
            _benefitPrefab,
            transform.position,
            Quaternion.identity
        );

        // 1‚²—˜‰v‚Å0A16‚²—˜‰vˆÈã‚Å1‚É‚È‚é
        float strength = Mathf.InverseLerp(1f, 16f, benefit);

        // Prefab‚Ì‘å‚«‚³‚ğŠî€‚ÉA1`2”{‚É‚·‚é
        float sizeMultiplier = Mathf.Lerp(1f, 2f, strength);

        obj.transform.localScale =
            _benefitPrefab.transform.localScale * sizeMultiplier;

        // ’â~‚ÉÁ‚¹‚é‚æ‚¤‚É‹L˜^
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
                $"<color=#00CC66>æ“¾‚µ‚½‚²—˜‰v {_benefit}</color>";
        }
        else
        {
            _remainingText.text = $"c‚è”Ï”Y{_remaining}";
        }
    }

    private void OnDestroy()
    {
        ClearBenefits();
    }
}