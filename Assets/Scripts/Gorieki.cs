using UnityEngine;

public class Gorieki : MonoBehaviour
{
    [Header("âÊñ Çà⁄ìÆÇ∑ÇÈë¨Ç≥")]
    [SerializeField] private float _speed = 0.08f;

    private Camera _camera;
    private Vector3 _viewportPosition;
    private Vector2 _direction;
    private float _phase;
    private float _elapsed;

    private void Start()
    {
        _camera = Camera.main;

        _viewportPosition =
            _camera.WorldToViewportPoint(transform.position);

        _direction = Random.insideUnitCircle.normalized;
        _phase = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;

        _viewportPosition.x +=
            _direction.x * _speed * Time.deltaTime;

        _viewportPosition.y +=
            _direction.y * _speed * Time.deltaTime;

        // âÊñ ÇÃí[Ç‹Ç≈êiÇÒÇæÇÁê‹ÇËï‘Ç∑
        const float margin = 0.1f;

        if (_viewportPosition.x < margin)
        {
            _viewportPosition.x = margin;
            _direction.x = Mathf.Abs(_direction.x);
        }
        else if (_viewportPosition.x > 1f - margin)
        {
            _viewportPosition.x = 1f - margin;
            _direction.x = -Mathf.Abs(_direction.x);
        }

        if (_viewportPosition.y < margin)
        {
            _viewportPosition.y = margin;
            _direction.y = Mathf.Abs(_direction.y);
        }
        else if (_viewportPosition.y > 1f - margin)
        {
            _viewportPosition.y = 1f - margin;
            _direction.y = -Mathf.Abs(_direction.y);
        }

        // è„â∫Ç…Ç”ÇÌÇ”ÇÌÇ≥ÇπÇÈ
        Vector3 position = _viewportPosition;
        position.y += Mathf.Sin(_elapsed * 2f + _phase) * 0.02f;

        transform.position =
            _camera.ViewportToWorldPoint(position);
    }
}