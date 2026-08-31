using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KwGeminid
{
    public class TouchCameraControls : MonoBehaviour
    {
        public float DragSpeed = 1f;
        public float MinZoom = 2.5f;
        public float MaxZoom = 12f;
        public Vector2 MinBounds = new Vector2(-10, -10);
        public Vector2 MaxBounds = new Vector2(30, 20);
        public Action<Vector3> OnWorldTap;

        Camera cam;
        Vector3 lastMousePos;
        bool isDragging;
        float touchStartTime;
        Vector2 touchStartPos;

        void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
        }

        void Update()
        {
            if (Input.touchSupported && Input.touchCount > 0)
            {
                HandleTouches();
            }
            else
            {
                HandleMouse();
            }
            ClampCamera();
        }

        void HandleTouches()
        {
            if (Input.touchCount == 1)
            {
                Touch t = Input.GetTouch(0);
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId)) return;

                if (t.phase == TouchPhase.Began)
                {
                    touchStartTime = Time.time;
                    touchStartPos = t.position;
                    isDragging = false;
                }
                else if (t.phase == TouchPhase.Moved)
                {
                    if (Vector2.Distance(t.position, touchStartPos) > 10f) isDragging = true;
                    if (isDragging)
                    {
                        Vector3 delta = cam.ScreenToWorldPoint(t.position - t.deltaPosition) - cam.ScreenToWorldPoint(t.position);
                        transform.position += delta;
                    }
                }
                else if (t.phase == TouchPhase.Ended)
                {
                    if (!isDragging && Time.time - touchStartTime < 0.35f)
                    {
                        Vector3 worldPos = cam.ScreenToWorldPoint(t.position);
                        if (OnWorldTap != null) OnWorldTap(worldPos);
                    }
                }
            }
            else if (Input.touchCount == 2)
            {
                Touch t0 = Input.GetTouch(0);
                Touch t1 = Input.GetTouch(1);
                Vector2 prevPos0 = t0.position - t0.deltaPosition;
                Vector2 prevPos1 = t1.position - t1.deltaPosition;
                float prevDist = (prevPos0 - prevPos1).magnitude;
                float curDist = (t0.position - t1.position).magnitude;
                float diff = curDist - prevDist;

                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - diff * 0.01f, MinZoom, MaxZoom);
            }
        }

        void HandleMouse()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            if (Input.GetMouseButtonDown(0))
            {
                lastMousePos = Input.mousePosition;
                touchStartTime = Time.time;
                touchStartPos = Input.mousePosition;
                isDragging = false;
            }
            else if (Input.GetMouseButton(0))
            {
                if (Vector2.Distance(Input.mousePosition, touchStartPos) > 6f) isDragging = true;
                if (isDragging)
                {
                    Vector3 delta = cam.ScreenToWorldPoint(lastMousePos) - cam.ScreenToWorldPoint(Input.mousePosition);
                    transform.position += delta;
                    lastMousePos = Input.mousePosition;
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                if (!isDragging && Time.time - touchStartTime < 0.35f)
                {
                    Vector3 worldPos = cam.ScreenToWorldPoint(Input.mousePosition);
                    if (OnWorldTap != null) OnWorldTap(worldPos);
                }
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - scroll * 4f, MinZoom, MaxZoom);
            }
        }

        void ClampCamera()
        {
            Vector3 p = transform.position;
            p.x = Mathf.Clamp(p.x, MinBounds.x, MaxBounds.x);
            p.y = Mathf.Clamp(p.y, MinBounds.y, MaxBounds.y);
            transform.position = p;
        }
    }
}
