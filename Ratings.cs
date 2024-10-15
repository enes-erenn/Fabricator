using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public enum SortingCategory
{
    Order,
    Company,
    Rating,
    Cash,
    DebtRatio,
    Sales
}

public enum SortingWay
{
    Descending,
    Ascending,
    None
}

public class RatingKeySelector
{
    public Func<ListRating, IComparable> SELECTOR;
    public SortingWay SortingWay;
}

public class Ratings : MonoBehaviour, IInitializable
{
    public Priority Priority { get; set; } = Priority.Low;

    [SerializeField] GameObject Prefab;
    [SerializeField] GameObject PlayerRating;

    [SerializeField] Transform Titles;
    [SerializeField] Transform Content;

    List<ListRating> ratings;
    List<RatingKeySelector> KeySelectors;

    void Awake()
    {
        KeySelectors = new()
        {
           new(){SELECTOR = (f) => f.ORDER, SortingWay= SortingWay.None},
           new(){SELECTOR = (f) => f.COMPANY, SortingWay= SortingWay.None},
           new(){SELECTOR = (f) => f.RATING, SortingWay= SortingWay.None},
           new(){SELECTOR = (f) => f.CASH, SortingWay= SortingWay.None},
           new(){SELECTOR = (f) => f.DEBT_RATIO, SortingWay= SortingWay.None},
           new(){SELECTOR = (f) => f.SALES, SortingWay= SortingWay.None},
        };
    }

    void Start()
    {
        List<string> titles = new(){
            null,
            "Company",
            "Rating",
            "Cash",
            "Debt Ratio",
            "Last Sales",
        };

        for (int i = 0; i < Titles.childCount; i++)
        {
            string name = titles[i];
            Button btn = Titles.GetChild(i).GetComponent<Button>();

            int index = i;

            if (name != null)
            {
                UI.TEXT.SET(name, Titles.GetChild(i).GetChild(0).gameObject);
            }

            btn.onClick.AddListener(() => HandleSort(KeySelectors[index]));
        }
    }

    public void Initialize()
    {
        for (int i = 0; i < GameManager.instance.DATA.RATINGS.Count; i++)
        {
            Instantiate(Prefab, Content);
        }

        SetPlayerRating();
    }

    void OnEnable()
    {
        CameraSystem.instance.CAN_MOVE = false;

        ratings = GameManager.instance.DATA.RATINGS;
        HandleSort(KeySelectors[0], false);
    }

    void HandleSort(RatingKeySelector keySelector, bool PlaySFX = true)
    {
        if (PlaySFX)
        {
            SFX.Play(SFXCategory.Switch);
        }

        int index = KeySelectors.FindIndex((s) => s.SELECTOR == keySelector.SELECTOR);

        if (index == 0)
        {
            if (keySelector.SortingWay == SortingWay.Ascending)
            {
                ratings = ratings.OrderByDescending(keySelector.SELECTOR).ToList();
                keySelector.SortingWay = SortingWay.Descending;
            }
            else
            {
                ratings = ratings.OrderBy(keySelector.SELECTOR).ToList();
                keySelector.SortingWay = SortingWay.Ascending;
            }
        }
        else
        {

            if (keySelector.SortingWay == SortingWay.None)
            {
                ratings = ratings.OrderByDescending(keySelector.SELECTOR).ToList();
                keySelector.SortingWay = SortingWay.Descending;
            }
            else if (keySelector.SortingWay == SortingWay.Descending)
            {
                ratings = ratings.OrderBy(keySelector.SELECTOR).ToList();
                keySelector.SortingWay = SortingWay.Ascending;
            }
            else
            {
                ratings = ratings.OrderBy(KeySelectors[0].SELECTOR).ToList();
                keySelector.SortingWay = SortingWay.None;
            }
        }

        Set(keySelector.SortingWay, index);
    }

    public void SetPlayerRating()
    {
        FactoryData factory = Utils.GetFactoryData();

        int order = GameManager.instance.DATA.RATINGS.Find((r) => r.FACTORY == factory.ID).ORDER;
        UI.TEXT.SET(order.ToString(), PlayerRating);
    }

    public void Set(SortingWay SortingWay = SortingWay.None, int keyIndex = 0)
    {
        FactoryData playerFactory = Utils.GetFactoryData();

        for (int i = 0; i < ratings.Count(); i++)
        {
            ListRating rating = ratings[i];
            GameObject obj = Content.GetChild(i).gameObject;

            string path;

            if (rating.ORDER == 1)
            {
                path = "Media/Game/LeftPanel/Ratings/first";
            }
            else if (rating.ORDER > 1 && rating.ORDER <= 5)
            {
                path = "Media/Game/LeftPanel/Ratings/second";
            }
            else
            {
                path = "Media/Game/LeftPanel/Ratings/third";
            }

            UI.IMAGE.SET.WITH_PATH(path, obj.transform.Find("List/Icon").GetChild(0).gameObject);
            UI.TEXT.SET(rating.ORDER.ToString(), obj.transform.Find("List/Icon/Order/Number").gameObject);
            UI.TEXT.SET(rating.COMPANY, obj.transform.Find("List/Company").GetChild(0).gameObject);
            UI.TEXT.SET(Utils.GetCreditRatingName(rating.RATING), obj.transform.Find("List/Rating").GetChild(0).gameObject);
            UI.TEXT.SET(Utils.GetDigit(rating.CASH, "$"), obj.transform.Find("List/Cash").GetChild(0).gameObject);
            UI.TEXT.SET($"{rating.DEBT_RATIO:0.00}%", obj.transform.Find("List/Debt Ratio").GetChild(0).gameObject);
            UI.TEXT.SET(Utils.GetDigit(rating.SALES), obj.transform.Find("List/LastMonthSales").GetChild(0).gameObject);

            if (rating.FACTORY == playerFactory.ID)
            {
                UI.IMAGE.OPACITY(obj.transform.Find("Image/Image").gameObject, 0.15f);
            }
            else
            {
                UI.IMAGE.OPACITY(obj.transform.Find("Image/Image").gameObject, 0.05f);
            }

            ChangeImage(obj.transform.Find("List").gameObject, i, keyIndex, SortingWay);

            if (!obj.activeSelf)
            {
                obj.SetActive(true);
            }
        }
    }

    void ChangeImage(GameObject item, int itemIndex, int index, SortingWay SortingWay)
    {
        for (int i = 0; i < item.transform.childCount; i++)
        {
            GameObject go = item.transform.GetChild(i).gameObject;

            if (i == index && SortingWay != SortingWay.None)
            {
                if (index == 0)
                {
                    continue;
                }

                if (SortingWay == SortingWay.Ascending)
                {
                    UI.IMAGE.OPACITY(go, (.2f + (float)(itemIndex + 1f) / ratings.Count()) * .5f);
                }
                else
                {
                    UI.IMAGE.OPACITY(go, .5f - (.2f + (float)(itemIndex + 1f) / ratings.Count()) * .5f);
                }
            }
            else
            {
                UI.IMAGE.OPACITY(go, 0f);
            }
        }
    }

    void OnDisable()
    {
        CameraSystem.instance.CAN_MOVE = true;
    }
}