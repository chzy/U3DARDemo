using System.Collections.Generic;

namespace ARMazGlass.Scripts.SceneSync
{
    public class SyncMessage
    {
        // 消息{"uid":"userId","type":"ping/sync",seq:1,"data":"自定义数据"}
        public string uid = "NativeInterface.NativeAPI.GetGlassSN()";
        
        // "ping/sync"
        public string type = "ping";
        public string data;

        public SyncMessage(string uid,string type)
        {
            this.uid = uid;
            this.type = type;
        }
    }

    public class RKSyncActionData
    {
        public long sendTime;       // 发送时间
        public RKSyncAction rkSyncAction; // 同步的动作类型
        public string assetId;      // 控制的资源Id，唯一标识符
        public string assetName;    // 资源名称
        // public string ownerSession; // 控制权限请求返回
        public RKSyncInfoData syncInfoData;    
    }

    public class RKSyncInfoData
    {              
        public int seq = 1;               // 操作次数，每次发送+1;
        public RKSyncState rkSyncState;   // 控制状态
        public List<float> position;      // 位姿
        public List<float> scale;         // Scale
        public List<float> quaternion;    // 旋转角
    }

    public enum RKSyncAction
    {
        Click,
        Drag,
        Media,
    }

    public enum RKSyncState
    {
        StartControl,
        KeepControl,
        EndControl
    }
}