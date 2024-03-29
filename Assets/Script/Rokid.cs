
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using ARMazGlass.Scripts.SceneSync;
    

public delegate void RokidSync(RKSyncAction sender, RKSyncState state);

namespace DigitalRubyShared
{
    
    public class Rokid : MonoBehaviour
    {
        public GameObject m_object;
        // public GameObject AsteroidPrefab;
        public Material LineMaterial;

        public Camera arCamera;
        public ARRaycastManager raycastManager;
        
        private Sprite[] asteroids;

        public event RokidSync gestureCallBack;
        
        private TapGestureRecognizer tapGesture;
        private TapGestureRecognizer doubleTapGesture;
        private TapGestureRecognizer tripleTapGesture;
        private SwipeGestureRecognizer swipeGesture;
        private PanGestureRecognizer panGesture;
        private ScaleGestureRecognizer scaleGesture;
        private RotateGestureRecognizer rotateGesture;
        private LongPressGestureRecognizer longPressGesture;

        private float nextAsteroid = float.MinValue;
        private GameObject draggingAsteroid;

        private readonly List<Vector3> swipeLines = new List<Vector3>();

        private void DebugText(string text, params object[] format)
        {
            //bottomLabel.text = string.Format(text, format);
            Debug.Log(string.Format(text, format));
        }

        private void BeginDrag(float screenX, float screenY)
        {
            Vector3 pos = new Vector3(screenX, screenY, 0.0f);
            pos = Camera.main.ScreenToWorldPoint(pos);
            RaycastHit2D hit = Physics2D.CircleCast(pos, 10.0f, Vector2.zero);
            if (hit.transform != null && hit.transform.gameObject.name == "Asteroid")
            {
                draggingAsteroid = hit.transform.gameObject;
                draggingAsteroid.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
                draggingAsteroid.GetComponent<Rigidbody2D>().angularVelocity = 0.0f;
            }
            else
            {
                longPressGesture.Reset();
            }
        }

        private void DragTo(float screenX, float screenY)
        {
            if (draggingAsteroid == null)
            {
                return;
            }

            Vector3 pos = new Vector3(screenX, screenY, 0.0f);
            pos = Camera.main.ScreenToWorldPoint(pos);
            draggingAsteroid.GetComponent<Rigidbody2D>().MovePosition(pos);
        }

        private void EndDrag(float velocityXScreen, float velocityYScreen)
        {
            if (draggingAsteroid == null)
            {
                return;
            }

            Vector3 origin = Camera.main.ScreenToWorldPoint(Vector3.zero);
            Vector3 end = Camera.main.ScreenToWorldPoint(new Vector3(velocityXScreen, velocityYScreen, 0.0f));
            Vector3 velocity = (end - origin);
            draggingAsteroid.GetComponent<Rigidbody2D>().velocity = velocity;
            draggingAsteroid.GetComponent<Rigidbody2D>().angularVelocity = UnityEngine.Random.Range(5.0f, 45.0f);
            draggingAsteroid = null;
            DebugText("Long tap flick velocity: {0}", velocity);
        }

        private void HandleSwipe(float endX, float endY)
        {
            Vector2 start = new Vector2(swipeGesture.StartFocusX, swipeGesture.StartFocusY);
            Vector3 startWorld = Camera.main.ScreenToWorldPoint(start);
            Vector3 endWorld = Camera.main.ScreenToWorldPoint(new Vector2(endX, endY));
            float distance = Vector3.Distance(startWorld, endWorld);
            startWorld.z = endWorld.z = 0.0f;

            swipeLines.Add(startWorld);
            swipeLines.Add(endWorld);

            if (swipeLines.Count > 4)
            {
                swipeLines.RemoveRange(0, swipeLines.Count - 4);
            }

            RaycastHit2D[] collisions = Physics2D.CircleCastAll(startWorld, 10.0f, (endWorld - startWorld).normalized, distance);

            if (collisions.Length != 0)
            {
                Debug.Log("Raycast hits: " + collisions.Length + ", start: " + startWorld + ", end: " + endWorld + ", distance: " + distance);

                Vector3 origin = Camera.main.ScreenToWorldPoint(Vector3.zero);
                Vector3 end = Camera.main.ScreenToWorldPoint(new Vector3(swipeGesture.VelocityX, swipeGesture.VelocityY, Camera.main.nearClipPlane));
                Vector3 velocity = (end - origin);
                Vector2 force = velocity * 500.0f;

                foreach (RaycastHit2D h in collisions)
                {
                    h.rigidbody.AddForceAtPosition(force, h.point);
                }
            }
        }

        private void TapGestureCallback(GestureRecognizer gesture)
        {
            if (gesture.State == GestureRecognizerState.Ended)
            {
                DebugText("Tapped at {0}, {1}", gesture.FocusX, gesture.FocusY);
                // CreateAsteroid(gesture.FocusX, gesture.FocusY);
            }
        }

        private void CreateTapGesture()
        {
            tapGesture = new TapGestureRecognizer();
            tapGesture.StateUpdated += TapGestureCallback;
            tapGesture.RequireGestureRecognizerToFail = doubleTapGesture;
            FingersScript.Instance.AddGesture(tapGesture);
        }

        private void DoubleTapGestureCallback(GestureRecognizer gesture)
        {
            if (gesture.State == GestureRecognizerState.Ended)
            {
                DebugText("Double tapped at {0}, {1}", gesture.FocusX, gesture.FocusY);
            }
        }

        private void CreateDoubleTapGesture()
        {
            doubleTapGesture = new TapGestureRecognizer();
            doubleTapGesture.NumberOfTapsRequired = 2;
            doubleTapGesture.StateUpdated += DoubleTapGestureCallback;
            doubleTapGesture.RequireGestureRecognizerToFail = tripleTapGesture;
            FingersScript.Instance.AddGesture(doubleTapGesture);
        }

        private void SwipeGestureCallback(GestureRecognizer gesture)
        {
            if (gesture.State == GestureRecognizerState.Ended)
            {
                HandleSwipe(gesture.FocusX, gesture.FocusY);
                DebugText("Swiped from {0},{1} to {2},{3}; velocity: {4}, {5}", gesture.StartFocusX, gesture.StartFocusY, gesture.FocusX, gesture.FocusY, swipeGesture.VelocityX, swipeGesture.VelocityY);
            }
        }

        private void CreateSwipeGesture()
        {
            swipeGesture = new SwipeGestureRecognizer();
            swipeGesture.Direction = SwipeGestureRecognizerDirection.Any;
            swipeGesture.StateUpdated += SwipeGestureCallback;
            swipeGesture.DirectionThreshold = 1.0f; // allow a swipe, regardless of slope
            FingersScript.Instance.AddGesture(swipeGesture);
        }

        private void PanGestureCallback(GestureRecognizer gesture)
        {
            if (gesture.State == GestureRecognizerState.Began) {
                gestureCallBack(RKSyncAction.Drag, RKSyncState.StartControl);
            } 
            else if (gesture.State == GestureRecognizerState.Executing)
            {
                gestureCallBack(RKSyncAction.Drag, RKSyncState.KeepControl);
            }
            else if (gesture.State == GestureRecognizerState.Ended)
            {
                gestureCallBack(RKSyncAction.Drag, RKSyncState.EndControl);
            }


            if (gesture.State == GestureRecognizerState.Executing)
            {
                DebugText("Panned, Location: {0}, {1}, Delta: {2}, {3}", gesture.FocusX, gesture.FocusY, gesture.DeltaX, gesture.DeltaY);

                float deltaX = panGesture.DeltaX / 25.0f;
                float deltaY = panGesture.DeltaY / 25.0f;
                Vector3 pos = m_object.transform.position;
                pos.x += deltaX;
                pos.y += deltaY;
              
                Vector2 screenPosition = new Vector2(panGesture.FocusX, panGesture.FocusY);
              
                // 创建一个 List 用于存储射线投射结果
                List<ARRaycastHit> hits = new List<ARRaycastHit>();

                // 进行射线投射
                if (raycastManager.Raycast(screenPosition, hits, TrackableType.AllTypes))
                {
                    // 获取第一个相交点的世界坐标
                    Vector3 arWorldPosition = hits[0].pose.position;
                    m_object.transform.position = arWorldPosition;
                    // 打印AR世界中的3D坐标
                    Debug.Log("AR World Position: " + arWorldPosition);
                }
            }
        }

        private void CreatePanGesture()
        {
            panGesture = new PanGestureRecognizer();
            panGesture.MinimumNumberOfTouchesToTrack = 1;
            panGesture.StateUpdated += PanGestureCallback;
            FingersScript.Instance.AddGesture(panGesture);
        }

        private void ScaleGestureCallback(GestureRecognizer gesture)
        {
            if (gesture.State == GestureRecognizerState.Began) {
                gestureCallBack(RKSyncAction.Drag, RKSyncState.StartControl);
            } 
            else if (gesture.State == GestureRecognizerState.Executing)
            {
                gestureCallBack(RKSyncAction.Drag, RKSyncState.KeepControl);
            }
            else if (gesture.State == GestureRecognizerState.Ended)
            {
                gestureCallBack(RKSyncAction.Drag, RKSyncState.EndControl);
            }

            if (gesture.State == GestureRecognizerState.Executing)
            {
                DebugText("Scaled: {0}, Focus: {1}, {2}", scaleGesture.ScaleMultiplier, scaleGesture.FocusX, scaleGesture.FocusY);
                m_object.transform.localScale *= scaleGesture.ScaleMultiplier;
            }
        }

        private void CreateScaleGesture()
        {
            scaleGesture = new ScaleGestureRecognizer();
            scaleGesture.StateUpdated += ScaleGestureCallback;
            FingersScript.Instance.AddGesture(scaleGesture);
        }

        private void RotateGestureCallback(GestureRecognizer gesture)
        {
            if (gesture.State == GestureRecognizerState.Began) {
                gestureCallBack(RKSyncAction.Drag, RKSyncState.StartControl);
            } 
            else if (gesture.State == GestureRecognizerState.Executing)
            {
                gestureCallBack(RKSyncAction.Drag, RKSyncState.KeepControl);
            }
            else if (gesture.State == GestureRecognizerState.Ended)
            {
                gestureCallBack(RKSyncAction.Drag, RKSyncState.EndControl);
            }

            if (gesture.State == GestureRecognizerState.Executing)
            {
                m_object.transform.Rotate(0.0f, rotateGesture.RotationRadiansDelta * Mathf.Rad2Deg, 0.0f);
            }
        }

        private void CreateRotateGesture()
        {
            rotateGesture = new RotateGestureRecognizer();
            rotateGesture.StateUpdated += RotateGestureCallback;
            FingersScript.Instance.AddGesture(rotateGesture);
        }

        private void LongPressGestureCallback(GestureRecognizer gesture)
        {
            if (gesture.State == GestureRecognizerState.Began)
            {
                DebugText("Long press began: {0}, {1}", gesture.FocusX, gesture.FocusY);
                BeginDrag(gesture.FocusX, gesture.FocusY);
            }
            else if (gesture.State == GestureRecognizerState.Executing)
            {
                DebugText("Long press moved: {0}, {1}", gesture.FocusX, gesture.FocusY);
                DragTo(gesture.FocusX, gesture.FocusY);
            }
            else if (gesture.State == GestureRecognizerState.Ended)
            {
                DebugText("Long press end: {0}, {1}, delta: {2}, {3}", gesture.FocusX, gesture.FocusY, gesture.DeltaX, gesture.DeltaY);
                EndDrag(longPressGesture.VelocityX, longPressGesture.VelocityY);
            }
        }

        private void CreateLongPressGesture()
        {
            longPressGesture = new LongPressGestureRecognizer();
            longPressGesture.MaximumNumberOfTouchesToTrack = 1;
            longPressGesture.StateUpdated += LongPressGestureCallback;
            FingersScript.Instance.AddGesture(longPressGesture);
        }

        private void PlatformSpecificViewTapUpdated(GestureRecognizer gesture)
        {
            if (gesture.State == GestureRecognizerState.Ended)
            {
                Debug.Log("You triple tapped the platform specific label!");
            }
        }

        private void CreatePlatformSpecificViewTripleTapGesture()
        {
            tripleTapGesture = new TapGestureRecognizer();
            tripleTapGesture.StateUpdated += PlatformSpecificViewTapUpdated;
            tripleTapGesture.NumberOfTapsRequired = 3;
            // tripleTapGesture.PlatformSpecificView = bottomLabel.gameObject;
            FingersScript.Instance.AddGesture(tripleTapGesture);
        }

        private static bool? CaptureGestureHandler(GameObject obj)
        {
            // I've named objects PassThrough* if the gesture should pass through and NoPass* if the gesture should be gobbled up, everything else gets default behavior
            if (obj.name.StartsWith("PassThrough"))
            {
                // allow the pass through for any element named "PassThrough*"
                return false;
            }
            else if (obj.name.StartsWith("NoPass"))
            {
                // prevent the gesture from passing through, this is done on some of the buttons and the bottom text so that only
                // the triple tap gesture can tap on it
                return true;
            }

            // fall-back to default behavior for anything else
            return null;
        }

        private void Start()
        {
            asteroids = Resources.LoadAll<Sprite>("FingersAsteroids");

            // don't reorder the creation of these :)
            CreatePlatformSpecificViewTripleTapGesture();
            CreateDoubleTapGesture();
            CreateTapGesture();
            CreateSwipeGesture();
            CreatePanGesture();
            CreateScaleGesture();
            CreateRotateGesture();
            CreateLongPressGesture();

            // pan, scale and rotate can all happen simultaneously
            panGesture.AllowSimultaneousExecution(scaleGesture);
            panGesture.AllowSimultaneousExecution(rotateGesture);
            scaleGesture.AllowSimultaneousExecution(rotateGesture);

            // prevent the one special no-pass button from passing through,
            //  even though the parent scroll view allows pass through (see FingerScript.PassThroughObjects)
            FingersScript.Instance.CaptureGestureHandler = CaptureGestureHandler;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ReloadDemoScene();
            }
        }

        private void LateUpdate()
        {
            if (Time.timeSinceLevelLoad > nextAsteroid)
            {
                nextAsteroid = Time.timeSinceLevelLoad + UnityEngine.Random.Range(1.0f, 4.0f);
                //CreateAsteroid(float.MinValue, float.MinValue);
            }

            int touchCount = Input.touchCount;
            if (FingersScript.Instance.TreatMousePointerAsFinger && Input.mousePresent)
            {
                touchCount += (Input.GetMouseButton(0) ? 1 : 0);
                touchCount += (Input.GetMouseButton(1) ? 1 : 0);
                touchCount += (Input.GetMouseButton(2) ? 1 : 0);
            }
            string touchIds = string.Empty;
            int gestureTouchCount = 0;
            foreach (GestureRecognizer g in FingersScript.Instance.Gestures)
            {
                gestureTouchCount += g.CurrentTrackedTouches.Count;
            }
            foreach (GestureTouch t in FingersScript.Instance.CurrentTouches)
            {
                touchIds += ":" + t.Id + ":";
            }

            string text = "Dpi: " + DeviceInfo.PixelsPerInch + System.Environment.NewLine +
                "Width: " + Screen.width + System.Environment.NewLine +
                "Height: " + Screen.height + System.Environment.NewLine +
                "Touches: " + FingersScript.Instance.CurrentTouches.Count + " (" + gestureTouchCount + "), ids" + touchIds + System.Environment.NewLine;
            print(text);
        }

        private void OnRenderObject()
        {
            if (LineMaterial == null)
            {
                return;
            }

            GL.PushMatrix();
            LineMaterial.SetPass(0);
            GL.LoadProjectionMatrix(Camera.main.projectionMatrix);
            GL.Begin(GL.LINES);
            for (int i = 0; i < swipeLines.Count; i++)
            {
                GL.Color(Color.yellow);
                GL.Vertex(swipeLines[i]);
                GL.Vertex(swipeLines[++i]);
            }
            GL.End();
            GL.PopMatrix();
        }

        public void ReloadDemoScene()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(0, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }
}
