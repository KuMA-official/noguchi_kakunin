using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Net;
using System.Net.Sockets;

// クラス名を役割に合わせて変更（旧: Title）
public class NetworkConnectionManager : MonoBehaviour
{
    [Header("入力フィールド（接続先アドレス）")]
    public TMP_InputField addressInput;

    [Header("自身のアドレス表示用")]
    public TextMeshProUGUI myAddressText; // 追加: 自身のアドレスを表示するテキスト

    [Header("接続設定")]
    public ushort port = 7777;

    [Header("遷移先")]
    public string nextSceneName;

    private UnityTransport transport;

    void Start()
    {
        transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("NetworkManagerにUnityTransportがアタッチされていません。");
        }
        else
        {
            Debug.Log("UnityTransportを検出しました。");
        }

        // 起動時にローカルIPを取得してUIに表示する
        string localIP = GetLocalIPAddress();
        if (myAddressText != null)
        {
            myAddressText.text = "Your Address: " + localIP;
        }
    }

    private void Update()
    {
        // デバッグ用ショートカットキー
        if (Input.GetKeyDown(KeyCode.H))
        {
            StartHost();
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            StartClient();
        }
    }

    // --- 追加：1つのボタンから呼び出されるメソッド ---
    public void OnConnectButtonPressed()
    {
        // 入力フィールドのテキストを取得（空白文字は削除）
        string inputAddress = addressInput != null ? addressInput.text.Trim() : "";

        // 何も入力されていなければホスト、入力されていればクライアントとして開始
        if (string.IsNullOrEmpty(inputAddress))
        {
            Debug.Log("IPアドレスが未入力のため、ホストとして起動します。");
            StartHost();
        }
        else
        {
            Debug.Log($"IPアドレスが入力されているため、クライアントとして接続します。接続先: {inputAddress}");
            StartClient();
        }
    }

    private void ApplyAddress(bool isHost)
    {
        if (transport == null) return;

        string inputAddress = addressInput != null ? addressInput.text.Trim() : "";
        string localIP = GetLocalIPAddress(); // 動的にローカルIPを取得する処理に戻しました

        if (isHost)
        {
            // ホスト用設定
            transport.ConnectionData.Address = localIP;
            transport.ConnectionData.Port = port;
            transport.ConnectionData.ServerListenAddress = "0.0.0.0"; // 外部からの接続を全て許可

            Debug.Log($"ホスト設定完了：Listen={localIP}:{port} / serverListenAddress=0.0.0.0");
        }
        else
        {
            // クライアント用設定
            // 入力が空の場合はローカルIPに繋ぐ（テスト用保護）、入力があればそのIPに繋ぐ
            string connectTo = !string.IsNullOrEmpty(inputAddress) ? inputAddress : localIP;
            transport.ConnectionData.Address = connectTo;
            transport.ConnectionData.Port = port;

            Debug.Log($"クライアント設定完了：接続先={connectTo}:{port}");
        }
    }

    private string GetLocalIPAddress()
    {
        string localIP = "127.0.0.1";
        try
        {
            foreach (var ip in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                // IPv4アドレスのみを取得
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }
        }
        catch
        {
            Debug.LogWarning("ローカルIPアドレスの取得に失敗しました。");
        }
        return localIP;
    }

    private void StartHost()
    {
        ApplyAddress(true);
        if (NetworkManager.Singleton.StartHost())
        {
            Debug.Log("ホストを開始しました。");
            NetworkManager.Singleton.SceneManager.LoadScene(nextSceneName, LoadSceneMode.Single);
        }
        else
        {
            Debug.LogError("ホストの起動に失敗しました。");
        }
    }

    private void StartClient()
    {
        ApplyAddress(false);
        if (NetworkManager.Singleton.StartClient())
        {
            Debug.Log("クライアント接続開始。");
        }
        else
        {
            Debug.LogError("クライアント接続に失敗しました。");
        }
    }
}