using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomChatUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Input")]
    [SerializeField] private TMP_InputField messageInput;
    [SerializeField] private Button sendButton;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;

    [Header("Message List")]
    [SerializeField] private Transform messageContainer;
    [SerializeField] private ChatLineView chatLinePrefab;
    [SerializeField] private int maxVisibleMessages = 8;

    [Header("Behavior")]
    [SerializeField] private bool openOnStart = false;
    [SerializeField] private bool focusInputWhenOpened = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.Return;

    private readonly Queue<ChatLineView> _spawnedLines = new Queue<ChatLineView>();

    private RoomChat _roomChat;
    private bool _isOpen;

    private void Awake()
    {
        if (sendButton != null)
            sendButton.onClick.AddListener(HandleSendClicked);

        if (openButton != null)
            openButton.onClick.AddListener(OpenChat);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseChat);

        SetOpenState(openOnStart);
        ClearAllLines();
    }

    private void OnEnable()
    {
        TryBindRoomChat();

        if (_roomChat != null)
            _roomChat.OnMessageReceived += HandleMessageReceived;

        if (NetworkLauncher.Instance != null)
            NetworkLauncher.Instance.OnRoomJoined += HandleRoomJoined;
    }

    private void OnDisable()
    {
        if (_roomChat != null)
            _roomChat.OnMessageReceived -= HandleMessageReceived;

        if (NetworkLauncher.Instance != null)
            NetworkLauncher.Instance.OnRoomJoined -= HandleRoomJoined;

        PlayerInput.GameplayInputBlocked = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (_isOpen && messageInput != null && messageInput.isFocused)
            {
                HandleSendClicked();
                return;
            }

            SetOpenState(!_isOpen);
        }

        if (!_isOpen)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseChat();
            return;
        }

        bool typing = messageInput != null && messageInput.isFocused;
        PlayerInput.GameplayInputBlocked = typing;
    }

    private void HandleRoomJoined(string sceneName, string sessionName)
    {
        if (_roomChat != null)
            _roomChat.OnMessageReceived -= HandleMessageReceived;

        _roomChat = null;
        ClearAllLines();
        SetOpenState(false);

        TryBindRoomChat();

        if (_roomChat != null)
            _roomChat.OnMessageReceived += HandleMessageReceived;
    }

    private void TryBindRoomChat()
    {
        _roomChat = FindFirstObjectByType<RoomChat>();
    }

    private void HandleSendClicked()
    {
        if (_roomChat == null)
        {
            TryBindRoomChat();
            if (_roomChat == null)
            {
                Debug.LogWarning("[RoomChatUI] No RoomChat found in current scene.");
                return;
            }
        }

        if (messageInput == null)
            return;

        string text = messageInput.text;
        if (string.IsNullOrWhiteSpace(text))
            return;

        string senderName = "Guest";

        if (LocalPlayerData.Instance != null && !string.IsNullOrWhiteSpace(LocalPlayerData.Instance.PlayerName))
            senderName = LocalPlayerData.Instance.PlayerName;

        bool sent = _roomChat.TrySendMessage(senderName, text);
        if (!sent)
            return;

        messageInput.text = string.Empty;
        messageInput.ActivateInputField();
        messageInput.Select();
    }

    private void HandleMessageReceived(string senderName, string message)
    {
        if (chatLinePrefab == null || messageContainer == null)
            return;

        ChatLineView line = Instantiate(chatLinePrefab, messageContainer);
        line.Bind(senderName, message);
        _spawnedLines.Enqueue(line);

        while (_spawnedLines.Count > maxVisibleMessages)
        {
            ChatLineView oldest = _spawnedLines.Dequeue();
            if (oldest != null)
                Destroy(oldest.gameObject);
        }

        Canvas.ForceUpdateCanvases();
    }

    public void OpenChat()
    {
        SetOpenState(true);
    }

    public void CloseChat()
    {
        if (messageInput != null)
            messageInput.DeactivateInputField();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SetOpenState(false);
    }

    private void SetOpenState(bool isOpen)
    {
        _isOpen = isOpen;

        if (panelRoot != null)
            panelRoot.SetActive(isOpen);

        PlayerInput.GameplayInputBlocked = false;

        if (isOpen && focusInputWhenOpened && messageInput != null)
        {
            messageInput.ActivateInputField();
            messageInput.Select();
        }
    }

    private void ClearAllLines()
    {
        while (_spawnedLines.Count > 0)
        {
            ChatLineView line = _spawnedLines.Dequeue();
            if (line != null)
                Destroy(line.gameObject);
        }

        if (messageContainer == null)
            return;

        for (int i = messageContainer.childCount - 1; i >= 0; i--)
            Destroy(messageContainer.GetChild(i).gameObject);
    }
}