using System;
using UnityEngine;
using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using Epic.OnlineServices.Platform;
using Steamworks;
using Epic.OnlineServices.Logging;
using UnityEngine.SceneManagement;

public class SteamEOSIntegration : MonoBehaviour
{
    public string ProductName = "MyUnityApplication";
    public string ProductVersion = "1.0";
    public string ProductId = "";
    public string SandboxId = "";
    public string DeploymentId = "";
    public string ClientId = "";
    public string ClientSecret = "";

    private static PlatformInterface eosPlatform;
    private const float PlatformTickInterval = 0.1f;
    private float platformTickTimer = 0f;

    private byte[] steamAuthTicket = new byte[1024];  
    private uint ticketSize; 
    private HAuthTicket authTicketHandle;
    void Start()
    {
        if (!SteamAPI.Init())
        {
            Debug.LogError("Failed to initialize Steamworks!");
            return;
        }

        Debug.Log("Steamworks initialized!");

       SteamNetworkingIdentity identity = new SteamNetworkingIdentity();
       authTicketHandle = SteamUser.GetAuthSessionTicket(steamAuthTicket, steamAuthTicket.Length, out ticketSize, ref identity);


        if (ticketSize > 0)
        {
            Debug.Log($"Steam Auth Session Ticket acquired. Size: {ticketSize}");

            string steamTicketBase64 = Convert.ToBase64String(steamAuthTicket, 0, (int)ticketSize);

            InitializeEOS(steamTicketBase64);
        }
        else
        {
            Debug.LogError("Failed to acquire Steam Auth Session Ticket.");
        }
    }

    private void InitializeEOS(string steamTicketBase64)
    {
        Debug.Log("Starting InitializeEOS...");

        var initializeOptions = new InitializeOptions()
        {
            ProductName = ProductName,
            ProductVersion = ProductVersion
        };

        var initializeResult = PlatformInterface.Initialize(ref initializeOptions);
        if (initializeResult != Result.Success)
        {
            Debug.LogError("Failed to initialize EOS Platform: " + initializeResult);
            return;
        }

        LoggingInterface.SetLogLevel(LogCategory.AllCategories, LogLevel.VeryVerbose);
        LoggingInterface.SetCallback((ref LogMessage logMessage) => Debug.Log(logMessage.Message));

        var options = new Options()
        {
            ProductId = ProductId,
            SandboxId = SandboxId,
            DeploymentId = DeploymentId,
            ClientCredentials = new ClientCredentials()
            {
                ClientId = ClientId,
                ClientSecret = ClientSecret
            }
        };

        eosPlatform = PlatformInterface.Create(ref options);
        if (eosPlatform == null)
        {
            Debug.LogError("Failed to create EOS Platform.");
            return;
        }

        Debug.Log("EOS Platform initialized. Logging in with Steam...");

        AuthenticateWithEOS(steamTicketBase64);
    }

    private void AuthenticateWithEOS(string steamTicketBase64)
    {
        Debug.Log("starting AuthenticateWithEOS...");

        var eosAuthInterface = eosPlatform.GetAuthInterface();
        if (eosAuthInterface == null)
        {
            Debug.LogError("EOS Auth Interface is not initialized.");
            return;
        }

    var loginOptions = new LoginOptions()
    {
        Credentials = new Credentials()
        {
            Type = LoginCredentialType.ExternalAuth,
            ExternalType = ExternalCredentialType.SteamSessionTicket,
            Token = steamTicketBase64
        },
        ScopeFlags = AuthScopeFlags.BasicProfile | AuthScopeFlags.FriendsList | AuthScopeFlags.Presence
    };

        // Login to EOS
        eosAuthInterface.Login(ref loginOptions, null, (ref LoginCallbackInfo loginCallbackInfo) =>
        {
            if (loginCallbackInfo.ResultCode == Result.Success)
            {
                Debug.Log($"Successfully authenticated with EOS! User ID: {loginCallbackInfo.LocalUserId}");
                SceneManager.LoadScene("test2");
            }
            else
            {
                Debug.LogError($"EOS login failed: {loginCallbackInfo.ResultCode}");
            }
        });
    }

    private void Update()
    {
        if (eosPlatform != null)
        {
            platformTickTimer += Time.deltaTime;

            if (platformTickTimer >= PlatformTickInterval)
            {
                platformTickTimer = 0;
                eosPlatform.Tick();
            }
        }
    }

    private void OnDestroy()
    {
        if (eosPlatform != null)
        {
            eosPlatform.Release();
            eosPlatform = null;
            PlatformInterface.Shutdown();
        }

        // Shutdown Steam API
        SteamAPI.Shutdown();
    }
}
