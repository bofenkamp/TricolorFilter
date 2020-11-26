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

    //shader-related
    [SerializeField] private Material filterMat;

    // Start is called before the first frame update
    void Start()
    {
        canvas = GetComponent<RectTransform>();
        margin *= Mathf.Min(canvas.sizeDelta.x, canvas.sizeDelta.y);
        float colorPickerSize = Mathf.Min(canvas.sizeDelta.x, canvas.sizeDelta.y) * size;
        float sliderWidth = Mathf.Clamp(colorPickerSize * sliderRatio, 20, canvas.sizeDelta.x);
        float sliderPos = -colorPickerSize - margin - sliderWidth / 2f;
        float colorButtonHeight = (colorPickerSize - (colorButtons.Length - 1) * margin) / colorButtons.Length;
        float colorButtonWidth = colorButtonHeight * goldenRatio;
        float markerSize = colorPickerSize * markerRatio;

        SetColorPickerSize(colorPickerSize);
        SetSliderSize(colorPickerSize, sliderWidth, sliderPos);
        SetKnobSize(sliderWidth);
        SetColorButtonSize(colorButtonHeight, colorButtonWidth);
        SetMarkerPositions(markerSize, colorPickerSize);
    }

    void SetColorPickerSize(float len)
    {
        foreach (RectTransform rt in colorPickers)
        {
            rt.sizeDelta = Vector2.one * len;
            rt.position = new Vector3(canvas.sizeDelta.x - margin, canvas.sizeDelta.y - margin);
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
            rt.position = new Vector3(canvas.sizeDelta.x - margin, currY, 0);
            currY -= (h + margin);
        }
    }

    void SetMarkerPositions(float size, float colorPickerSize)
    {
        float top = canvas.sizeDelta.y - margin;
        float bottom = top - colorPickerSize;
        float right = canvas.sizeDelta.x - margin;
        float left = right - colorPickerSize;

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
}
