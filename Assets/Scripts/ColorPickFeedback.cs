using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColorPickFeedback : MonoBehaviour
{
    private bool initialized = false;

    //positioning/color of this object
    [SerializeField] private Vector3 screenCenter;
    [SerializeField] private float left;
    [SerializeField] private float right;
    [SerializeField] private float top;
    [SerializeField] private float bottom;
    private Material mat;
    public float sliderVal {
        get
        {
            return m_sliderVal;
        }
        set
        {
            m_sliderVal = value;
            SetAppearance();
            if (gameObject.activeSelf)
                GetNewColor(markerRT.position - screenCenter);
        }
    }
    private float m_sliderVal;

    //transforms & GameObjects
    private Transform canvas;
    private RectTransform rt;
    private RectTransform slider;
    [SerializeField] private GameObject[] colorButtons;
    [SerializeField] private RectTransform markerRT;
    [SerializeField] private GameObject[] otherMarkers;
    private Image markerImg;

    //filter
    [SerializeField] private Material mainFilter;
    public string affectedColor;
    private float x;
    private float y;

    //color selection related
    private bool adjustingSlider = false;
    [SerializeField] private float snapDist;

    private void Start()
    {
        if (!initialized)
            Initialize();
    }

    void Initialize()
    {
        if (initialized)
            return;

        initialized = true;
        canvas = GameObject.Find("Canvas").transform;
        rt = GetComponent<RectTransform>();
        screenCenter = canvas.GetComponent<RectTransform>().sizeDelta / 2f;
        mat = GetComponent<Image>().material;
        slider = transform.GetChild(0).GetComponent<RectTransform>();
        sliderVal = slider.GetComponent<Slider>().value;
        markerImg = markerRT.GetComponent<Image>();
        snapDist *= rt.sizeDelta.x;
    }

    void Update()
    {
        SetAppearance();
        AllowColorSelection();
    }

    void SetAppearance()
    {
        if (!initialized)
            Initialize();
        Vector2 size = rt.sizeDelta;
        Vector2 anchor = 0.5f * (rt.anchorMax + rt.anchorMin);
        rt.anchorMax = anchor;
        rt.anchorMin = anchor;
        left = transform.position.x - anchor.x * size.x - screenCenter.x;
        right = transform.position.x + (1 - anchor.x) * size.x - screenCenter.x;
        bottom = transform.position.y - anchor.y * size.y - screenCenter.y;
        top = transform.position.y + (1 - anchor.y) * size.y - screenCenter.y;
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

        if ((IsTouchedPointWithinBounds(selectionPoint.Value) && !IsClickContinuous()) 
            || (!adjustingSlider && IsClickContinuous()))
        {
            adjustingSlider = false;
            GetNewColor(selectionPoint.Value);
        }
        else
        {
            if (ShouldCloseUI(selectionPoint.Value))
            {
                ClosePicker();
            }
            else
            {
                adjustingSlider = true;
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

    /// <summary>
    /// checks if the current click/touch has been happening for more than one frame
    /// </summary>
    bool IsClickContinuous()
    {
#if (UNITY_EDITOR || UNITY_STANDALONE)

        return (Input.GetMouseButton(0) && !Input.GetMouseButtonDown(0));

#elif (UNITY_IOS || UNITY_ANDROID)

        return Input.touches[0].phase == TouchPhase.Moved || Input.touches[0].phase == TouchPhase.Stationary;

#endif
    }

    void GetNewColor(Vector2 selectionPoint)
    {
        x = Mathf.Clamp((selectionPoint.x - left) / (right - left), 0, 1);
        y = Mathf.Clamp((selectionPoint.y - bottom) / (top - bottom), 0, 1);
        ChangeColor(x, sliderVal, y);
    }

    void ChangeColor(float h, float s, float l)
    {
        Color newColor = Color.HSVToRGB(h, s, l);
        mainFilter.SetColor(affectedColor, newColor);
        if (!markerImg)
        {
            markerImg = markerRT.GetComponent<Image>();
        }
        markerImg.color = newColor;
        SetMarkerPos(new Vector3(left + h * (right - left), bottom + l * (top - bottom), 0) + screenCenter);
    }

    void SetMarkerPos(Vector3 defaultPos)
    {
        Vector3? newPos;

        //try to snap to position of other marker
        List<Vector3> eligibleSnaps = new List<Vector3>();
        foreach (GameObject obj in otherMarkers)
        {
            newPos = TrySnapToPosition(obj.transform.position, defaultPos);
            if (newPos.HasValue)
                eligibleSnaps.Add(newPos.Value);
        }

        if (eligibleSnaps.Count > 1)
        {
            markerRT.position = GetClosestPosition(eligibleSnaps, defaultPos);
            return;
        }
        else if (eligibleSnaps.Count == 1)
        {
            markerRT.position = eligibleSnaps[0];
            return;
        }

        //try to snap to position of line between markers
        List<(Vector3, Vector3)> eligibleLines = new List<(Vector3, Vector3)>();
        (Vector3, Vector3)? newLine;
        for (int i = 0; i < otherMarkers.Length - 1; i++)
        {
            for (int j = i + i; j < otherMarkers.Length; j++)
            {
                newLine = TrySnapToLine(defaultPos, 
                    otherMarkers[i].transform.position, otherMarkers[j].transform.position);
                if (newLine.HasValue)
                {
                    eligibleLines.Add(newLine.Value);
                }
            }
        }

        if (eligibleLines.Count > 0)
        {
            (Vector3, Vector3) closestLine;
            if (eligibleLines.Count > 1)
            {
                closestLine = GetClosestLine(eligibleLines, defaultPos);
            }
            else
            {
                closestLine = eligibleLines[0];
            }
            markerRT.position = GetClosestPointOnLine(defaultPos, closestLine.Item1, closestLine.Item2);
            return;
        }

        markerRT.position = defaultPos;
    }

    Vector3? TrySnapToPosition(Vector3 target, Vector3 curr)
    {
        if (Vector3.Distance(target, curr) <= snapDist)
            return target;
        else
            return null;
    }

    Vector3 GetClosestPosition(List<Vector3> positions, Vector3 pos)
    {
        Vector3 closestPos = positions[0];
        float smallestDist = Vector3.Distance(positions[0], pos);
        for (int i = 1; i < positions.Count; i++)
        {
            float dist = Vector3.Distance(positions[i], pos);
            if (dist < smallestDist)
            {
                closestPos = positions[i];
                smallestDist = dist;
            }
        }

        return closestPos;
    }

    (Vector3, Vector3)? TrySnapToLine(Vector3 p0, Vector3 p1, Vector3 p2)
    {
        Vector3 closestPointOnLine = GetClosestPointOnLine(p0, p1, p2);
        float dist = Vector2.Distance(closestPointOnLine, p0);
        if (dist <= snapDist)
            return (p1, p2);
        else
            return null;
    }

    float GetDistFromLine(Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float d;
        if (p1.x == p2.x && p1.y == p2.y)
        {
            d = Vector2.Distance(new Vector2(p0.x, p0.y), new Vector2(p1.x, p1.y));
        }
        else if (p1.x == p2.x)
        {
            d = Mathf.Abs(p0.x - p1.x);
        }
        else if (p1.y == p2.y)
        {
            d = Mathf.Abs(p0.y - p1.y);
        }
        else
        {
            //Debug.Log($"GETTING LINE DIST: p0 = {p0}, line = {p1} -> {p2}");
            float num = Mathf.Abs((p2.y - p1.y) * p0.x - (p2.x - p1.x) * p0.y + p2.x * p1.y - p2.y * p1.x);
            float den = Mathf.Sqrt(Mathf.Pow(p2.y - p1.y, 2) + Mathf.Pow(p2.x - p1.x, 2));
            //Debug.Log($"d = {num / den}");
            d = num / den;
        }
        Debug.Log(p0 - screenCenter);
        return d;
    }

    (Vector3, Vector3) GetClosestLine(List<(Vector3, Vector3)> lines, Vector3 pos)
    {
        (Vector3, Vector3) closestLine = lines[0];
        float closestDist = GetDistFromLine(pos, closestLine.Item1, closestLine.Item2);
        for (int i = 1; i < lines.Count; i++)
        {
            float dist = GetDistFromLine(pos, lines[i].Item1, lines[i].Item2);
            if (dist < closestDist)
            {
                closestLine = lines[i];
                closestDist = dist; 
            }
        }
        return closestLine;
    }

    Vector3 GetClosestPointOnLine(Vector3 p0, Vector3 p1, Vector3 p2)
    {
        Vector3 p;
        if (p1.x == p2.x)
        {
            if (p1.y == p2.y) //p1 & p2 identical
            {
                p = p1;
            }
            else //horizontal line
            {
                p = new Vector3(p0.x, p1.y, 0);
            }
        }
        else
        {
            if (p1.y == p2.y) //vertical line
            {
                p = new Vector3(p1.x, p0.y, 0);
            }
            else //diagonal line
            {
                float m = (p2.y - p1.y) / (p2.x - p1.x);
                float b = p1.y - m * p1.x;
                float n = -1 / m;
                float c = p0.y - n * p0.x;
                float x = (c - b) / (m - n);
                float y = m * x + b;
                p = new Vector3(x, y, 0);
            }
        }
        if (IsPointWithinBox(p))
            return p;
        else
            return GetClosestPointWithinBox(p, p1, p2);
    }

    Vector3 GetClosestPointWithinBox(Vector3 p0, Vector3 p1, Vector3 p2)
    {
        Vector3 p;
        Debug.Log($"{p0} not within box");
        if (p0.x - screenCenter.x < left) //too far left
        {
            Debug.Log("point too far left");
            p = GetPointOnLineWithXValue(left + screenCenter.x + 1, p1, p2);
            Debug.Log($"readjusted to {p}");
            if (IsPointWithinBox(p))
                return p;
        }
        else if (p0.x - screenCenter.x > right) //too far right
        {
            Debug.Log("point too far right");
            p = GetPointOnLineWithXValue(right + screenCenter.x - 1, p1, p2);
            Debug.Log($"readjusted to {p}");
            if (IsPointWithinBox(p))
                return p;
        }
        if (p0.y - screenCenter.y < bottom) //too far down
        {
            Debug.Log("point too far down");
            p = GetPointOnLineWithYValue(bottom + screenCenter.y + 1, p1, p2);
            Debug.Log($"readjusted to {p}");
            if (IsPointWithinBox(p))
                return p;
        }
        else if (p0.y - screenCenter.y > top) //too far up
        { 
            Debug.Log("point too far up");
            p = GetPointOnLineWithYValue(top + screenCenter.y - 1, p1, p2);
            Debug.Log($"readjusted to {p}");
            if (IsPointWithinBox(p))
                return p;
        }
        if(!IsPointWithinBox(p0))
        {
            Debug.Log($"Point {p0} still not in box");
        }
        return p0;
    }

    Vector3 GetPointOnLineWithXValue(float x, Vector3 p1, Vector3 p2)
    {
        float m = (p2.y - p1.y) / (p2.x - p1.x);
        float b = p1.y - m * p1.x;
        float y = m * x + b;
        return new Vector3(x, y, 0);
    }

    Vector3 GetPointOnLineWithYValue(float y, Vector3 p1, Vector3 p2)
    {
        float m = (p2.y - p1.y) / (p2.x - p1.x);
        float b = p1.y - m * p1.x;
        float x = (y - b) / m;
        return new Vector3(x, y, 0);
    }

    bool IsPointWithinBox(Vector3 p) //takes in world coordinates, not shader coordinates
    {
        p -= screenCenter;
        return left <= p.x && p.x <= right && bottom <= p.y && p.y <= top;
    }

    bool ShouldCloseUI(Vector2 pos)
    {
        float sliderRight = slider.transform.position.x + slider.sizeDelta.x / 2f - screenCenter.x;
        if (pos.x <= sliderRight && bottom <= pos.y && pos.y <= top)
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
        foreach (GameObject obj in otherMarkers)
        {
            obj.SetActive(false);
        }
        markerRT.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        PlayerPrefs.SetFloat('r' + affectedColor, mainFilter.GetColor(affectedColor).r);
        PlayerPrefs.SetFloat('g' + affectedColor, mainFilter.GetColor(affectedColor).g);
        PlayerPrefs.SetFloat('b' + affectedColor, mainFilter.GetColor(affectedColor).b);
    }
}
