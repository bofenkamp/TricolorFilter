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
        StartCamera();
    }

    void StartCamera()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        string webcamName = devices[camID].name;
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        camTex = new WebCamTexture(webcamName);
        renderer.material.mainTexture = camTex;
        try
        {
            camTex.Play();
        }
        catch
        {
            FlipCamera();
        }
    }

    public void FlipCamera()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        camID++;
        if (camID >= devices.Length)
        {
            camID = 0;
        }

#if (UNITY_IOS || UNITY_ANDROID)

        float quadRot = (transform.eulerAngles.y + 180) % 360;
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, quadRot, transform.eulerAngles.z);

#endif

        StartCamera();
    }

    void OnDestroy()
    {
        camTex.Stop();
    }
}
