using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Handbook : MonoBehaviour, IInitializable
{
    public Priority Priority { get; set; } = Priority.Low;

    public static Handbook instance;

    [SerializeField] GameObject HandbookObject;

    [SerializeField] GameObject Title;
    [SerializeField] GameObject TutorialReward;
    [SerializeField] GameObject Header;
    [SerializeField] GameObject Description;
    [SerializeField] GameObject ChapterInfo;
    [SerializeField] GameObject PointInfo;
    [SerializeField] GameObject Fade;

    [SerializeField] bool _switch;

    bool isWaiting;

    void Awake()
    {
        instance = this;
    }

    public void Initialize()
    {
        bool IS_COMPLETED = GameManager.instance.DATA.HANDBOOK_INFO.IS_COMPLETED;

        if (IS_COMPLETED)
        {
            return;
        }

        Set();

        Animate.Game.Move(HandbookObject, -400, "X", 1f, .5f);
    }

    public TutorialType GetCurrentQuestType()
    {
        HandbookInfo handbookInfo = GameManager.instance.DATA.HANDBOOK_INFO;

        if (handbookInfo.IS_COMPLETED)
        {
            return TutorialType.None;
        }

        HandbookData handbookData = GameManager.instance.HandbookData;

        Chapter chapter = handbookData.TUTORIAL.CHAPTERS[handbookInfo.Chapter];
        ChapterPoint point = chapter.Points[handbookInfo.Point];

        return point.Type;
    }

    public void Next()
    {
        HandbookData handbookData = GameManager.instance.HandbookData;
        HandbookInfo handbookInfo = GameManager.instance.DATA.HANDBOOK_INFO;

        Chapter currentChapter = handbookData.TUTORIAL.CHAPTERS[handbookInfo.Chapter];

        if (handbookInfo.Point + 1 > currentChapter.Points.Count - 1)
        {
            if (handbookInfo.Chapter + 1 > handbookData.TUTORIAL.CHAPTERS.Count - 1)
            {
                Complete();
                return;
            }

            handbookInfo.Chapter++;
            handbookInfo.Point = 0;

            Set();

            return;
        }

        handbookInfo.Point++;

        Set();
    }

    void Set()
    {
        SFX.Play(SFXCategory.Type);

        HandbookData handbookData = GameManager.instance.HandbookData;
        HandbookInfo handbookInfo = GameManager.instance.DATA.HANDBOOK_INFO;

        Chapter chapter = handbookData.TUTORIAL.CHAPTERS[handbookInfo.Chapter];
        ChapterPoint point = chapter.Points[handbookInfo.Point];

        int chapterCount = handbookData.TUTORIAL.CHAPTERS.Count;
        int pointCount = chapter.Points.Count;

        string tutorialReward = Utils.GetDigit(handbookData.TUTORIAL.REWARD);

        UI.TEXT.SET(handbookData.TUTORIAL.TITLE, Title);
        UI.TEXT.SET(tutorialReward, TutorialReward);
        UI.TEXT.SET(chapter.Title, Header);
        UI.TEXT.SET(point.Name, Description);
        UI.TEXT.SET($"{handbookInfo.Chapter + 1}/{chapterCount}", ChapterInfo);
        UI.TEXT.SET($"{handbookInfo.Point + 1}/{pointCount}", PointInfo);
    }

    void Complete()
    {
        GameManager.instance.DATA.HANDBOOK_INFO.IS_COMPLETED = true;
        FactoryData factory = Utils.GetFactoryData();
        factory.CASH += GameManager.instance.HandbookData.TUTORIAL.REWARD;
        Animate.Game.Move(HandbookObject, -390f, "X", 0f, .5f);
    }

    public void ClickToComplete()
    {
        TutorialType quest = GetCurrentQuestType();
        int typeIndex = GetTutorialChapterIndex(TutorialType.Complete);
        int currentIndex = GetTutorialChapterIndex(quest);

        if (typeIndex == currentIndex)
        {
            CheckQuest(TutorialType.Complete);
        }
    }

    public int GetTutorialChapterIndex(TutorialType type)
    {
        int index = 0;
        List<Chapter> chapters = GameManager.instance.HandbookData.TUTORIAL.CHAPTERS;

        Chapter chapter = chapters.Find((c) => c.Points.Any((p) => p.Type == type));
        int chapterIndex = chapters.FindIndex((c) => c.Title == chapter.Title);

        if (chapterIndex > 0)
        {
            index += chapters.Take(chapterIndex - 1).Select((c) => c.Points.Count()).Sum();
        }

        index += chapter.Points.FindIndex((p) => p.Type == type);

        return index;
    }

    IEnumerator NextTo(int amount = 1)
    {
        isWaiting = true;

        for (int i = 1; i <= amount; i++)
        {
            Animate.Game.Fade(Fade, 1f, 1f, AnimationController.Activate);
            yield return new WaitForSeconds(1f);
            yield return new WaitForSeconds(2f);
            Animate.Game.Fade(Fade, 0f, .5f, AnimationController.Deactivate);
            yield return new WaitForSeconds(.5f);
            SFX.Play(SFXCategory.Type);
            Next();
        }

        isWaiting = false;
    }

    IEnumerator HandleCheck(TutorialType sent)
    {
        while (isWaiting)
        {
            yield return null;
        }

        TutorialType current = GetCurrentQuestType();

        if (current == TutorialType.None)
        {
            yield break;
        }

        int sentIndex = GetTutorialChapterIndex(sent);
        int currentIndex = GetTutorialChapterIndex(current);

        if (sentIndex > currentIndex)
        {
            StartCoroutine(NextTo(sentIndex - currentIndex + 1));
            yield break;
        }

        FactoryData factory = Utils.GetFactoryData();

        List<TutorialType> noCheckNeeded = new()
        {
            TutorialType.Inspect_Market,
            TutorialType.Establish_Factory,
            TutorialType.Establish_RD_Center,
            TutorialType.Establish_Warehouse,
            TutorialType.Establish_Office,
            TutorialType.Select_Industry,
            TutorialType.Arrange_Product_Features,
            TutorialType.Arrange_Production_Quantity,
            TutorialType.Set_Product_Price,
            TutorialType.Review_Staff,
            TutorialType.Arrange_Storage,
            TutorialType.Complete_RD_Project,
            TutorialType.Complete
        };

        if (noCheckNeeded.Contains(current))
        {
            if (current == sent)
            {
                StartCoroutine(NextTo());
            }

            yield break;
        }

        if (sent == TutorialType.Handle_Machines)
        {
            int factoryMachines = factory.MACHINES.Find((m) => m.AUTOMATION == factory.MACHINE_AUTOMATION).COUNT;
            int neededMachines = Utils.GetNeededMachineCount(factory);

            if (factory.MACHINES.Any((m) => m.RENTED) || factoryMachines >= neededMachines)
            {
                StartCoroutine(NextTo());
                yield break;
            }
        }

        if (sent == TutorialType.Start_RD_Project)
        {
            if (factory.RESEARCH_DEVELOPMENTS.Count() > 0)
            {
                StartCoroutine(NextTo());
                yield break;
            }
        }

        if (sent == TutorialType.Agree_With_Suppliers)
        {
            StartCoroutine(NextTo());
            yield break;
        }

        if (sent == TutorialType.Launch_Advertising)
        {
            if (factory.ADVERTISING_BUDGET.Where((ad) => ad.BUDGET > 0).ToList().Count < 2)
            {
                yield break;
            }

            if (current == TutorialType.Launch_Advertising)
            {
                StartCoroutine(NextTo());
            }
        }
    }

    public void CheckQuest(TutorialType type)
    {
        StartCoroutine(HandleCheck(type));
    }
}