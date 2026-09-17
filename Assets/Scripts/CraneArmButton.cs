using UnityEngine;
using UnityEngine.EventSystems;

public class CraneArmButton : MonoBehaviour,
    IPointerDownHandler,
    IPointerEnterHandler,
    IPointerUpHandler
{
    //クレーンの腕
    [SerializeField] private CraneArmController _craneArm;

    //回転方向
    [SerializeField] private int _rotationDirection = 1;

    //ボタンを押したとき
    public void OnPointerDown(PointerEventData eventData)
    {
        _craneArm.StartButtonControl(_rotationDirection);
    }

    //クリックしたまま隣のボタンに入ったとき
    public void OnPointerEnter(PointerEventData eventData)
    {
        _craneArm.ChangeRotationDirection(_rotationDirection);
    }

    //クリックを離したとき
    public void OnPointerUp(PointerEventData eventData)
    {
        _craneArm.EndButtonControl();
    }
}