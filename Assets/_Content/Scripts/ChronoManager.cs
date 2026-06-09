using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class ChronoManager : MonoBehaviour
{
    public static ChronoManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private TextMeshProUGUI _scoresText;

    [Tooltip("Délai en secondes après un stop avant de pouvoir relancer le chrono")]
    [SerializeField] private float _restartCooldown = 2f;

   

    private float _elapsed;
    private bool _running;
    private float _cooldownRemaining;
    private readonly List<float> _scores = new List<float>();

    public bool IsRunning => _running;

    void Awake()
    {
        if (!Instance) Instance = this;
    }

    void Update()
    {
        if (_cooldownRemaining > 0)
            _cooldownRemaining -= Time.deltaTime;

        if (!_running) return;
        _elapsed += Time.deltaTime;
        if (_timerText) _timerText.text = FormatTime(_elapsed);
    }

    public void StartChrono()
    {
        if (_cooldownRemaining > 0) return;

        _elapsed = 0f;
        _running = true;
        ItemPickup.RespawnAll();
        PlayerInventory.Instance?.Clear();
    }

    public void StopChrono()
    {
        if (!_running) return;
        _running = false;
        _cooldownRemaining = _restartCooldown;
        _scores.Add(_elapsed);
        RefreshScoresDisplay();
    }

    private void RefreshScoresDisplay()
    {
        if (!_scoresText) return;
        var sb = new StringBuilder();
        for (int i = 0; i < _scores.Count; i++)
            sb.AppendLine($"#{i + 1}  {FormatTime(_scores[i])}");
        _scoresText.text = sb.ToString();
    }

    public static string FormatTime(float t)
    {
        int min = (int)(t / 60);
        int sec = (int)(t % 60);
        int ms  = (int)((t * 100f) % 100f);
        return $"{min:00}:{sec:00}.{ms:00}";
    }
}
