using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public enum BankPanelType
{
    Debt,
    Loan
}

public class BankPanel : MonoBehaviour
{
    public Bank bank;

    [SerializeField] Transform BankContent;
    [SerializeField] Transform InfoContent;

    [SerializeField] List<GameObject> Categories;
    [SerializeField] List<GameObject> Titles;

    [SerializeField] List<GameObject> Panels;

    List<Bank> banks;

    bool IS_INITIALIZED;

    List<Variable_FLOAT_LAST> infos;

    List<string> categories;
    List<string> titles;

    void Awake()
    {
        categories = new(){
            "Debt",
            "Loan"
        };

        titles = new(){
            "Terms",
            "Loans",
            "Loan Information"
        };

        infos = new()
        {
            new(){
                NAME = "Total Debt",
                VALUE = 0,
            },
            new(){
                NAME = "Total Principal",
                VALUE = 0,
            },
            new(){
                NAME = "Total Debt Paid",
                VALUE = 0,
            },
        };
    }

    void Start()
    {
        for (int i = 0; i < Categories.Count; i++)
        {
            int index = i;

            UI.TEXT.SET(categories[i], Categories[i].transform.Find("Text").gameObject);

            Categories[i].GetComponent<Button>().onClick.AddListener(() =>
            {
                Handler(index);
            });
        }

        for (int i = 0; i < Titles.Count; i++)
        {
            UI.TEXT.SET(titles[i], Titles[i]);
        }

        for (int i = 0; i < Panels.Count; i++)
        {
            GameObject panel = Panels[i];
            panel.GetComponent<IBankPanel>().BankPanel = this;
        }

        OnStart(false);

        IS_INITIALIZED = true;
    }

    void OnEnable()
    {
        if (IS_INITIALIZED)
        {
            OnStart();
        }
    }

    public void OnChange()
    {
        SetInfos();
    }

    void OnStart(bool playSfx = true)
    {
        banks = GameManager.instance.DATA.FINANCE.BANKS.OrderBy((b) => b.INTEREST).ToList();
        bank = banks[0];

        CustomFunctions.instance.SetTransformItemsOpacity(BankContent, 0, 1f, .5f);

        Handler(0, playSfx);

        SetBanks();

        SetInfos();
    }

    void Handler(int index, bool play = true)
    {
        CustomFunctions.instance.SetPanel(
            index,
            Panels,
            Categories,
            (go) =>
            {
                go.SetActive(true);
                go.GetComponent<IBankPanel>().OnStart();
            },
            (go) =>
            {
                go.SetActive(false);
            },
            play
        );
    }

    void SetBanks()
    {
        banks = GameManager.instance.DATA.FINANCE.BANKS.OrderBy((b) => b.INTEREST).ToList();

        for (int i = 0; i < BankContent.childCount; i++)
        {
            GameObject bankObject = BankContent.GetChild(i).gameObject;

            UI.TEXT.SET(banks[i].NAME, bankObject.transform.Find("Header/Name").gameObject);
            UI.TEXT.SET($"Interest {banks[i].INTEREST}%", bankObject.transform.Find("Interest").gameObject);
            UI.IMAGE.SET.WITH_PATH($"Media/Game/LeftPanel/Bank/{banks[i].ICON}", bankObject.transform.Find("Image/Logo").gameObject);

            int index = i;

            bankObject.GetComponent<Button>().onClick.AddListener(() =>
            {
                SFX.Play(SFXCategory.Category);

                bank = banks[index];

                CustomFunctions.instance.SetTransformItemsOpacity(BankContent, index, 1f, .5f);
                SetInfos();

                Panels.Find((p) => p.activeSelf).GetComponent<IBankPanel>().OnStart();
            }
            );
        }
    }

    void SetInfos()
    {
        FactoryData factory = Utils.GetFactoryData();

        banks = GameManager.instance.DATA.FINANCE.BANKS.OrderBy((b) => b.INTEREST).ToList();

        int totalDebt = factory.LOANS.Select((l) => l.AMOUNT).Sum();
        int totalPrincipal = factory.LOANS.Select((l) => l.PRINCIPAL).Sum();
        int totalDebtPaid = factory.LOANS.Select((l) => l.PAID).Sum();

        infos = new()
        {
            new(){
                NAME = "Total Debt",
                VALUE = totalDebt,
                LAST_VALUE = infos.Find((i) => i.NAME == "Total Debt").VALUE
            },
            new(){
                NAME = "Total Principal",
                VALUE = totalPrincipal,
                LAST_VALUE = infos.Find((i) => i.NAME == "Total Principal").VALUE
            },
            new(){
                NAME = "Total Debt Paid",
                VALUE = totalDebtPaid,
                LAST_VALUE = infos.Find((i) => i.NAME == "Total Debt Paid").VALUE
            },
            new(){
                NAME = "Credit Rating",
            },
        };

        for (int i = 0; i < InfoContent.childCount; i++)
        {
            GameObject InfoItem = InfoContent.GetChild(i).gameObject;

            UI.TEXT.SET(infos[i].NAME, InfoItem.transform.Find("Name").gameObject);

            if (infos[i].NAME != "Credit Rating")
            {
                CustomFunctions.instance.CountText(InfoItem.transform.Find("Value").gameObject, (int)infos[i].VALUE, (int)infos[i].LAST_VALUE, 60, 1, "$");
            }
            else
            {
                int ratingValue = factory.CREDIT_RATINGS.Find((b) => b.NAME == bank.NAME).VALUE;

                UI.TEXT.SET(Utils.PaintCreditRating(ratingValue), InfoItem.transform.Find("Value").gameObject);
            }
        }
    }
}