using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhotoTaker : MonoBehaviour
{
    private GameObject canvas;
    int photosTaken;

    void Start()
    {
        canvas = GameObject.Find("Canvas");
        if (!PlayerPrefs.HasKey("photosTaken"))
            photosTaken = 0;
        else
            photosTaken = PlayerPrefs.GetInt("photosTaken");
    }

    public void TakePhoto()
    {
        photosTaken++;
        canvas.SetActive(false);
        ScreenCapture.CaptureScreenshot(Application.persistentDataPath + "/Tricolor" + photosTaken + ".png");
        canvas.SetActive(true);
    }

    private void OnDestroy()
    {
        PlayerPrefs.SetInt("photosTaken", photosTaken);
    }
}