using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace GameServer {
    class ServerSend {

        public static void SendTCPData(int toClient, Packet packet) {
            packet.WriteLength();
            Server.clients[toClient].tcp.SendData(packet);
        }
        private static void SendTCPDataToAll(Packet packet) {
            packet.WriteLength();
            for(int i = 1; i <= Server.MaxPlayers; i++) {
                Server.clients[i].tcp.SendData(packet);
            }
        }

        private static void SendTCPDataToAll(int excludeClient, Packet packet) {
            packet.WriteLength();
            for(int i = 1; i <= Server.MaxPlayers; i++) {
                if(i != excludeClient) {
                    Server.clients[i].tcp.SendData(packet);
                }
            }
        }
        public static void SendUDPData(int toClient, Packet packet) {
            packet.WriteLength();
            Server.clients[toClient].udp.SendData(packet);
        }
        private static void SendUDPDataToAll(Packet packet) {
            packet.WriteLength();
            for(int i = 1; i <= Server.MaxPlayers; i++) {
                Server.clients[i].udp.SendData(packet);
            }
        }
        private static void SendUDPDataToAll(int excludeClient, Packet packet) {
            packet.WriteLength();
            for(int i = 1; i <= Server.MaxPlayers; i++) {
                if(i != excludeClient) {
                    Server.clients[i].udp.SendData(packet);
                }
            }
        }
        public static void Welcome(int toClient, string msg) {
            using (Packet packet = new Packet((int)ServerPackets.welcome)) {
                packet.Write(msg);
                packet.Write(toClient);
                SendTCPData(toClient, packet);
            }
        }
        public static void SpawnPlayer(int toClient, Player player) {
            using(Packet packet = new Packet((int)ServerPackets.spawnPlayer)) {
                packet.Write(player.id);
                packet.Write(player.username);
                packet.Write(player.position);
                packet.Write(player.rotation);

                SendTCPData(toClient, packet);
            }
        }
        public static void PlayerPosition(Player player) {
            using(Packet packet = new Packet((int)ServerPackets.playerPostition)) {
                packet.Write(player.id);
                packet.Write(player.position);

                SendUDPDataToAll(packet);
            }
        }
        public static void PlayerRotation(Player player) {
            using(Packet packet = new Packet((int)ServerPackets.playerRotation)) {
                packet.Write(player.id);
                packet.Write(player.rotation);

                SendUDPDataToAll(player.id, packet);
            }
        }
    }
}
