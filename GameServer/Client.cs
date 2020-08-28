using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace GameServer {
    class Client {
        public static int dataBufferSize = 4096;
        public int id;
        public Player player;
        public TCP tcp;
        public UDP udp;

        public Client(int clientId) {
            id = clientId;
            tcp = new TCP(clientId);
            udp = new UDP(clientId);
        }

        public class TCP {
            public TcpClient socket;
            private readonly int id;
            private NetworkStream stream;
            private Packet receivedData;
            private byte[] receiveBuffer; 

            public TCP(int clientId) {
                id = clientId;
            }

            public void Connect(TcpClient _socket) {
                socket = _socket;
                socket.ReceiveBufferSize = dataBufferSize;
                socket.SendBufferSize = dataBufferSize;

                stream = socket.GetStream();

                receivedData = new Packet();
                receiveBuffer = new byte[dataBufferSize];

                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);

                ServerSend.Welcome(id, "Welcome");
            }

            public void Disconnect() {
                socket.Close();
                stream = null;
                receivedData = null;
                receiveBuffer = null;
                socket = null;
            }

            public  void SendData(Packet packet) {
                try {
                    if(socket != null) {
                        stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"Error sending data to player {id} via TCP: {ex.Message}");
                }
            }

            private void ReceiveCallback(IAsyncResult result) {
                try {
                    int byteLength = stream.EndRead(result);
                    if(byteLength <= 0) {
                        Server.clients[id].Disconnect();
                        return;
                    }
                    byte[] data = new byte[byteLength];
                    Array.Copy(receiveBuffer, data, byteLength);

                    receivedData.Reset(HandleData(data));
                    stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
                } catch (Exception ex) {
                    Console.WriteLine($"Error recieve TCP data: {ex.Message}");
                    Server.clients[id].Disconnect();
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
                        using(Packet packet = new Packet(packetBytes)) {
                            int packetId = packet.ReadInt();
                            Server.packetHandlers[packetId](id, packet);
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
            public IPEndPoint endPoint;
            private int id;

            public UDP(int _id) {
                id = _id;
            }

            public void Connect(IPEndPoint _endPoint) {
                endPoint = _endPoint;
            }

            public void Disconnect() {
                endPoint = null;
            }

            public void SendData(Packet packet) {
                Server.SendUDPData(endPoint, packet);
            }

            public void HandleData(Packet packet) {
                int packetLength = packet.ReadInt();
                byte[] packetBytes = packet.ReadBytes(packetLength);

                ThreadManager.ExecuteOnMainThread(() => {
                    using (Packet packet = new Packet(packetBytes)) {
                        int packetId = packet.ReadInt();
                        Server.packetHandlers[packetId](id, packet);
                    }
                });
            }
        }

        public void SendIntoGame(string playerName) {
            player = new Player(id, playerName, new System.Numerics.Vector3(0, 0, 0));

            //Spawning in all other players to our new player
            foreach(Client client in Server.clients.Values) {
                if(client.player != null) {
                    if(client.id != id) {
                        ServerSend.SpawnPlayer(id, client.player);
                    }
                    ServerSend.SpawnPlayer(client.id, player);
                }
            }
        }
        public void Disconnect() {
            Console.WriteLine($"{tcp.socket.Client.RemoteEndPoint} has disocnnected");

            player = null;
            tcp.Disconnect();
            udp.Disconnect();
        }
    }
}
