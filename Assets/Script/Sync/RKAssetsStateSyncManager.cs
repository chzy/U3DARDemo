using System;
using System.Collections.Generic;
using ARMazGlass.Scripts.SceneSync;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Serialization;

namespace ARMazGlass.Scripts.Utils
{
    public class RKAssetsStateSyncManager : MonoBehaviour
    {
        private static string TAG = "RKAssetsStateSyncManager:::";

        private int sendSeq = 0;
        private SyncMessage _syncMessage;
        private RKSyncActionData _rkSyncActionData;
        private RKSyncInfoData _rkSyncInfoData;

        // private RKSyncInfoData _rkSyncInfoData;
        private string AssetsID;
        private string name;


        private Vector3 targetPos = Vector3.zero;
        private RKSyncInfoData _lastRkSyncInfoData = null;
        
        private int lastReceiveSeq = 0;
        private RKSyncActionData rkSyncActionData;
        
        //是否正在受控-->>>受控状态下，不接收消息通信
        public bool isControl = false;
        
        

        public void InitSyncManager(string assetsId, string name)
        {
            var _uid = RKHandle._uid;
            this.AssetsID = assetsId;
            this.name = name;
            
            UdpManager.AddSyncAssetListener(AssetsID);
            if (null == _syncMessage)
            {
                _syncMessage = new SyncMessage(_uid, "sync");       
            }
            
            if (null == _rkSyncActionData)
            {
                _rkSyncActionData = new RKSyncActionData();
            }
            
            if (_rkSyncInfoData == null)
            {
                _rkSyncInfoData = new RKSyncInfoData();
            }
        }

        private void Update()
        {
            // 接收消息，
            if (!isControl)
            {
                ReceiveSyncMessage();
            }
            
            // 更新位姿
            if (null != rkSyncActionData)
            {
                transform.localPosition = targetPos;
            }

            if (rkSyncActionData?.syncInfoData?.rkSyncState == RKSyncState.EndControl)
            {
                rkSyncActionData = null;
            }
        }

        /// <summary>
        /// 发送同步信息
        /// </summary>
        public void SendSyncMessage(RKSyncState rkSyncState, RKSyncAction rkSyncAction, string assetsId)
        {
            print("SendSyncMessage:| "  + assetsId + "|" + AssetsID);
            if (!assetsId.Equals(AssetsID))
            {
                return;
            }
            sendSeq++;
            // 组装同步的Info数据
            _rkSyncInfoData.seq = sendSeq;
            _rkSyncInfoData.rkSyncState = rkSyncState;
            _rkSyncInfoData.position = new List<float>() { transform.localPosition.x, transform.localPosition.y, transform.localPosition.z };
            _rkSyncInfoData.scale = new List<float>() { transform.localScale.x, transform.localScale.y, transform.localScale.z };
            _rkSyncInfoData.quaternion = new List<float>() { transform.localRotation.x, transform.localRotation.y, transform.localRotation.z, transform.localRotation.w };
            var syncInfo = JsonConvert.SerializeObject(_rkSyncInfoData);
            RDebug.I(TAG, $"SendSyncMessage()---->>_rkSyncInfoData::::{syncInfo}");

            // 组装同步的Action数据
            _rkSyncActionData.sendTime = 1;
            _rkSyncActionData.rkSyncAction = rkSyncAction;
            _rkSyncActionData.assetId = assetsId;
            _rkSyncActionData.assetName = name;
            _rkSyncActionData.syncInfoData = _rkSyncInfoData;
            var syncAction = JsonConvert.SerializeObject(_rkSyncActionData);
            // transform.worldToLocalMatrix();
            RDebug.I(TAG, $"SendSyncMessage()---->>_rkSyncActionData::::{syncAction}");

            // _syncMessage.seq = sendSeq;
            _syncMessage.data = syncAction;
            // 发送消息
            UdpManager.SendMessage(JsonConvert.SerializeObject(_syncMessage));

            if (rkSyncState == RKSyncState.EndControl)
            {
                sendSeq = 0;
            }
        }

        /// <summary>
        /// 接收同步信息
        /// </summary>
        private void ReceiveSyncMessage()
        {
            // 查询该资源的同步数据--->>并将比该lastReceiveSeq小的所有数据都清除
            rkSyncActionData = UdpManager.GetRKSyncActionData(AssetsID, lastReceiveSeq);
            if (null == rkSyncActionData || null == rkSyncActionData?.syncInfoData)
            {
                return;
            }
            
            lastReceiveSeq = rkSyncActionData.syncInfoData.seq;
            
            RDebug.I(TAG, $"ReceiveSyncInfo()---->> seq:{lastReceiveSeq}  |  {JsonConvert.SerializeObject(rkSyncActionData)}");
            targetPos = new Vector3(rkSyncActionData.syncInfoData.position[0], rkSyncActionData.syncInfoData.position[1], rkSyncActionData.syncInfoData.position[2]);
            transform.localRotation = new Quaternion(rkSyncActionData.syncInfoData.quaternion[0], rkSyncActionData.syncInfoData.quaternion[1],
                rkSyncActionData.syncInfoData.quaternion[2], rkSyncActionData.syncInfoData.quaternion[3]);


            if (rkSyncActionData.syncInfoData.rkSyncState == RKSyncState.EndControl)
            {
                RDebug.I(TAG, $"ReceiveSyncInfo()---->>seq:{lastReceiveSeq}  |  Over last Data:::: {JsonConvert.SerializeObject(rkSyncActionData.syncInfoData)}");
                UdpManager.ClearCacheSyncActionData(AssetsID);
                lastReceiveSeq = 0;
            }
        }

        private void OnDisable()
        {
        }

        private void OnDestroy()
        {
        }
    }
}