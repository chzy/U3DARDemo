using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ARMazGlass.Scripts.Utils;
using Newtonsoft.Json;
using UnityEngine;
using Object = System.Object;

namespace ARMazGlass.Scripts.SceneSync
{
    public class UdpManager
    {
        private static string TAG = "UdpManager::::";
        //"192.168.31.124" 
        //"10.81.3.40"
        private const string updHost = "192.168.31.124";
        // 客户端
        private static UdpClient udpClient;
        // 远程服务器
        private static IPEndPoint remoteEndPoint;
        
        private static readonly Object _lock = new Object();
        
        // 开始接收同步他人的数据
        public static bool isConnectUdp = false;

        // 接收到的数据
        // public static RKSyncActionData receiveActionData;

        private static Dictionary<string, List<RKSyncActionData>> syncDiction = new Dictionary<string, List<RKSyncActionData> >();

        // 本地的UUid，用于过滤处理自己发送的消息
        public static string _localUid = "";
        

        public static void AddSyncAssetListener(string assetsId)
        {
            if (!syncDiction.ContainsKey(assetsId))
            {
                List<RKSyncActionData> rkSyncActionDatas = new List<RKSyncActionData>();
                syncDiction.Add(assetsId,rkSyncActionDatas);
                RDebug.I(TAG,$"AddSyncAssetListener()------>>assetsId: {assetsId}");
            }
            else
            {
                RDebug.I(TAG,$"AddSyncAssetListener()------>>Dictionary is exists {assetsId} Key, And Value:{syncDiction[assetsId].Count}");
            }
        }


        /// <summary>
        /// 清除所有的缓存同步动作数据
        /// </summary>
        /// <param name="assetsId"></param>
        public static void ClearCacheSyncActionData(string assetsId)
        {
            if (!syncDiction.ContainsKey(assetsId))
            {
                return;
            }

            RDebug.I(TAG,$"ClearCacheSyncActionData()------>>assetsId:{assetsId}");
            lock (_lock)
            {
                syncDiction[assetsId]?.Clear();
            }
        }

        /// <summary>
        /// 查询并获取该资源，最接近上一帧同步的数据
        /// </summary>
        /// <param name="assetsId"></param>
        /// <param name="lastSeq"></param>
        /// <returns></returns>
        public static RKSyncActionData GetRKSyncActionData(string assetsId,int lastSeq)
        {
            RDebug.I(TAG,$"GetRKSyncActionData()------>>assetsId:{assetsId} | lastSeq:{lastSeq}");
            if (!syncDiction.ContainsKey(assetsId))
            {
                return null;
            }

            lock (_lock)
            {
                List<RKSyncActionData> syncDataList = syncDiction[assetsId];
                if (null == syncDataList || syncDataList.Count == 0)
                {
                    return null;
                }

                RKSyncActionData syncActionData = null;
                int listIndex = 0;
                for (int i = 0; i < syncDataList.Count; i++)
                {
                    if (syncDataList[i].syncInfoData.seq > lastSeq)
                    {
                        syncActionData = syncDataList[i];
                        listIndex = i;
                        break;
                    }
                }

                if (listIndex != 0)
                {
                    syncDataList.RemoveRange(0,listIndex);
                }
                
                return syncActionData;
            }
        }


        public static void InitializeUdpClient()
        {
            // 初始化UdpClient
            udpClient = new UdpClient();

            // 设置远程端点（你的UDP服务器的IP地址和端口）
            // remoteEndPoint = new IPEndPoint(IPAddress.Parse("10.81.3.40"), 12345);
            remoteEndPoint = new IPEndPoint(IPAddress.Parse(updHost), 12345);
            // remoteEndPoint = new IPEndPoint(IPAddress.Parse("10.91.2.77"), 12345);

            
            new Thread(() =>
            {
                while (isConnectUdp)
                {
                    byte[] receivedData =  udpClient.Receive(ref remoteEndPoint);
                    string message = Encoding.UTF8.GetString(receivedData);

                   
                    SyncMessage syncMessage = JsonConvert.DeserializeObject<SyncMessage>(message);
                    // 未收到消息 || 收到的是自己发出的同步消息 || 收到的是非同步消息 || 收到的消息动作状态为空
                    if (syncMessage == null || syncMessage.uid.Equals(_localUid) ||
                        !syncMessage.type.Equals("sync") ||  string.IsNullOrEmpty(syncMessage.data))
                    {
                        continue;
                    }
                    
                    // 解析获取RKSyncActionData
                    var actionData = JsonConvert.DeserializeObject<RKSyncActionData>(syncMessage.data);
                    if (null == actionData || string.IsNullOrEmpty(actionData.assetId) ||
                        null == actionData.syncInfoData)
                    {
                        continue;
                    }
                    
                    if (!syncDiction.ContainsKey(actionData.assetId))
                    {
                        continue;
                    }

                    // 进行加锁新增数据
                    lock (_lock)
                    {
                        List<RKSyncActionData> syncDataList = syncDiction[actionData.assetId];
                        // 插入最新的数据
                        syncDataList.Add(actionData);
                        // 进行排序--->>从小到大排列
                        syncDataList.Sort((data1, data2) => data1.syncInfoData.seq.CompareTo(data2.syncInfoData.seq));

                        // 保证不超过50条数据
                        if (syncDataList.Count > 50)
                        {
                            syncDataList.RemoveAt(0);
                        }
                    }

                }
                
            }).Start();
            
        }

        
        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="obj"></param>
        public static void SendMessage(string message)
        {
            RDebug.I(TAG,$"SendMessage()------>>{message}");
       

            byte[] bytes = Encoding.UTF8.GetBytes(message);
            try
            {
                udpClient.Send(bytes, bytes.Length, remoteEndPoint);
                
            }
            catch (Exception err)
            {
                RDebug.I(TAG,$"{err.Message}------>>发送失败");
            }
        }
        
    }
}