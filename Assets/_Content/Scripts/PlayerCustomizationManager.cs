using UnityEngine;

/// <summary>
/// Sauvegarde les préférences de personnalisation dans PlayerPrefs.
/// Lu par PlayerAppearance au démarrage de la scène de jeu.
/// </summary>
public class PlayerCustomizationManager : MonoBehaviour
{
    public void SetBlue()
    {
        PlayerPrefs.SetString("PlayerColor", "Blue");
        PlayerPrefs.Save();
    }

    public void SetYellow()
    {
        PlayerPrefs.SetString("PlayerColor", "Yellow");
        PlayerPrefs.Save();
    }

    public void ToggleHat()
    {
        PlayerPrefs.SetInt("Hat", PlayerPrefs.GetInt("Hat", 0) == 0 ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void ToggleWings()
    {
        PlayerPrefs.SetInt("Wings", PlayerPrefs.GetInt("Wings", 0) == 0 ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetFirstPerson()
    {
        PlayerPrefs.SetInt("FirstPerson", 1);
        PlayerPrefs.Save();
    }

    public void SetThirdPerson()
    {
        PlayerPrefs.SetInt("FirstPerson", 0);
        PlayerPrefs.Save();
    }
}
