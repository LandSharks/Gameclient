using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Networking;


namespace UI {
    public class UIManager : MonoBehaviour {
        public static UIManager instance;

        public GameObject startMenu;
        public InputField userNameField;
        private void Awake() {
            if(instance == null) {
                instance = this;
            }
            else if(instance != null) {
                Debug.Log("Instance already exists, destroying object");
                Destroy(this);
            }
        }
        public void OnConnectedToServer() {
            startMenu.SetActive(false);
            userNameField.interactable = false;
            Client.instance.ConnectToServer();
        }
    }
}