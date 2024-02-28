using System;
using System.Collections;
using System.Collections.Generic;
using ARMazGlass.Scripts.Utils;
using DigitalRubyShared;
using UnityEngine;
using ARMazGlass.Scripts.SceneSync;
using Newtonsoft.Json;


public class RKHandle : MonoBehaviour
{
    public const string _uid = "chzy";
    public Rokid _rokid;
    public RKAssetsStateSyncManager _SyncManager;
    public string assetId = "ControlNodeAA_Id";
    private string assetName = "";
    private void OnEnable()
    {
        _rokid.gestureCallBack += RokidSync;
        initUpd();
    }

    private void initUpd()
    {
        UdpManager.isConnectUdp = true;
        UdpManager._localUid = _uid;
        UdpManager.InitializeUdpClient();
        SyncMessage syncMessage = new SyncMessage(UdpManager._localUid,"ping");
        UdpManager.SendMessage(JsonConvert.SerializeObject(syncMessage));
        if (string.IsNullOrEmpty(assetName))
        {
            assetName = $"{gameObject.name}";
        }
        _SyncManager.InitSyncManager(assetId, assetName);
    }
    
    private void RokidSync(RKSyncAction action, RKSyncState state)
    {
        print("RK Handle RokidSync ");
        _SyncManager.SendSyncMessage(state, action, assetId);
        if (state == RKSyncState.StartControl)
        {
            _SyncManager.isControl = true;
        } 
        else if (state == RKSyncState.EndControl)
        {
            _SyncManager.isControl = false;
        }
    }
}
