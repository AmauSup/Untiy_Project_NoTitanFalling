using UnityEngine;

/// <summary>
/// Applique l'apparence sauvegardée (chapeau, ailes) au démarrage.
/// Les valeurs sont persistées via PlayerCustomizationManager (PlayerPrefs).
/// </summary>
public class PlayerAppearance : MonoBehaviour
{
    [SerializeField] private GameObject _hat;
    [SerializeField] private GameObject _wings;

    void Start()
    {
        _hat.SetActive(PlayerPrefs.GetInt("Hat", 0) == 1);
        _wings.SetActive(PlayerPrefs.GetInt("Wings", 0) == 1);
    }
}
