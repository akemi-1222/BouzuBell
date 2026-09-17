using UnityEngine;

public class CraneArmController : MonoBehaviour
{
    //回転設定
    [SerializeField] private float _rotationSpeed = 50.0f;
    [SerializeField] private float _startRotation = -10.0f;
    [SerializeField] private float _minRotation = -25.0f;
    [SerializeField] private float _maxRotation = 15.0f;

    //回転方向
    private int _rotationDir = 0;
    //ボタンをクリックしているか
    private bool _isPointerDown = false;

    //クリック中か取得
    public bool IsPointerDown
    {
        get { return _isPointerDown; }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //ゲーム開始時の角度を設定
        transform.localRotation = Quaternion.Euler(0.0f, 0.0f, _startRotation);
    }

    // Update is called once per frame
    void Update()
    {
        //ボタンが押さなければ処理しない
        if (_rotationDir == 0)
            return;

        //現在のZ回転を取得
        float rotationZ = transform.localEulerAngles.z;

        //360度表記をマイナス角度に変換
        if (rotationZ > 180.0f)
            rotationZ -= 360.0f;

        //回転させる
        rotationZ += _rotationDir * _rotationSpeed * Time.deltaTime;

        //回転範囲を制限
        rotationZ = Mathf.Clamp(rotationZ, _minRotation, _maxRotation);

        //回転を反映
        transform.localRotation = Quaternion.Euler(0.0f, 0.0f, rotationZ);
    }

    //ボタンを押したとき
    public void StartButtonControl(int direction)
    {
        _isPointerDown = true;
        _rotationDir = direction;
    }

    //隣のボタンに移動したとき
    public void ChangeRotationDirection(int direction)
    {
        if (_isPointerDown)
        {
            _rotationDir = direction;
        }
    }

    //回転だけを止める
    public void StopRotation()
    {
        _rotationDir = 0;
    }

    //ボタンを離したとき
    public void EndButtonControl()
    {
        _isPointerDown = false;
        _rotationDir = 0;
    }
}
