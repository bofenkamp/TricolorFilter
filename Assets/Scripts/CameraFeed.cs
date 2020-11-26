using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFeed : MonoBehaviour
{
    WebCamTexture camTex;

    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        string webcamName = devices[devices.Length - 1].name;
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
