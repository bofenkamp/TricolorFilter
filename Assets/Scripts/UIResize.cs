using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIResize : MonoBehaviour
{
    //values
    [SerializeField] private float size = 0.4f;
    [SerializeField] private float margin = 10f;
    private float goldenRatio = 1.618034f;
    [SerializeField] private float sliderRatio = 0.1f;
    [SerializeField] private float markerRatio = .1167f;

    //UI elements
    private RectTransform canvas;
    [SerializeField] private RectTransform[] colorPickers;
    [SerializeField] private RectTransform[] sliders;
    [SerializeField] private RectTransform[] sliderKnobs;
    [SerializeField] private RectTransform[] colorButtons;
    [SerializeField] private RectTransform[] markers;
    [SerializeField] private RectTransform flipCamButton;
    [SerializeField] private RectTransform hideButton;
    [SerializeField] private RectTransform showButton;

    //shader-related
    [SerializeField] private Material filterMat;

    // Start is called before the first frame update
    void Start()
    {
        canvas = GetComponent<RectTransform>();
        margin *= Mathf.Min(canvas.sizeDelta.x, canvas.sizeDelta.y);
        float colorPickerSize = Mathf.Min(canvas.sizeDelta.x, canvas.sizeDelta.y) * size;
        float sliderWidth = Mathf.Clamp(colorPickerSize * sliderRatio, 20, canvas.sizeDelta.x);
        float sliderPos = colorPickerSize + margin + sliderWidth / 2f;
        float colorButtonHeight = (colorPickerSize - (colorButtons.Length - 1) * margin) / colorButtons.Length;
        float colorButtonWidth = colorButtonHeight * goldenRatio;
        float markerSize = colorPickerSize * markerRatio;
        float flipButtonLen = colorButtonHeight * (flipCamButton.sizeDelta.x / flipCamButton.sizeDelta.y);
        float camBottom;

        SetColorPickerSize(colorPickerSize);
        SetSliderSize(colorPickerSize, sliderWidth, sliderPos);
        SetKnobSize(sliderWidth);
        SetColorButtonSize(colorButtonHeight, colorButtonWidth);
        SetMarkerPositions(markerSize, colorPickerSize);
        SetCamFlipButtonSize(flipButtonLen, colorButtonHeight, colorPickerSize, out camBottom);
        SetHideAndShow(camBottom);
    }

    void SetColorPickerSize(float len)
    {
        foreach (RectTransform rt in colorPickers)
        {
            rt.sizeDelta = Vector2.one * len;
            rt.position = new Vector3(margin, canvas.sizeDelta.y - margin);
        }
    }

    void SetSliderSize(float h, float w, float x)
    {
        foreach (RectTransform rt in sliders)
        {
            rt.sizeDelta = new Vector2(w, h);
            rt.localPosition = new Vector3(x, -h/2f, 0);
        }
    }

    void SetKnobSize(float len)
    {
        foreach (RectTransform rt in sliderKnobs)
        {
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, len);
        }
    }

    void SetColorButtonSize(float h, float w)
    {
        float currY = canvas.sizeDelta.y - margin;
        foreach (RectTransform rt in colorButtons)
        {
            rt.sizeDelta = new Vector2(w, h);
            rt.position = new Vector3(margin, currY, 0);
            currY -= (h + margin);
        }
    }

    void SetMarkerPositions(float size, float colorPickerSize)
    {
        float top = canvas.sizeDelta.y - margin;
        float bottom = top - colorPickerSize;
        float left = margin;
        float right = left + colorPickerSize;

        for (int i = 0; i < markers.Length; i++)
        {
            RectTransform mark = markers[i];
            mark.sizeDelta = Vector2.one * size;
            Color color = filterMat.GetColor("_Color" + (i + 1).ToString());
            mark.GetComponent<Image>().color = color;
            float h;
            float s;
            float l;
            Color.RGBToHSV(color, out h, out s, out l);
            float x = left + (h * colorPickerSize);
            float y = bottom + (l * colorPickerSize);
            mark.position = new Vector3(x, y, 0);
        }
    }

    void SetCamFlipButtonSize(float l, float h, float colorPickerSize, out float camBottom)
    {
        flipCamButton.sizeDelta = new Vector2(l, h);
        flipCamButton.position = new Vector3(margin,
            canvas.sizeDelta.y - 2 * margin - colorPickerSize);
        camBottom = flipCamButton.position.y - flipCamButton.sizeDelta.y;
    }

    void SetHideAndShow(float camBottom)
    {
        hideButton.position = new Vector3(margin, camBottom - margin);
        showButton.position = new Vector3(margin, canvas.sizeDelta.y - margin);
    }
}