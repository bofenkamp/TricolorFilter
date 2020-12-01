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
            {
                GetNewColor(markerRT.position - screenCenter);
                TrySnapSlider();
            }
        }
    }
    private float m_sliderVal;

    //transforms & GameObjects
    private Transform canvas;
    private RectTransform rt;
    private RectTransform slider;
    private Slider sliderFunct;
    [SerializeField] private GameObject[] colorButtons;
    [SerializeField] private RectTransform markerRT;
    [SerializeField] private GameObject[] otherMarkers;
    [SerializeField] private Slider[] otherSliders;
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
        sliderFunct = slider.GetComponent<Slider>();
        sliderVal = sliderFunct.value;
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
        if (!markerImg)
        {
            markerImg = markerRT.GetComponent<Image>();
        }
        SetMarkerPos(new Vector3(left + h * (right - left), bottom + l * (top - bottom), 0) + screenCenter);

        //adjust to snap
        h = (markerRT.position.x - screenCenter.x - left) / (right - left);
        s = sliderVal;
        l = (markerRT.position.y - screenCenter.y - bottom) / (top - bottom);
        Color newColor = Color.HSVToRGB(h, s, l);
        mainFilter.SetColor(affectedColor, newColor);
        markerImg.color = newColor;
    }

    void SetMarkerPos(Vector3 defaultPos) //automatically snaps if possible
    {
        if (CanSnapToOtherMarkers(defaultPos))
            return;
        else if (CanSnapToMidpointBetweenMarkers(defaultPos))
            return;
        else if (CanSnapToIntersectionsOfTrendlineAndHueLumLines(defaultPos))
            return;
        else if (CanSnapToLineBetweenMarkers(defaultPos))
            return;
        else if (CanSnapToHueLumIntersection(defaultPos))
            return;
        else if (CanSnapToComplimentaryHue(defaultPos))
            return;
        else if (CanSnapToComplimentaryLum(defaultPos))
            return;
        else
            markerRT.position = defaultPos;
    }

    bool CanSnapToOtherMarkers(Vector3 defaultPos)
    {
        List<Vector3> eligibleSnaps = new List<Vector3>();
        return GetBestPosInList(eligibleSnaps, defaultPos);
    }

    bool CanSnapToMidpointBetweenMarkers(Vector3 defaultPos)
    {
        List<Vector3> eligiblePoses = new List<Vector3>();
        for (int i = 0; i < otherMarkers.Length - 1; i++)
        {
            for (int j = i + 1; j < otherMarkers.Length; j++)
            {
                eligiblePoses.Add(0.5f * (otherMarkers[i].transform.position + otherMarkers[j].transform.position));
                //make otherMarkers[1] the midpoint
                eligiblePoses.Add((otherMarkers[1].transform.position - otherMarkers[0].transform.position) + otherMarkers[1].transform.position);
                //make otherMarkers[2] the midpoint
                eligiblePoses.Add((otherMarkers[0].transform.position - otherMarkers[1].transform.position) + otherMarkers[0].transform.position);
            }
        }

        return GetBestPosInList(eligiblePoses, defaultPos);
    }

    bool CanSnapToIntersectionsOfTrendlineAndHueLumLines(Vector3 defaultPos)
    {
        List<float> complimentaryHues = GetComplimentaryHues();
        List<float> complimentaryLums = GetComplimentaryLums();
        List<Vector3> pointsToTest = new List<Vector3>();
        (Vector3, Vector3) trendLine = (otherMarkers[0].transform.position, otherMarkers[1].transform.position);

        foreach (float hue in complimentaryHues)
        {
            Vector3? intersect = GetPointOnLineWithXValue(left + hue * (right - left) + screenCenter.x,
                trendLine.Item1, trendLine.Item2);
            if (intersect.HasValue && IsPointWithinBox(intersect.Value))
                pointsToTest.Add(intersect.Value);
        }
        foreach (float lum in complimentaryLums)
        {
            Vector3? intersect = GetPointOnLineWithYValue(bottom + lum * (top - bottom) + screenCenter.y,
                trendLine.Item1, trendLine.Item2);
            if (intersect.HasValue && IsPointWithinBox(intersect.Value))
                pointsToTest.Add(intersect.Value);
        }

        return GetBestPosInList(pointsToTest, defaultPos);
    }

    bool CanSnapToLineBetweenMarkers(Vector3 defaultPos)
    {
        //try to snap to position of line between markers
        List<(Vector3, Vector3)> eligibleLines = new List<(Vector3, Vector3)>();
        for (int i = 0; i < otherMarkers.Length - 1; i++)
        {
            for (int j = i + i; j < otherMarkers.Length; j++)
            {
                eligibleLines.Add((otherMarkers[i].transform.position, 
                    otherMarkers[j].transform.position));
            }
        }

        return GetBestLineInList(eligibleLines, defaultPos);
    }

    bool CanSnapToHueLumIntersection(Vector3 defaultPos)
    {
        List<float> complimentaryHues = GetComplimentaryHues();
        List<float> complimentaryLums = GetComplimentaryLums();
        List<Vector3> pointsToTest = new List<Vector3>();

        foreach (float hue in complimentaryHues)
        {
            float x = left + hue * (right - left) + screenCenter.x;
            foreach (float lum in complimentaryLums)
            {
                float y = bottom + lum * (top - bottom) + screenCenter.y;
                pointsToTest.Add(new Vector3(x, y, 0));
            }
        }

        Vector3? newPos;
        List<Vector3> eligiblePoses = new List<Vector3>();
        foreach (Vector3 point in pointsToTest)
        {
            newPos = TrySnapToPosition(point, defaultPos);
            if (newPos.HasValue)
            {
                eligiblePoses.Add(newPos.Value);
            }
        }

        if (eligiblePoses.Count > 0)
        {
            markerRT.position = GetClosestPosition(eligiblePoses, defaultPos);
            return true;
        }
        else
        {
            return false;
        }
    }

    bool CanSnapToComplimentaryHue(Vector3 defaultPos)
    {
        List<float> complimentaryHues = GetComplimentaryHues();
        List<(Vector3, Vector3)> eligibleLines = new List<(Vector3, Vector3)>();

        foreach (float hue in complimentaryHues)
        {
            float x = left + hue * (right - left) + screenCenter.x;
            Vector3 p1 = new Vector3(x, 0, 0);
            Vector3 p2 = new Vector3(x, 1, 0);
            eligibleLines.Add((p1, p2));
        }

        return GetBestLineInList(eligibleLines, defaultPos);
    }

    bool CanSnapToComplimentaryLum(Vector3 defaultPos)
    {
        List<float> complimentaryLums = GetComplimentaryLums();
        List<(Vector3, Vector3)> eligibleLines = new List<(Vector3, Vector3)>();

        foreach (float lum in complimentaryLums)
        {
            float y = bottom + lum * (top - bottom) + screenCenter.y;
            Vector3 p1 = new Vector3(0, y, 0);
            Vector3 p2 = new Vector3(1, y, 0);
            eligibleLines.Add((p1, p2));
        }

        return GetBestLineInList(eligibleLines, defaultPos);
    }

    bool GetBestPosInList(List<Vector3> positions, Vector3 defaultPos)
    {
        Vector3? newPos;
        List<Vector3> eligiblePoses = new List<Vector3>();

        foreach (Vector3 pos in positions)
        {
            if (!IsPointWithinBox(pos))
            {
                continue;
            }
            newPos = TrySnapToPosition(pos, defaultPos);
            if (newPos.HasValue)
            {
                eligiblePoses.Add(newPos.Value);
            }
        }

        if (eligiblePoses.Count > 0)
        {
            markerRT.position = GetClosestPosition(eligiblePoses, defaultPos);
            return true;
        }
        else
        {
            return false;
        }
    }

    bool GetBestLineInList(List<(Vector3, Vector3)> lines, Vector3 defaultPos)
    {
        (Vector3, Vector3)? newLine;
        List<(Vector3, Vector3)> eligibleLines = new List<(Vector3, Vector3)>();
        foreach ((Vector3, Vector3) line in lines)
        {
            newLine = TrySnapToLine(defaultPos, line.Item1, line.Item2);
            if (newLine.HasValue)
            {
                eligibleLines.Add(newLine.Value);
            }
        }
        if (eligibleLines.Count > 0)
        {
            (Vector3, Vector3) closestLine = GetClosestLine(eligibleLines, defaultPos);
            markerRT.position = GetClosestPointOnLine(defaultPos, closestLine.Item1, closestLine.Item2);
            return true;
        }
        else
        {
            return false;
        }
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
        if (positions.Count == 1)
            return positions[0];

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
            float num = Mathf.Abs((p2.y - p1.y) * p0.x - (p2.x - p1.x) * p0.y + p2.x * p1.y - p2.y * p1.x);
            float den = Mathf.Sqrt(Mathf.Pow(p2.y - p1.y, 2) + Mathf.Pow(p2.x - p1.x, 2));
            d = num / den;
        }
        return d;
    }

    (Vector3, Vector3) GetClosestLine(List<(Vector3, Vector3)> lines, Vector3 pos)
    {
        if (lines.Count == 1)
            return lines[0];

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
            else //vertical line
            {
                p = new Vector3(p1.x, p0.y, 0);
            }
        }
        else
        {
            if (p1.y == p2.y) //horizontal line
            {
                p = new Vector3(p0.x, p1.y, 0);
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
        Vector3? p;
        if (p0.x - screenCenter.x < left) //too far left
        {
            p = GetPointOnLineWithXValue(left + screenCenter.x + 1, p1, p2);
            if (p.HasValue && IsPointWithinBox(p.Value))
                return p.Value;
        }
        else if (p0.x - screenCenter.x > right) //too far right
        {
            p = GetPointOnLineWithXValue(right + screenCenter.x - 1, p1, p2);
            if (p.HasValue && IsPointWithinBox(p.Value))
                return p.Value;
        }
        if (p0.y - screenCenter.y < bottom) //too far down
        {
            p = GetPointOnLineWithYValue(bottom + screenCenter.y + 1, p1, p2);
            if (p.HasValue && IsPointWithinBox(p.Value))
                return p.Value;
        }
        else if (p0.y - screenCenter.y > top) //too far up
        { 
            p = GetPointOnLineWithYValue(top + screenCenter.y - 1, p1, p2);
            if (p.HasValue && IsPointWithinBox(p.Value))
                return p.Value;
        }
        return p0;
    }

    Vector3? GetPointOnLineWithXValue(float x, Vector3 p1, Vector3 p2)
    {
        if (p1.x == p2.x) //vertical, no solution or infinite solutions
            return null;
        else if (p1.y == p2.y) //horizontal
            return new Vector3(x, p1.y, 0);
        else
        {
            float m = (p2.y - p1.y) / (p2.x - p1.x);
            float b = p1.y - m * p1.x;
            float y = m * x + b;
            return new Vector3(x, y, 0);
        }
    }

    Vector3? GetPointOnLineWithYValue(float y, Vector3 p1, Vector3 p2)
    {
        if (p1.y == p2.y) //horizontal, no solution or infinite solutions
            return null;
        else if (p1.x == p2.x) //vertical
            return new Vector3(p1.x, y, 0);
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

    List<float> GetComplimentaryHues()
    {
        List<float> complimentaryHues = new List<float>();
        float[] existingHues = new float[2];

        for (int i = 0; i < existingHues.Length; i++)
        {
            Transform currMark = otherMarkers[i].transform;
            float hue = ((currMark.position.x - screenCenter.x) - left) / (right - left);
            existingHues[i] = hue;
        }

        float diff = GetHueDifference(existingHues[0], existingHues[1]);
        complimentaryHues.Add(existingHues[0]); //monochromatic

        if (diff == 0)
        {
            complimentaryHues.Add((existingHues[0] + 0.5f) % 1); //complimentary
            return complimentaryHues;
        }

        complimentaryHues.Add(existingHues[1]); //monochromatic
        complimentaryHues.Add(0.5f * (existingHues[0] + existingHues[1])); //analogous
        complimentaryHues.Add((complimentaryHues[2] + 0.5f) % 1); //analogous

        if (diff != 0.5f) //accented analogous
        {
            float max;
            float min;
            if (Mathf.Abs(existingHues[0] - existingHues[1]) < 0.5f) //shortest distance doesn't need to wrap around color wheel
            {
                max = Mathf.Max(existingHues[0], existingHues[1]);
                min = Mathf.Min(existingHues[0], existingHues[1]);
            }
            else //shortest distance requires passing around hue = 0 and hue = 1
            {
                max = Mathf.Min(existingHues[0], existingHues[1]);
                min = Mathf.Max(existingHues[0], existingHues[1]);
            }
            complimentaryHues.Add((max + diff) % 1);
            complimentaryHues.Add((min - diff) % 1);
        }

        return complimentaryHues;
    }

    float GetHueDifference(float h1, float h2)
    {
        float diff = Mathf.Abs(h1 - h2);
        if (diff > 0.5f)
            return (1 - diff) % 1;
        else
            return diff % 1;
    }

    List<float> GetComplimentaryLums()
    {
        List<float> complimentaryLums = new List<float>();
        float[] otherLums = new float[2];

        for (int i = 0; i < otherLums.Length; i++)
        {
            Transform currMark = otherMarkers[i].transform;
            float lum = ((currMark.position.y - screenCenter.y) - bottom) / (top - bottom);
            otherLums[i] = lum;
        }

        float diff = Mathf.Abs(otherLums[0] - otherLums[1]);
        complimentaryLums.Add(otherLums[0]);

        if (diff == 0)
        { 
            return complimentaryLums;
        }

        complimentaryLums.Add(otherLums[1]);
        complimentaryLums.Add(0.5f * (otherLums[0] + otherLums[1]));
        float highLum = Mathf.Max(otherLums[0], otherLums[1]) + diff;
        if (highLum <= 1)
            complimentaryLums.Add(highLum);
        float lowLum = Mathf.Min(otherLums[0], otherLums[1]) - diff;
        if (lowLum >= 0)
            complimentaryLums.Add(lowLum);

        return complimentaryLums;
    }

    List<float> GetComplimentarySats()
    {
        List<float> complimentarySats = new List<float>();
        float[] otherSats = new float[2];

        for (int i = 0; i < otherSats.Length; i++)
        {
            otherSats[i] = otherSliders[i].value;
        }

        float diff = Mathf.Abs(otherSats[0] - otherSats[1]);
        complimentarySats.Add(otherSats[0]);

        if (diff == 0)
        {
            return complimentarySats;
        }

        complimentarySats.Add(otherSats[1]);
        complimentarySats.Add(0.5f * (otherSats[0] + otherSats[1]));
        float highSat = Mathf.Max(otherSats[0], otherSats[1]) + diff;
        if (highSat <= 1)
            complimentarySats.Add(highSat);
        float lowSat = Mathf.Min(otherSats[0], otherSats[1]) - diff;
        if (lowSat >= 0)
            complimentarySats.Add(lowSat);

        return complimentarySats;
    }

    void TrySnapSlider()
    {
        float defaultSat = GetDefaultSat();
        List<float> complimentarySats = GetComplimentarySats();
        List<float> eligibleSats = new List<float>();

        foreach (float sat in complimentarySats)
        {
            if (Mathf.Abs(defaultSat - sat) <= snapDist / (right - left))
                eligibleSats.Add(sat);
        }

        if (eligibleSats.Count > 0)
        {
            if (eligibleSats.Count == 1)
                sliderFunct.value = eligibleSats[0];

            float closestSatVal = eligibleSats[0];
            float smallestDiff = Mathf.Abs(eligibleSats[0] - defaultSat);
            for (int i = 1; i < eligibleSats.Count; i++)
            {
                float sat = eligibleSats[i];
                float diff = Mathf.Abs(sat - defaultSat);
                if (diff < smallestDiff)
                {
                    closestSatVal = sat;
                    smallestDiff = diff;
                }
            }

            if (smallestDiff >= 0.00390625f) // 1/256, already at desired snap
                sliderFunct.value = closestSatVal;
        }
        else
        {
            sliderFunct.value = defaultSat;
        }
    }

    float GetDefaultSat()
    {
        float yVal;

#if (UNITY_STANDALONE || UNITY_EDITOR)

        yVal = Mathf.Clamp(((Input.mousePosition.y - screenCenter.y) - bottom) / (top - bottom), 0, 1);

#elif (UNITY_IOS || UNITY_ANDROID)

        yVal = Mathf.Clamp(((Input.touches[0].position.y - screenCenter.y) - bottom) / (top - bottom), 0, 1);

#endif

        return yVal;
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
}