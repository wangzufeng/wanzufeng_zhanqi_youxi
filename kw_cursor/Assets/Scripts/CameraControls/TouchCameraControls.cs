using UnityEngine;
using UnityEngine.EventSystems;

namespace KwCursor
{
    /// <summary>
    /// 移动端相机控制：单指拖动平移、双指捏合缩放、点按选取；
    /// 编辑器内用鼠标左键拖动平移、滚轮缩放、左键点按选取。
    /// </summary>
    public class TouchCameraControls : MonoBehaviour
    {
        public System.Action<Vector3> OnWorldTap;

        public Vector2 MinBounds = new Vector2(-100f, -100f);
        public Vector2 MaxBounds = new Vector2(100f, 100f);
        public float MinZoom = 2.5f;
        public float MaxZoom = 9f;

        Camera cam;
        bool gestureStartedOnUI;
        bool panning;
        bool pinched;
        Vector2 downScreenPos;
        Vector2 lastScreenPos;

        float DragThresholdPx
        {
            get
            {
                float dpi = Screen.dpi;
                return Mathf.Max(18f, dpi > 0f ? dpi * 0.12f : 18f);
            }
        }

        void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
        }

        void Update()
        {
            if (cam == null) return;

            if (Input.touchCount >= 2)
            {
                pinched = true;
                HandlePinch();
                return;
            }

            if (Input.touchCount == 1)
            {
                HandleTouch(Input.GetTouch(0));
                return;
            }

            HandleMouse();
        }

        void HandlePinch()
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);
            Vector2 p0 = t0.position - t0.deltaPosition;
            Vector2 p1 = t1.position - t1.deltaPosition;
            float prev = (p0 - p1).magnitude;
            float cur = (t0.position - t1.position).magnitude;
            if (cur > 1f)
            {
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize * prev / cur, MinZoom, MaxZoom);
            }

            // 双指中点平移
            Vector2 mid = (t0.position + t1.position) * 0.5f;
            Vector2 midPrev = (p0 + p1) * 0.5f;
            PanByScreenDelta(midPrev, mid);
            ClampPosition();
        }

        void HandleTouch(Touch t)
        {
            switch (t.phase)
            {
                case TouchPhase.Began:
                    gestureStartedOnUI = IsOverUI(t.fingerId);
                    panning = false;
                    pinched = false;
                    downScreenPos = t.position;
                    lastScreenPos = t.position;
                    break;

                case TouchPhase.Moved:
                    if (gestureStartedOnUI) break;
                    if (!panning && (t.position - downScreenPos).magnitude > DragThresholdPx)
                        panning = true;
                    if (panning)
                    {
                        PanByScreenDelta(lastScreenPos, t.position);
                        ClampPosition();
                    }
                    lastScreenPos = t.position;
                    break;

                case TouchPhase.Ended:
                    if (!gestureStartedOnUI && !panning && !pinched && OnWorldTap != null)
                        OnWorldTap(ScreenToWorld(t.position));
                    panning = false;
                    pinched = false;
                    break;

                case TouchPhase.Canceled:
                    panning = false;
                    pinched = false;
                    break;
            }
        }

        void HandleMouse()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.0001f)
            {
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize * (1f - scroll * 0.8f), MinZoom, MaxZoom);
                ClampPosition();
            }

            if (Input.GetMouseButtonDown(0))
            {
                gestureStartedOnUI = IsOverUI(-1);
                panning = false;
                downScreenPos = Input.mousePosition;
                lastScreenPos = Input.mousePosition;
            }
            else if (Input.GetMouseButton(0))
            {
                if (gestureStartedOnUI) return;
                Vector2 cur = Input.mousePosition;
                if (!panning && (cur - downScreenPos).magnitude > DragThresholdPx)
                    panning = true;
                if (panning)
                {
                    PanByScreenDelta(lastScreenPos, cur);
                    ClampPosition();
                }
                lastScreenPos = cur;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                if (!gestureStartedOnUI && !panning && OnWorldTap != null)
                    OnWorldTap(ScreenToWorld(Input.mousePosition));
                panning = false;
            }
        }

        void PanByScreenDelta(Vector2 fromScreen, Vector2 toScreen)
        {
            Vector3 a = ScreenToWorld(fromScreen);
            Vector3 b = ScreenToWorld(toScreen);
            Vector3 delta = a - b;
            delta.z = 0f;
            transform.position += delta;
        }

        Vector3 ScreenToWorld(Vector2 screen)
        {
            Vector3 p = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -transform.position.z));
            p.z = 0f;
            return p;
        }

        void ClampPosition()
        {
            Vector3 p = transform.position;
            p.x = Mathf.Clamp(p.x, MinBounds.x, MaxBounds.x);
            p.y = Mathf.Clamp(p.y, MinBounds.y, MaxBounds.y);
            transform.position = p;
        }

        static bool IsOverUI(int fingerId)
        {
            if (EventSystem.current == null) return false;
            if (fingerId >= 0) return EventSystem.current.IsPointerOverGameObject(fingerId);
            return EventSystem.current.IsPointerOverGameObject();
        }

        public System.Collections.IEnumerator PanTo(Vector3 target, float duration = 0.25f)
        {
            Vector3 start = transform.position;
            Vector3 end = new Vector3(target.x, target.y, start.z);
            end.x = Mathf.Clamp(end.x, MinBounds.x, MaxBounds.x);
            end.y = Mathf.Clamp(end.y, MinBounds.y, MaxBounds.y);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
                yield return null;
            }
        }
    }
}
