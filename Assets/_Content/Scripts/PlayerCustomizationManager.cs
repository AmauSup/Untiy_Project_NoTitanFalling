using UnityEngine;

public class PlayerCustomizationManager : MonoBehaviour
{
    public Renderer robotRenderer;

    public GameObject hat;
    public GameObject wings;

    private void Start()
    {
        LoadCustomization();
    }

    public void SetBlue()
    {
        ChangeColor(Color.blue);

        PlayerPrefs.SetString("PlayerColor", "Blue");
        PlayerPrefs.Save();

        Debug.Log("Blue selected");
    }

    public void SetYellow()
    {
        ChangeColor(Color.yellow);

        PlayerPrefs.SetString("PlayerColor", "Yellow");
        PlayerPrefs.Save();

        Debug.Log("Yellow selected");
    }

    private void ChangeColor(Color newColor)
    {
        if (robotRenderer == null)
            return;

        Material[] mats = robotRenderer.materials;

        foreach (Material mat in mats)
        {
            mat.color = newColor;
        }
    }

    public void ToggleHat()
    {
        if (hat == null)
            return;

        bool state = !hat.activeSelf;

        hat.SetActive(state);

        PlayerPrefs.SetInt("HatEnabled", state ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void ToggleWings()
    {
        if (wings == null)
            return;

        bool state = !wings.activeSelf;

        wings.SetActive(state);

        PlayerPrefs.SetInt("WingsEnabled", state ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void LoadCustomization()
    {
        string color = PlayerPrefs.GetString("PlayerColor", "Yellow");

        if (color == "Blue")
            ChangeColor(Color.blue);
        else
            ChangeColor(Color.yellow);

        if (hat != null)
            hat.SetActive(PlayerPrefs.GetInt("HatEnabled", 1) == 1);

        if (wings != null)
            wings.SetActive(PlayerPrefs.GetInt("WingsEnabled", 1) == 1);
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