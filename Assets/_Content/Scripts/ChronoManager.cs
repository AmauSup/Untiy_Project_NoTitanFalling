using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class ChronoManager : MonoBehaviour
{
    public static ChronoManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private TextMeshProUGUI _scoresText;

    private float _elapsed;
    private bool _running;
    private readonly List<float> _scores = new List<float>();

    public bool IsRunning => _running;

    void Awake()
    {
        if (!Instance) Instance = this;
    }

    void Update()
    {
        if (!_running) return;
        _elapsed += Time.deltaTime;
        if (_timerText) _timerText.text = FormatTime(_elapsed);
    }

    public void StartChrono()
    {
        _elapsed = 0f;
        _running = true;
    }

    public void StopChrono()
    {
        if (!_running) return;
        _running = false;
        _scores.Add(_elapsed);
        RefreshScoresDisplay();

        // Respawn items and clear inventory for next run
        ItemPickup.RespawnAll();
        PlayerInventory.Instance?.Clear();
    }

    private void RefreshScoresDisplay()
    {
        if (!_scoresText) return;
        var sb = new StringBuilder();
        for (int i = 0; i < _scores.Count; i++)
            sb.AppendLine($"#{i + 1}  {FormatTime(_scores[i])}");
        _scoresText.text = sb.ToString();
    }

    private static string FormatTime(float t)
    {
        int min = (int)(t / 60);
        int sec = (int)(t % 60);
        int ms  = (int)((t * 100f) % 100f);
        return $"{min:00}:{sec:00}.{ms:00}";
    }
}
