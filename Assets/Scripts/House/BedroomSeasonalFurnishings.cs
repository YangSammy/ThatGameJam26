using System.Collections;
using UnityEngine;

namespace MemoryWhitebox
{
    // The house owns the day. Only the displayed quilt and curtains change.
    public sealed class BedroomSeasonalFurnishings : MonoBehaviour
    {
        [SerializeField] GameObject spring, summer, autumn, winter;
        WBMain house;

        void OnEnable() => StartCoroutine(BindToHouse());

        IEnumerator BindToHouse()
        {
            while (WBMain.Instance == null || !WBMain.Instance.HouseReady)
                yield return null;
            house = WBMain.Instance;
            house.DayChanged += Display;
            Display(house.CurrentDay);
        }

        void OnDisable()
        {
            StopAllCoroutines();
            if (house != null) house.DayChanged -= Display;
            house = null;
        }

        void Display(StoryDay day)
        {
            spring.SetActive(day == StoryDay.SpringTape || day == StoryDay.SpringDeparture);
            summer.SetActive(day == StoryDay.SummerPhoto);
            autumn.SetActive(day == StoryDay.AutumnWriting);
            winter.SetActive(day == StoryDay.WinterPainting);
        }
    }
}
