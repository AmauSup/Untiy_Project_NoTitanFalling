using UnityEngine;

public class PlayerCustomizationManager : MonoBehaviour
{
    public void SetBlue()
    {
        PlayerPrefs.SetString("PlayerColor", "Blue");
        PlayerPrefs.Save();
        Debug.Log("Blue selected");
    }

    public void SetYellow()
    {
        PlayerPrefs.SetString("PlayerColor", "Yellow");
        PlayerPrefs.Save();
        Debug.Log("Yellow selected");
    }

    public void ToggleHat()
    {
        int current = PlayerPrefs.GetInt("Hat", 0);
        int newValue = current == 0 ? 1 : 0;

        PlayerPrefs.SetInt("Hat", newValue);
        PlayerPrefs.Save();

        Debug.Log("Hat toggled = " + newValue);
    }

    public void ToggleWings()
    {
        int current = PlayerPrefs.GetInt("Wings", 0);
        int newValue = current == 0 ? 1 : 0;

        PlayerPrefs.SetInt("Wings", newValue);
        PlayerPrefs.Save();

        Debug.Log("Wings toggled = " + newValue);
    }
    
    public void SetFirstPerson()
    {
        PlayerPrefs.SetInt("FirstPerson", 1);
        PlayerPrefs.Save();
        Debug.Log("Camera = First Person");
    }

    public void SetThirdPerson()
    {
        PlayerPrefs.SetInt("FirstPerson", 0);
        PlayerPrefs.Save();
        Debug.Log("Camera = Third Person");
    }
}