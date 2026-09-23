using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [Header("—h‚ê‚éŠÔ")]
    [SerializeField, Min(0.01f)]
    private float _duration = 0.15f;

    [Header("ã‚¢‘ÅŒ‚‚Ì—h‚ê•")]
    [SerializeField, Min(0f)]
    private float _smallShake = 0.03f;

    [Header("‹­‚¢‘ÅŒ‚‚Ì—h‚ê•")]
    [SerializeField, Min(0f)]
    private float _largeShake = 0.12f;

    private float _remainingTime;
    private float _strength;
    private Vector3 _offset;

    public void Shake(int benefit)
    {
        if (!isActiveAndEnabled)
            return;

        float rate = Mathf.InverseLerp(1f, 16f, benefit);
        float strength = Mathf.Lerp(_smallShake, _largeShake, rate);

        // ˜A‘±–½’†‚µ‚Ä‚à—h‚ê•‚ğ‘«‚µ‘±‚¯‚È‚¢
        _strength = Mathf.Max(_strength, strength);
        _remainingTime = _duration;
    }

    private void LateUpdate()
    {
        // ‘O‚ÌƒtƒŒ[ƒ€‚Å‰Á‚¦‚½—h‚ê‚ğæ‚èœ‚­
        transform.position -= _offset;
        _offset = Vector3.zero;

        if (_remainingTime <= 0f)
        {
            _strength = 0f;
            return;
        }

        _remainingTime =
            Mathf.Max(0f, _remainingTime - Time.deltaTime);

        // I‚í‚è‚É‹ß‚Ã‚­‚Ù‚Ç—h‚ê‚ğ¬‚³‚­‚·‚é
        float fade = _remainingTime / _duration;
        Vector2 shake = Random.insideUnitCircle * _strength * fade;

        _offset = new Vector3(shake.x, shake.y, 0f);
        transform.position += _offset;

        if (_remainingTime <= 0f)
            _strength = 0f;
    }

    public void StopShake()
    {
        transform.position -= _offset;

        _offset = Vector3.zero;
        _remainingTime = 0f;
        _strength = 0f;
    }

    private void OnDisable()
    {
        StopShake();
    }
}