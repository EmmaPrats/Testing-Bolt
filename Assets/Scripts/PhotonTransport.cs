//using System;
//using System.Collections.Generic;
//using System.Threading;
//using UnityEngine;
//
//namespace Biped.Multiplayer.Photon
//{
//    public class PhotonTransport : NetTransport
//    {
//#region Unity events
//
//        public void Update() //THIS IS BEING CALLED FROM GameNetWorkSystem?!?!?!?!
//        {
//            ProcessNetworkPackages();
//            SnapShotMgr.Instance.Update();
//        }
//
//        public void FixedUpdate()
//        {
//            ProcessNetworkPackages();
//            SnapShotMgr.Instance.Update();
//        }
//
//        private void OnDestroy()
//        {
//            TearDown();
//        }
//
//#endregion
//
//#region Initialization, Set Up, Tear Down
//
//        private bool mIsSetUp;
//
//        private void SetUp()
//        {
//            if (mIsSetUp)
//                return;
//
//            EventManager.Instance.RegisterEvent(EEvent.EAddPeer, OnAddPeer);
//            EventManager.Instance.RegisterEvent(EEvent.ERemovePeer, OnRemovePeer);
//            EventManager.Instance.RegisterEvent(EEvent.EQuickGame, OnQuitGame);
//
//            mPacketReadThread = new Thread(StreamPacketReadLoop);
//            mPacketReadThread.IsBackground = true;
//            mPacketReadThread.Start();
//
//            mIsSetUp = true;
//        }
//
//        private void TearDown()
//        {
//            if (!mIsSetUp)
//                return;
//
//            EventManager.Instance.RemoveEvent(EEvent.EAddPeer, OnAddPeer);
//            EventManager.Instance.RemoveEvent(EEvent.ERemovePeer, OnRemovePeer);
//            EventManager.Instance.RemoveEvent(EEvent.EQuickGame, OnQuitGame);
//
//            mPlayPacketReadThread = false;
//            mPacketReadThread?.Abort();
//
//            mIsSetUp = false;
//        }
//
//        private void OnQuitGame(object ev)
//        {
//           TearDown();
//        }
//
//#endregion
//
//#region NetTransport
//
//        public override void Init()
//        {
//            Debug.Log($"#### {name} :: {GetType().Name} :: Init()");
//            SetUp();
//        }
//
//        private bool mIsServerStarted = false;
//
//        public override void StartServer(OnStartHostDelegate callback)
//        {
//            Debug.Log($"#### {name} :: {GetType().Name} :: StartServer()");
//            Application.runInBackground = true;
//
//            if (!mIsServerStarted)
//            {
//                mIsServerStarted = true;
//                callback.Invoke(null);
//            }
//        }
//
//        public override void StopServer()
//        {
//            Debug.Log($"#### {name} :: {GetType().Name} :: StopServer()");
//            AvailablePeerList.Clear();
//            mIsServerStarted = false;
//        }
//
//        public override void JoinGame(ulong peerID, OnJoinGameDelegate callback)
//        {
//            Debug.Log($"#### {name} :: {GetType().Name} :: JoinGame(peerID: {peerID})");
//            AvailablePeerList.Add(peerID);
//            callback.Invoke(peerID, null);
//        }
//
//        public override void LeaveGame()
//        {
//            Debug.Log($"#### {name} :: {GetType().Name} :: LeaveGame()");
//            AvailablePeerList.Clear();
//        }
//
//        public override void SendReliable(NetHost host, byte[] data, int len)
//        {
//            var photonPacket = new PhotonPacket(data, (uint) len, LobbySystem.Instance.LocalUID());
//#if CONNECTION_DEBUG
//            MultiplayerDebug.Instance.LogSending(
//                $"\n    == SENDING RELIABLE ==    TimeCounter.TickID: {TimeCounter.TickID}" +
//                $"\n        {string.Join("\n", host.ToStringLineByLine("        "))}" +
//                $"\n        data ({data.Length} length)" +
////                $"\n            {string.Join(", ", data)}" +
//                $"\n        len: {len}" +
//                $"\n        photonPacket:\n{string.Join("\n", photonPacket.ToStringLineByLine("            "))}");
////            Debug.Log($"#### {name} :: {GetType().Name} :: SendReliable(host.connectIdx = {host.connectIdx}, host.peerID: {host.peerID}, data.Length: {data.Length}, len: {len})");
//#endif
//            PhotonClient.Instance.SendReliable(photonPacket);
//        }
//
//        public override void SendUnreliable(NetHost host, byte[] data, int len)
//        {
//#if CONNECTION_DEBUG
////            Debug.Log($"#### {name} :: {GetType().Name} :: SendUnreliable(host.connectIdx = {host.connectIdx}, host.peerID: {host.peerID}, data.Length: {data.Length}, len: {len}, BunchQueue.BUNCHSIZE: {BunchQueue.BUNCHSIZE}) :: {(len > BunchQueue.BUNCHSIZE ? "calling BunchSendPkg()" : "calling OpRaiseEvent()")}");
//#endif
//            if (len > BunchQueue.BUNCHSIZE)
//            {
//#if CONNECTION_DEBUG
//                MultiplayerDebug.Instance.LogSending(
//                    $"\n    == SENDING UN-RELIABLE ==    TimeCounter.TickID: {TimeCounter.TickID}" +
//                    $"\n{string.Join("\n", host.ToStringLineByLine("        "))}" +
//                    $"\n        data ({data.Length} length)" +
////                $"\n            {string.Join(", ", data)}" +
//                    $"\n        len: {len} (BunchQueue.BUNCHSIZE: {BunchQueue.BUNCHSIZE})");
//#endif
//                BunchSendPkg(host, data, len, false);
//            }
//            else
//            {
//                var photonPacket = new PhotonPacket(data, (uint) len, LobbySystem.Instance.LocalUID());
//#if CONNECTION_DEBUG
//                MultiplayerDebug.Instance.LogSending(
//                    $"\n    == SENDING UN-RELIABLE ==    TimeCounter.TickID: {TimeCounter.TickID}" +
//                    $"\n{string.Join("\n", host.ToStringLineByLine("        "))}" +
//                    $"\n        data ({data.Length} length)" +
////                $"\n            {string.Join(", ", data)}" +
//                    $"\n        len: {len} (BunchQueue.BUNCHSIZE: {BunchQueue.BUNCHSIZE})" +
//                    $"\n        photonPacket:\n{string.Join("\n",photonPacket.ToStringLineByLine("            "))}");
//#endif
//                PhotonClient.Instance.SendUnreliable(photonPacket);
//            }
//        }
//
//        public override void CloseConnection(NetHost host)
//        {
//            Debug.Log($"#### {name} :: {GetType().Name} :: CloseConnection(peer: {host.peerID})");
//            if (AvailablePeerList.Contains(host.peerID))
//                AvailablePeerList.Remove(host.peerID);
//        }
//
//#endregion
//
//#region Peers
//
//        private static List<ulong> AvailablePeerList = new List<ulong>(); //TODO rename.
//
//        private void OnAddPeer(object ev)
//        {
//            var addP2PPeerEvent = ev as EventAddP2PPeer;
//            if (addP2PPeerEvent == null)
//                return;
//            if (!AvailablePeerList.Contains(addP2PPeerEvent.peerID))
//            {
//                AvailablePeerList.Add(addP2PPeerEvent.peerID);
//                GameNetWorkSystem.OnNewConnection(addP2PPeerEvent.peerID);
//            }
//        }
//
//        private void OnRemovePeer(object ev)
//        {
//            var peerId = (ulong) ev;
//            if (AvailablePeerList.Contains(peerId))
//            {
//                AvailablePeerList.Remove(peerId);
//                GameNetWorkSystem.OnDisconnected(peerId);
//            }
//        }
//
//#endregion
//
//#region Packet reading
//
//        //Are these static fields instead of function variables because of multi-threading?
//        private static MessageDecoder mMessageDecoder = new MessageDecoder();
//        private static Queue<NetWorkEvent> mReliableMessages = new Queue<NetWorkEvent>();
//        private static Queue<NetWorkEvent> mUnreliableMessages = new Queue<NetWorkEvent>();
//
//        private Thread mPacketReadThread;
//        private bool mPlayPacketReadThread = true;
//
//        private void StreamPacketReadLoop()
//        {
//            try
//            {
//                while (mPlayPacketReadThread)
//                {
//                    Thread.Sleep(2);
//                    StreamPacketReadBackgroundThread();
//                }
//            }
//            catch (ThreadAbortException)
//            {
//                Thread.ResetAbort();
//            }
//        }
//
//        //Is this a static field instead of a function variable because of multi-threading?
//        private static List<NetWorkEvent> mFrameProcList = new List<NetWorkEvent>();
//
//        private void StreamPacketReadBackgroundThread()
//        {
////            Debug.Log($"#### {GetType().Name} :: StreamPacketReadBackgroundThread()");
//
//            mFrameProcList.Clear();
//
//            PhotonLobby.ReadP2PPackageMultiThread(mFrameProcList, mMessageDecoder);
//            foreach (var networkEvent in mFrameProcList)
//                StreamMessageRead(networkEvent);
//
//            mFrameProcList.Clear();
//        }
//
//        /// <remarks>Since we are multi-threading, this method will be called in the background thread.</remarks>
//        private void StreamMessageRead(NetWorkEvent networkEvent)
//        {
//            var message = networkEvent.data as Message;
//
//            if (message != null)
//            {
//#if CONNECTION_DEBUG
//                MultiplayerDebug.Instance.LogReceiving($"\n        == READ MESSAGE ==    TimeCounter.TickID: {TimeCounter.TickID}\n{string.Join("\n", message.ToStringLineByLine("            "))}");
//#endif
//                if (!message.head.msgReliable)
//                    GameNetWorkSystem.stat_.RecvStat(message.head.ackSeq, message.head.sendSeq, Timestamp.curRealTime);
//
//                if (message.head.msgId == NetMsgId.DeltaState && message.head.RoomID == GameState.RoomID)
//                {
//                    SnapShotMgr.RecvDeltaSnapShot(message);
//                    Message.ResetMessage(message);
//                    return;
//                }
//
//                if (message.head.msgReliable)
//                {
//                    lock (mReliableMessages)
//                    {
//                        mReliableMessages.Enqueue(networkEvent);
//                    }
//                }
//                else
//                {
//                    lock (mUnreliableMessages)
//                    {
//                        mUnreliableMessages.Enqueue(networkEvent);
//                    }
//                }
//            }
//#if CONNECTION_DEBUG
//            else
//                MultiplayerDebug.Instance.LogSending($"\n        == networkEvent.data is NOT Message ==    TimeCounter.TickID: {TimeCounter.TickID}");
//#endif
//        }
//
//        //These could be variables inside the function:
//        private Queue<NetWorkEvent> mReliableMessagesToProcess = new Queue<NetWorkEvent>();
//        private Queue<NetWorkEvent> mUnreliableMessagesToProcess = new Queue<NetWorkEvent>();
//
//        private void ProcessNetworkPackages()
//        {
//            lock (mReliableMessages)
//            {
//                while (mReliableMessages.Count > 0)
//                {
//                    mReliableMessagesToProcess.Enqueue(mReliableMessages.Dequeue());
//                }
//            }
//
//            lock (mUnreliableMessages)
//            {
//                while (mUnreliableMessages.Count > 0)
//                {
//                    mUnreliableMessagesToProcess.Enqueue(mUnreliableMessages.Dequeue());
//                }
//            }
//
//            var begin = Time.realtimeSinceStartup;
//            var end = begin;           
//            while (mReliableMessagesToProcess.Count > 0 && end < begin + TimeCounter.FixedDeltaTime/5.0f)
//            {
//                var networkEvent = mReliableMessagesToProcess.Dequeue();
//                HandleReadEv(networkEvent);
//                end = Time.realtimeSinceStartup;
//            }
//
//            while (mUnreliableMessagesToProcess.Count > 0 && end < begin + TimeCounter.FixedDeltaTime/5.0f)
//            {
//                var networkEvent = mUnreliableMessagesToProcess.Dequeue();
//                HandleReadEv(networkEvent);
//                end = Time.realtimeSinceStartup;
//            }
//
//            //不可靠消息最多保留3个 Keep up to 3 unreliable messages
//            while (mUnreliableMessagesToProcess.Count > 3)
//            {
//                var networkEvent = mUnreliableMessagesToProcess.Dequeue();
//                Message.ResetMessage(networkEvent.data as Message);
//            }
//        }
//
//        private void HandleReadEv(NetWorkEvent ev)
//        {
//            var message = ev.data as Message;
//#if CONNECTION_DEBUG
////            Debug.Log($"#### {name} :: {GetType().Name} :: HandleReadEv(from: {ev.arg1}, message: {(message == null ? "[ev is NOT a Message!]" : $"{message.name}, {(message is S2CHelloMessage ? $"message.ret: {((S2CHelloMessage) message).ret}, " : message is C2SHelloMessage ? $"message.localUID: {((C2SHelloMessage) message).localUID}, " : "")}message.head.RoomID: {message.head.RoomID}")}) :: GameState.RoomID == {GameState.RoomID}");
//#endif
//
//#if PROFILE_ON
//            UnityEngine.Profiling.Profiler.BeginSample("Msg Proc");
//            UnityEngine.Profiling.Profiler.BeginSample(message.name);
//#endif
//            if (message.head.RoomID == GameState.RoomID)
//            {
//                GameNetWorkSystem.RecvMessage((ulong) ev.arg1, message);
//            }
//
//            Message.ResetMessage(message);
//
//#if PROFILE_ON
//            UnityEngine.Profiling.Profiler.EndSample();
//            UnityEngine.Profiling.Profiler.EndSample();
//#endif
//        }
//
//#endregion
//
//#region Bunch
//
//        private static int mBunchSeq = 10237;
//        private static byte[] mBunchToSend = new byte[BunchQueue.BUNCHSIZE + 256];
//        private WriteStream mBunchSendStream = new WriteStream();
//
////        private void BunchSendPkg(NetHost host, byte[] data, int originalLength, bool reliable)
////        {
////            var packetStartIndex = 0;
////            var bunchIndex = 0;
////
////            var seq = (mBunchSeq++) % 257123712;
////
////            var bunchCount = originalLength % BunchQueue.BUNCHSIZE == 0
////                ? originalLength / BunchQueue.BUNCHSIZE
////                : originalLength / BunchQueue.BUNCHSIZE + 1;
////
////            var lengthLeft = originalLength;
////            while (lengthLeft > 0)
////            {
////                var packetLength = lengthLeft <= BunchQueue.BUNCHSIZE
////                    ? lengthLeft
////                    : BunchQueue.BUNCHSIZE;
////
////                lengthLeft -= packetLength;
////
////                var bunchMessage = (BunchMessage) Message.CreateMessage(NetMsgId.PacketBunch);
////                Array.Copy(data, packetStartIndex, bunchMessage.bunchBuffer, 0, packetLength);
////                bunchMessage.Seq = seq;
////                bunchMessage.index = bunchIndex;
////                bunchMessage.bunchCnt = bunchCount;
////                bunchMessage.bunchLen = packetLength;
////
////                mBunchSendStream.Start(mBunchToSend, BunchQueue.BUNCHSIZE + 256);
////
////                bunchMessage.Encode(mBunchSendStream);
////
////                var dataLength = mBunchSendStream.DataLen();
////
////                PhotonClient.Instance.SendUnreliable(new PhotonPacket(mBunchToSend, (uint) dataLength, LobbySystem.Instance.LocalUID()));
////
////                BunchMessage.Destory(bunchMessage);
////
////                packetStartIndex += packetLength;
////                bunchIndex++;
////            }
////        }
//
//        void BunchSendPkg(NetHost host, byte[] data, int orglen, bool reliable)
//        {
//            int len = orglen;
//
//            int leftLen = len;
//
//            int sendLen = 0;
//
//            int bunchIndex = 0;
//
//            int seq = (mBunchSeq++) % 257123712;
//
//            int bunchCnt = (len % BunchQueue.BUNCHSIZE == 0)
//                ? (len / BunchQueue.BUNCHSIZE)
//                : (len / BunchQueue.BUNCHSIZE) + 1;
//
//            while (leftLen > 0)
//            {
//                int packLen = leftLen;
//
//                if (leftLen > BunchQueue.BUNCHSIZE)
//                {
//                    packLen = BunchQueue.BUNCHSIZE;
//                }
//
//                leftLen -= packLen;
//
//                BunchMessage bunch = Message.CreateMessage(NetMsgId.PacketBunch) as BunchMessage;
//
//                Array.Copy(data, sendLen, bunch.bunchBuffer, 0, packLen);
//
//                sendLen += packLen;
//
//                bunch.Seq = seq;
//
//                bunch.index = bunchIndex;
//
//                bunch.bunchCnt = bunchCnt;
//
//                bunch.bunchLen = packLen;
//
//                mBunchSendStream.Start(mBunchToSend, BunchQueue.BUNCHSIZE + 256);
//
//                bunch.Encode(mBunchSendStream);
//
//                int dataLen = mBunchSendStream.DataLen();
//
//                var photonPacket = new PhotonPacket(mBunchToSend, (uint) dataLen, LobbySystem.Instance.LocalUID());
//#if CONNECTION_DEBUG
//                MultiplayerDebug.Instance.LogSending(
//                    $"\n        == SENDING BUNCH UN-RELIABLE ==    TimeCounter.TickID: {TimeCounter.TickID}" +
//                    $"\n            mBunchToSend ({mBunchToSend.Length} length)" +
////                    $"\n                {string.Join(", ", mBunchToSend)}" +
//                    $"\n            dataLen: {dataLen}" +
//                    $"\n            photonPacket:\n{string.Join("\n", photonPacket.ToStringLineByLine("                "))}");
//#endif
//                PhotonClient.Instance.SendUnreliable(photonPacket);
//
//                BunchMessage.Destory(bunch);
//
//                bunchIndex++;
//            }
//        }
//
//        #endregion
//    }
//}
