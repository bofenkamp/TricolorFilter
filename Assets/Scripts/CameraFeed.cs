using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFeed : MonoBehaviour
{
    WebCamTexture camTex;
    int camID = 0;

    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
#if (UNITY_EDITOR || UNITY_STANDALONE)
        camID = 0;
#elif (UNITY_IOS || UNITY_ANDROID)
        camID = devices.Length - 1;
#endif
        Debug.Log($"{devices.Length} webcams found");
        Debug.Log(devices[camID].name);
        string webcamName = devices[camID].name;
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        camTex = new WebCamTexture(webcamName);
        renderer.material.mainTexture = camTex;
        camTex.Play();
    }

    void OnDestroy()
    {
        camTex.Stop();
    }
}
