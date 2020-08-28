using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace GameServer {
    class ServerHandle {
        public static void WelcomeReceived(int fromClient, Packet packet) {
            int clientIdCheck = packet.ReadInt();
            string username = packet.ReadString();

            Console.WriteLine($"{Server.clients[fromClient].tcp.socket.Client.RemoteEndPoint} connected successfully and is now player {fromClient}");
            if(fromClient != clientIdCheck) {
                Console.WriteLine($"Player \"{username}\" (ID: {fromClient} has assumed the wrong clientId ({clientIdCheck})!");
            }
            Server.clients[fromClient].SendIntoGame(username);
        }

        public static void PlayerMovement(int fromClient, Packet packet) {
            bool[] inputs = new bool[packet.ReadInt()];
            for(int i = 0; i < inputs.Length; i++) {
                inputs[i] = packet.ReadBool();
            }
            Quaternion rotation = packet.ReadQuaternion();

            Server.clients[fromClient].player.SetInput(inputs, rotation);
        }
    }
}
