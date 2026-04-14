using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableElement : MonoBehaviour,
    IDragHandler, IPointerDownHandler
{
    private RectTransform _rectTransform;
    private Canvas _canvas;
    private Vector2 _offset;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out _offset
        );
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)_canvas.transform,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );
        _rectTransform.localPosition = localPoint - _offset;
    }

    void Update()
    {
        if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            float prevDist = (
                (t0.position - t0.deltaPosition) -
                (t1.position - t1.deltaPosition)
            ).magnitude;

            float currDist = (t0.position - t1.position).magnitude;
            float delta    = currDist - prevDist;

            float scaleFactor = 1 + delta * 0.001f;
            _rectTransform.localScale *= scaleFactor;
        }
    }
}