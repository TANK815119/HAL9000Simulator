using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace Rekabsen
{
    public class ButtonSceneTransition : NetworkBehaviour, ButtonActionInterface
    {
        [SerializeField] private string sceneName;
        //private NetworkVariable<bool> transitionCalled = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        // Update is called once per frame
        void Update()
        {
            //if (transitionCalled.Value)
            //{
            //    SceneTransition();
            //}
        }

        public void Play()
        {
            if(IsOwner)
            {
                //transitionCalled.Value = true;
                SceneTransitionServerRPC();
            }
        }

        [ServerRpc] //first to server
        private void SceneTransitionServerRPC(ServerRpcParams rpcParams = default)
        {
            //SceneTransitionClientRPC();
            if (IsServer)
            {
                Debug.Log("change scene called");
                ChangeScene();
            }
        }

        private void ChangeScene()
        {
            NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }

        [ClientRpc] //then to all the clients
        private void SceneTransitionClientRPC(ClientRpcParams rpcParams = default)
        {
            SceneTransition();
        }

        private void SceneTransition()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            {
                Debug.LogError("NetworkManager is not properly initialized or not listening.");
                return;
            }

            Debug.Log("SceneTransition called. Scene Name: " + sceneName);
            SceneTransitionHandler.Singleton.SetSceneName(sceneName);
            if (IsHost)
            {
                Debug.Log("Initializing as Host.");
                SceneTransitionHandler.Singleton.InitializeAsHost = true;
                SceneTransitionHandler.Singleton.InitializeAsMultiplayer = false;
                //SceneTransitionHandler.Singleton.JoinRelayCode = keyboard.GetText(); -- not neccessary
                SceneTransitionHandler.Singleton.Initialize();
            }
            if (!IsHost)
            {
                Debug.Log("Initializing as Client.");
                SceneTransitionHandler.Singleton.InitializeAsHost = false;
                SceneTransitionHandler.Singleton.InitializeAsMultiplayer = false;
                //SceneTransitionHandler.Singleton.JoinRelayCode = keyboard.GetText(); should be same as innitial join
                SceneTransitionHandler.Singleton.Initialize();
            }
        }
    }
}