using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class UpdateUIButtonColor : MonoBehaviour
{
    private Image image;
    private Material postEffectMat;

    private enum ColorInput { Black, Grey, White};
    [SerializeField] private ColorInput input;

    void Start()
    {
        image = GetComponent<Image>();
        postEffectMat = FindObjectOfType<PostEffectScript>().mat;
    }

    // Update is called once per frame
    void Update()
    {
        switch (input)
        {
            case ColorInput.Black:
                image.color = postEffectMat.GetColor("_Color1");
                break;
            case ColorInput.Grey:
                image.color = postEffectMat.GetColor("_Color2");
                break;
            case ColorInput.White:
                image.color = postEffectMat.GetColor("_Color3");
                break;
        }
    }
}
