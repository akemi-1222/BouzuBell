using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayButtonController : MonoBehaviour,
    IPointerClickHandler
{
    [Header("再生ボタンの画像")]
    [SerializeField] private Sprite _playSprite;

    [Header("停止ボタンの画像")]
    [SerializeField] private Sprite _stopSprite;

    [Header("年明けまでのタイマー")]
    [SerializeField] private GameTimer _timer;

    [Header("プレイ中のカメラのSize")]
    [SerializeField, Min(0.1f)]
    private float _playCameraSize = 3.5f;

    [Header("プレイ中にカメラを動かす距離")]
    [SerializeField]
    private Vector2 _cameraMove = new Vector2(0f, -1.5f);

    [Header("二重振り子の物理演算")]
    [SerializeField] private DoublePendulumController _pendulum;

    [Header("プレイ中に非表示にする画像")]
    [SerializeField] private SpriteRenderer _rightLeftSprite;
    [SerializeField] private SpriteRenderer _connectorArm1Sprite;
    [SerializeField] private SpriteRenderer _connectorArm2Sprite;
    [SerializeField] private Image _hammerSelectImage;
    [SerializeField] private TMP_Text _textMeshPro;

    [Header("プレイ中に停止する操作")]
    [SerializeField] private CraneArmController _craneArm;
    [SerializeField] private ConnectorArmController _connector1;
    [SerializeField] private ConnectorArmController _connector2;
    [SerializeField] private HammerSelectButtom _hammerSelect;

    [Header("ハンマーの軌跡")]
    [SerializeField] private TrailRenderer _hammerTrail;

    [Header("プレイ中に表示にする画像")]
    [SerializeField] private TMP_Text _restBonnou;
    [SerializeField] private GameObject _bonnouBarObject;

    [Header("ダメージ表示の親")]
    [SerializeField] private GameObject _damageLayer;

    private Camera _camera;
    private SpriteRenderer _spriteRenderer;

    private bool _isPlaying;

    //開始前のカメラ設定
    private float _initialCameraSize;
    private Vector3 _initialCameraPosition;

    //開始前のボタン設定
    private Vector3 _initialButtonPosition;
    private Vector3 _initialButtonScale;

    //画面のどの位置にボタンがあるか
    private Vector3 _buttonViewportPosition;

    private void Start()
    {
        _camera = Camera.main;
        _spriteRenderer = GetComponent<SpriteRenderer>();

        if (_camera == null ||
            !_camera.orthographic ||
            _spriteRenderer == null ||
            _playSprite == null ||
            _stopSprite == null)
        {
            Debug.LogError(
                "OrthographicのMainCamera、SpriteRenderer、" +
                "再生・停止画像を確認してください。",
                this
            );

            enabled = false;
            return;
        }

        if (_bonnouBarObject != null)
            _bonnouBarObject.SetActive(false);

        //元に戻せるように開始時の設定を保存する
        _initialCameraSize = _camera.orthographicSize;
        _initialCameraPosition = _camera.transform.position;

        _initialButtonPosition = transform.position;
        _initialButtonScale = transform.localScale;

        _buttonViewportPosition = _camera.WorldToViewportPoint(transform.position);

        _spriteRenderer.sprite = _playSprite;

        _damageLayer.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isActiveAndEnabled)
            return;

        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        _isPlaying = !_isPlaying;

        if (_isPlaying)
            StartPlay();
        else
            StopPlay();
    }

    private void StartPlay()
    {
        // 物理演算を準備できなかった場合は開始しない
        if (_pendulum == null || !_pendulum.BeginSimulation())
        {
            Debug.LogError(
                "二重振り子の参照または設定を確認してください。",
                this
            );

            _isPlaying = false;
            return;
        }

        if (_hammerTrail != null)
        {
            _hammerTrail.Clear();
            _hammerTrail.emitting = true;
        }

        DamageText[] oldTexts = _damageLayer.GetComponentsInChildren<DamageText>(true);

        foreach (DamageText text in oldTexts)
        {
            text.gameObject.SetActive(false);
            Destroy(text.gameObject);
        }

        _damageLayer.SetActive(true);

        _restBonnou.enabled = true;

        if (_bonnouBarObject != null)
            _bonnouBarObject.SetActive(true);

        SetEditMode(false);

        _spriteRenderer.sprite = _stopSprite;

        //カメラを拡大する
        _camera.orthographicSize = _playCameraSize;

        //鐘とクレーンが見える位置へ移動する
        _camera.transform.position = _initialCameraPosition + new Vector3(_cameraMove.x, _cameraMove.y, 0f);

        //ボタンを画面上の元の位置に置き直す
        transform.position = _camera.ViewportToWorldPoint(_buttonViewportPosition);

        //ズームしてもボタンが大きくならないようにする
        float sizeRatio = _camera.orthographicSize / _initialCameraSize;

        transform.localScale = _initialButtonScale * sizeRatio;

        if (_timer != null)
            _timer.StartTimer();

        
    }

    private void StopPlay()
    {
        if (_hammerTrail != null)
        {
            _hammerTrail.emitting = false;
            _hammerTrail.Clear();
        }

        if (_bonnouBarObject != null)
            _bonnouBarObject.SetActive(false);

        _damageLayer.SetActive(false);

        _restBonnou.enabled = false;

        _pendulum.EndSimulation();

        _spriteRenderer.sprite = _playSprite;

        //カメラを開始前の状態へ戻す
        _camera.orthographicSize = _initialCameraSize;
        _camera.transform.position = _initialCameraPosition;

        //ボタンも元へ戻す
        transform.position = _initialButtonPosition;
        transform.localScale = _initialButtonScale;

        //非表示にした画像と、停止した操作を戻す
        SetEditMode(true);

        if (_timer != null)
            _timer.ResetTimer();
    }

    private void SetEditMode(bool canEdit)
    {
        //画像の表示を切り替える
        _rightLeftSprite.enabled = canEdit;
        _connectorArm1Sprite.enabled = canEdit;
        _connectorArm2Sprite.enabled = canEdit;
        _hammerSelectImage.enabled = canEdit;
        _textMeshPro.enabled = canEdit;

        //押しっぱなしの状態を解除する
        _craneArm.EndButtonControl();

        //操作スクリプトを切り替える
        _craneArm.enabled = canEdit;
        _connector1.enabled = canEdit;
        _connector2.enabled = canEdit;
        _hammerSelect.enabled = canEdit;
    }
}