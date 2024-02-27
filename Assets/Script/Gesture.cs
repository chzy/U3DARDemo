using UnityEngine;

public class GestureController : MonoBehaviour
{
    private Vector3 touchStart;
    private Vector2[] lastTouchPositions = new Vector2[2];
    private Vector3 lastObjectPosition;
    private bool isRotating = false;
    private bool isScaling = false;

    void Update()
    {
        HandleTouches();
    }

    void HandleTouches()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                touchStart = touch.position;
            }
            else if (touch.phase == TouchPhase.Moved)
            {
                // 计算移动距离并应用到物体上
                // Vector3 touchDelta = Camera.main.ScreenToWorldPoint(touch.position) -
                                     Camera.main.ScreenToWorldPoint(touchStart);
                transform.position = Camera.main.ScreenToWorldPoint(touch.position) ;
                // touchStart = touch.position;
            }
        }
        else if (Input.touchCount == 2)
        {
            Touch firstTouch = Input.GetTouch(0);
            Touch secondTouch = Input.GetTouch(1);

            if (firstTouch.phase == TouchPhase.Began || secondTouch.phase == TouchPhase.Began)
            {
                lastTouchPositions[0] = firstTouch.position - firstTouch.deltaPosition;
                lastTouchPositions[1] = secondTouch.position - secondTouch.deltaPosition;
            }
            else if (firstTouch.phase == TouchPhase.Moved || secondTouch.phase == TouchPhase.Moved)
            {
                if (!isRotating)
                {
                    RotateObject(firstTouch.position, secondTouch.position);
                }

                if (!isScaling)
                {
                    ScaleObject(firstTouch.position, secondTouch.position);
                }
            }
        }
    }

    void MoveObject(Vector2 touchPosition)
    {
        Vector3 screenPos = Camera.main.ScreenToWorldPoint(new Vector3(touchPosition.x, touchPosition.y, 10));
        transform.position = new Vector3(screenPos.x, screenPos.y, lastObjectPosition.z);
    }

    void RotateObject(Vector2 firstTouchPosition, Vector2 secondTouchPosition)
    {
        if (!isRotating)
        {
            isRotating = true;
            lastTouchPositions[0] = firstTouchPosition;
            lastTouchPositions[1] = secondTouchPosition;
            return;
        }

        Vector2 currentTouchPosition1 = firstTouchPosition;
        Vector2 currentTouchPosition2 = secondTouchPosition;

        Vector2 previousTouchVector = (lastTouchPositions[0] - lastTouchPositions[1]).normalized;
        Vector2 currentTouchVector = (currentTouchPosition1 - currentTouchPosition2).normalized;

        float angle = Vector2.SignedAngle(previousTouchVector, currentTouchVector);

        transform.Rotate(Vector3.up, angle * 0.1f);

        lastTouchPositions[0] = currentTouchPosition1;
        lastTouchPositions[1] = currentTouchPosition2;
    }

    void ScaleObject(Vector2 firstTouchPosition, Vector2 secondTouchPosition)
    {
        if (!isScaling)
        {
            isScaling = true;
            return;
        }

        float previousTouchDeltaMagnitude = (lastTouchPositions[0] - lastTouchPositions[1]).magnitude;
        float touchDeltaMagnitude = (firstTouchPosition - secondTouchPosition).magnitude;

        float deltaMagnitudeDiff = touchDeltaMagnitude - previousTouchDeltaMagnitude;

        Vector3 newScale = transform.localScale + Vector3.one * deltaMagnitudeDiff * 0.01f;
        transform.localScale = newScale;
    }
}
