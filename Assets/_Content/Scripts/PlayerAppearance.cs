using UnityEngine;

public class PlayerAppearance : MonoBehaviour
{
    public GameObject hat;
    public GameObject wings;

    void Start()
    {
        hat.SetActive(PlayerPrefs.GetInt("Hat", 0) == 1);
        wings.SetActive(PlayerPrefs.GetInt("Wings", 0) == 1);

        Debug.Log("Hat = " + PlayerPrefs.GetInt("Hat", 0));
        Debug.Log("Wings = " + PlayerPrefs.GetInt("Wings", 0));
    }
}