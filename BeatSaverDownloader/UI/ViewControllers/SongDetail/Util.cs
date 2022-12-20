using System;
using System.Linq;
using HMUI;
using IPA.Utilities;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace BeatSaverDownloader.UI.ViewControllers.SongDetail
{
    public class Util
    {
        public static TextSegmentedControl CreateTextSegmentedControl(RectTransform parent, Vector2 anchoredPosition, Vector2 sizeDelta, Action<int> onValueChanged = null, float fontSize = 4f, float padding = 8f)
        {
            var segmentedControl = new GameObject("CustomTextSegmentedControl", typeof(RectTransform)).AddComponent<TextSegmentedControl>();
            segmentedControl.gameObject.AddComponent<HorizontalLayoutGroup>();

            var segments = Resources.FindObjectsOfTypeAll<TextSegmentedControlCell>();

            segmentedControl.SetField("_singleCellPrefab", segments.First(x => x.name == "SingleHorizontalTextSegmentedControlCell"));
            segmentedControl.SetField("_firstCellPrefab", segments.First(x => x.name == "LeftHorizontalTextSegmentedControlCell"));
            segmentedControl.SetField("_middleCellPrefab", segments.Last(x => x.name == "MiddleHorizontalTextSegmentedControlCell"));
            segmentedControl.SetField("_lastCellPrefab", segments.Last(x => x.name == "RightHorizontalTextSegmentedControlCell"));

            segmentedControl.SetField("_container", Resources.FindObjectsOfTypeAll<TextSegmentedControl>().Select(x => x.GetField<DiContainer, TextSegmentedControl>("_container")).First(x => x != null));

            segmentedControl.transform.SetParent(parent, false);
            if (segmentedControl.transform is RectTransform rt)
            {
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = anchoredPosition;
                rt.sizeDelta = sizeDelta;
            }

            segmentedControl.SetField("_fontSize", fontSize);
            segmentedControl.SetField("_padding", padding);
            if (onValueChanged != null)
                segmentedControl.didSelectCellEvent += (sender, index) => { onValueChanged(index); };

            return segmentedControl;
        }

        public static IconSegmentedControl CreateIconSegmentedControl(RectTransform parent, Vector2 anchoredPosition, Vector2 sizeDelta, Action<int> onValueChanged = null)
        {
            var segmentedControl = new GameObject("CustomIconSegmentedControl", typeof(RectTransform)).AddComponent<IconSegmentedControl>();
            segmentedControl.gameObject.AddComponent<HorizontalLayoutGroup>();

            var segments = Resources.FindObjectsOfTypeAll<IconSegmentedControlCell>();

            segmentedControl.SetField("_singleCellPrefab", segments.First(x => x.name == "SingleHorizontalIconSegmentedControlCell"));
            segmentedControl.SetField("_firstCellPrefab", segments.First(x => x.name == "LeftHorizontalIconSegmentedControlCell"));
            segmentedControl.SetField("_middleCellPrefab", segments.First(x => x.name == "MiddleHorizontalIconSegmentedControlCell"));
            segmentedControl.SetField("_lastCellPrefab", segments.First(x => x.name == "RightHorizontalIconSegmentedControlCell"));

            segmentedControl.SetField("_container", Resources.FindObjectsOfTypeAll<IconSegmentedControl>().Select(x => x.GetField<DiContainer, IconSegmentedControl>("_container")).First(x => x != null));

            segmentedControl.transform.SetParent(parent, false);
            if (segmentedControl.transform is RectTransform rt)
            {
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = anchoredPosition;
                rt.sizeDelta = sizeDelta;
            }

            if (onValueChanged != null)
                segmentedControl.didSelectCellEvent += (sender, index) => { onValueChanged(index); };

            return segmentedControl;
        }
    }
}