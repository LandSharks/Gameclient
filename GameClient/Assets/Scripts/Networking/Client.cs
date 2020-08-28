using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;



namespace Networking {
    public class Client : MonoBehaviour {
        public static Client instance;
        public static int dataBufferSize = 4096; //TODO: Store this some other way?

        public string ip = "127.0.0.1"; // TODO: chosing ip...?
        public int port = 8080;
        public int clientId = 0;
        public TCP tcp;
        public UDP udp;

        private bool isConnected = false;
        private delegate void PacketHandler(Packet packet);
        private static Dictionary<int, PacketHandler> packetHandlers;

        private void Awake() {
            if(instance == null) {
                instance = this;
            }
            else if(instance != null) {
                Debug.Log("Instance already exists, destroying object");
                Destroy(this);
            }
        }
        private void Start() {
            tcp = new TCP();
            udp = new UDP();
        }
        private void OnApplicationQuit() {
            Disconnect();
        }
        public void ConnectToServer() {
            InitClientData();
            isConnected = true;
            tcp.Connect();
        }
        private void Disconnect() {
            if(isConnected) {
                isConnected = false;
                tcp.socket.Close();
                udp.socket.Close();
                Debug.Log("Disconnected");
            }
        }
        private void InitClientData() {
            packetHandlers = new Dictionary<int, PacketHandler>() {
                { (int)ServerPackets.welcome, ClientHandle.Welcome },
                { (int)ServerPackets.spawnPlayer, ClientHandle.SpawnPlayer },
                { (int)ServerPackets.playerPostition, ClientHandle.PlayerPosition },
                { (int)ServerPackets.playerRotation, ClientHandle.PlayerRotation }
            };
            Debug.Log("Initialized packets.");
        }
        public class TCP {
            public TcpClient socket;

            private NetworkStream stream;
            private Packet receivedData;
            private byte[] recieveBuffer;

            public void Connect() {
                socket = new TcpClient() {
                    ReceiveBufferSize = dataBufferSize,
                    SendBufferSize = dataBufferSize
                };
                recieveBuffer = new byte[dataBufferSize];
                socket.BeginConnect(instance.ip, instance.port, ConnectCallback, socket);
            }
            private void Disconnect() {
                instance.Disconnect();
                stream = null;
                receivedData = null;
                recieveBuffer = null;
                socket = null;
            }

            private void ConnectCallback(IAsyncResult result) {
                socket.EndConnect(result);
                if(!socket.Connected) {
                    return;
                }

                stream = socket.GetStream();

                receivedData = new Packet();

                stream.BeginRead(recieveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            }

            public void SendData(Packet packet) {
                try {
                    if(socket != null) {
                        stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
                    }
                }
                catch(Exception ex) {
                    Debug.Log($"Error sending data to server via TCP: {ex.Message}");
                }
            }
            private void ReceiveCallback(IAsyncResult result) {
                try {
                    int byteLength = stream.EndRead(result);
                    if(byteLength <= 0) {
                        instance.Disconnect();
                        return;
                    }
                    byte[] data = new byte[byteLength];
                    Array.Copy(recieveBuffer, data, byteLength);

                    receivedData.Reset(HandleData(data));
                    stream.BeginRead(recieveBuffer, 0, dataBufferSize, ReceiveCallback, null);
                }
                catch(Exception ex) {
                    Disconnect();
                }
            }

            private bool HandleData(byte[] data) {
                int packetLength = 0;
                receivedData.SetBytes(data);

                // Check packet if the first 4 bytes contain an int. All packets start with an int representing length of packet
                if(receivedData.UnreadLength() >= 4) {
                    packetLength = receivedData.ReadInt();
                    if(packetLength <= 0) {
                        return true;
                    }
                }

                // As long as cond is true, receivedData has another complete packet we can handle               
                while(packetLength > 0 && packetLength <= receivedData.UnreadLength()) {
                    byte[] packetBytes = receivedData.ReadBytes(packetLength);
                    ThreadManager.ExecuteOnMainThread(() => {
                        using (Packet packet = new Packet(packetBytes)) {
                            int packetId = packet.ReadInt();
                            packetHandlers[packetId](packet);
                        }
                    });
                    packetLength = 0;
                    if(receivedData.UnreadLength() >= 4) {
                        packetLength = receivedData.ReadInt();
                        if(packetLength <= 0) {
                            return true;
                        }
                    }
                }
                if(packetLength <= 1) {
                    return true;
                }

                return false;
            }
        }
        public class UDP {
            public UdpClient socket;
            public IPEndPoint endPoint;

            public UDP() {
                endPoint = new IPEndPoint(IPAddress.Parse(instance.ip), instance.port);
            }
            public void Connect(int localPort) {
                socket = new UdpClient(localPort);
                socket.Connect(endPoint);
                socket.BeginReceive(ReceiveCallback, null);
                using(Packet packet = new Packet()) {
                    SendData(packet);
                }
            }
            public void Disconnect() {
                instance.Disconnect();
                endPoint = null;
                socket = null;
            }
            public void SendData(Packet packet) {
                try {
                    packet.InsertInt(instance.clientId);
                    if(socket != null) {
                        socket.BeginSend(packet.ToArray(), packet.Length(), null, null);
                    }
                } catch (Exception ex) {
                    Debug.Log($"Failed to send udp data for client {instance.clientId} {ex.Message}");
                }
            }
            private void ReceiveCallback(IAsyncResult result) {
                try {
                    byte[] data = socket.EndReceive(result, ref endPoint);
                    socket.BeginReceive(ReceiveCallback, null);

                    if(data.Length < 4) {
                        instance.Disconnect();
                        return;
                    }
                    HandleData(data);
                } catch(Exception ex) {
                    Disconnect();
                }
            }
            private void HandleData(byte[] data) {
                using(Packet packet = new Packet(data)) {
                    int packetLength = packet.ReadInt();
                    data = packet.ReadBytes(packetLength); //Reads first 4 bytes of packet that specifies length
                }
                ThreadManager.ExecuteOnMainThread(() => {
                    using(Packet packet = new Packet(data)) {
                        int packetId = packet.ReadInt();
                        packetHandlers[packetId](packet);
                    }
                });
            }
        }
    }
}