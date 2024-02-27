using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.UI;
public class main : MonoBehaviour
{
    [SerializeField] ARSession m_Session;
    public Text coordinateText; // UI Text组件，用于显示坐标
    public ARCameraManager _ARCameraManager;
    // Start is called before the first frame update
    void Start()
    {
        if ((ARSession.state == ARSessionState.None) ||
            (ARSession.state == ARSessionState.CheckingAvailability))
        {
             ARSession.CheckAvailability();
        }
        if (ARSession.state == ARSessionState.Unsupported)
        {
            // Start some fallback experience for unsupported devices
            print("当前设备不支持AR，请更换设备");
        }
        else
        {
            m_Session.enabled = true;
            _ARCameraManager.frameReceived += OnCameraFrameReceived;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        Matrix4x4? projectionMatrix = eventArgs.projectionMatrix;
        
    }
}
