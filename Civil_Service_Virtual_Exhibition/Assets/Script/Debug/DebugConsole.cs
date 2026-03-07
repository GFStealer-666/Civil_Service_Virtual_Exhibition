using UnityEngine;
using System.Collections.Generic;

public class DebugConsole : MonoBehaviour
{
    private List<string> _logs = new(20);
    private GUIStyle _style;

    void OnEnable() => Application.logMessageReceived += OnLog;
    void OnDisable() => Application.logMessageReceived -= OnLog;

    void OnLog(string msg, string stack, LogType type)
    {
        _logs.Add($"[{type}] {msg}");
        if (_logs.Count > 40) _logs.RemoveAt(0);
    }

    void OnGUI()
    {
        _style ??= new GUIStyle { fontSize = 14, normal = { textColor = Color.white } };
        
        GUI.Box(new Rect(10, 10, 600, 280), "");
        for (int i = 0; i < _logs.Count; i++)
            GUI.Label(new Rect(15, 15 + i * 18, 590, 20), _logs[i], _style);
    }
}
