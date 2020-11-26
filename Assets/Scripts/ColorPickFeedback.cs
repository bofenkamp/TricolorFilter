using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColorPickFeedback : MonoBehaviour
{
    //positioning/color of this object
    [SerializeField] private Vector2 screenCenter;
    [SerializeField] private float left;
    [SerializeField] private float right;
    [SerializeField] private float top;
    [SerializeField] private float bottom;
    private Material mat;
    public float sliderVal { get; set; }

    //transforms & GameObjects
    private Transform canvas;
    private RectTransform rt;
    private RectTransform slider;
    [SerializeField] private GameObject[] colorButtons;

    //filter
    [SerializeField] private Material mainFilter;
    [SerializeField] private string affectedColor;

    void Start()
    {
        canvas = transform.parent;
        rt = GetComponent<RectTransform>();
        screenCenter = canvas.GetComponent<RectTransform>().sizeDelta / 2f;
        mat = GetComponent<Image>().material;
        slider = transform.GetChild(0).GetComponent<RectTransform>();
        sliderVal = slider.GetComponent<Slider>().value;
    }

    void Update()
    {
        SetAppearance();
        AllowColorSelection();
    }

    void SetAppearance()
    {
        Vector2 size = rt.sizeDelta;
        Vector2 anchor = 0.5f * (rt.anchorMax + rt.anchorMin);
        rt.anchorMax = anchor;
        rt.anchorMin = anchor;
        left = transform.position.x - anchor.x * rt.sizeDelta.x - screenCenter.x;
        right = transform.position.x + (1 - anchor.x) * rt.sizeDelta.x - screenCenter.x;
        bottom = transform.position.y - anchor.y * rt.sizeDelta.y - screenCenter.y;
        top = transform.position.y + (1 - anchor.y) * rt.sizeDelta.y - screenCenter.y;
        mat.SetFloat("_Left", left);
        mat.SetFloat("_Right", right);
        mat.SetFloat("_Top", top);
        mat.SetFloat("_Bottom", bottom);
        mat.SetFloat("_ThirdVal", sliderVal);
    }

    void AllowColorSelection()
    {
        Vector2? selectionPoint = GetSelectionPoint();

        if (!selectionPoint.HasValue)
            return;

        if (IsTouchedPointWithinBounds(selectionPoint.Value))
        {
            float x = (selectionPoint.Value.x - left) / (right - left);
            float y = (selectionPoint.Value.y - bottom) / (top - bottom);
            ChangeColor(x, y, sliderVal);
        }
        else
        {
            if (ShouldCloseUI(selectionPoint.Value))
            {
                ClosePicker();
            }
        }
    }

    Vector2? GetSelectionPoint()
    {
#if (UNITY_EDITOR || UNITY_STANDALONE) //for desktop

        if (Input.GetMouseButton(0))
        {
            return GetClickLocation();
        }
        else
        {
            return null;
        }

#elif (UNITY_IOS || UNITY_ANDROID) //for mobile

        if (Input.touchCount > 0)
        {
            return GetClickLocation();
        }
        else
        {
            return null;
        }

#else

        return null;

#endif
    }

    Vector2 GetClickLocation()
    {
#if (UNITY_EDITOR || UNITY_STANDALONE)

        Vector2 mousePos = Input.mousePosition;
        return new Vector2(mousePos.x - screenCenter.x, mousePos.y - screenCenter.y);

#elif (UNITY_IOS || UNITY_ANDROID)

        Vector2 touchPos = Input.touches[0].position;
        return new Vector2(touchPos.x - screenCenter.x, touchPos.y - screenCenter.y);

#endif
    }

    bool IsTouchedPointWithinBounds(Vector2 pos)
    {
        return left <= pos.x && pos.x <= right && bottom <= pos.y && pos.y <= top;
    }

    void ChangeColor(float x, float y, float z)
    {
        Color newColor = Color.HSVToRGB(x, z, y);
        mainFilter.SetColor(affectedColor, newColor);
    }

    bool ShouldCloseUI(Vector2 pos)
    {
        float sliderLeft = slider.transform.position.x - slider.sizeDelta.x / 2f - screenCenter.x;
        if (pos.x >= sliderLeft && bottom <= pos.y && pos.y <= top)
            return false;

#if (UNITY_EDITOR || UNITY_STANDALONE)

        return Input.GetMouseButtonDown(0);

#elif (UNITY_IOS || UNITY_ANDROID)

        return Input.touches[0].phase == TouchPhase.Began;

#else

        return true;

#endif
    }

    void ClosePicker()
    {
        foreach (GameObject obj in colorButtons)
        {
            obj.SetActive(true);
        }
        gameObject.SetActive(false);
    }
}
