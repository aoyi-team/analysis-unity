using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SkinCarouselManager : MonoBehaviour
{
    [Header("核心配置")]
    public int skinCount = 4; // 皮肤总数（1、2、3、4+）
    public Transform leftPos; // 左侧位置（1个固定位置）
    public Transform centerPos; // 中间顶层位置（1个固定位置）
    public Transform rightPosRoot; // 右侧位置根节点（子节点是右侧的多个位置，按顺序排列：RightPos_0、RightPos_1...）
    public GameObject skinCardPrefab; // 皮肤卡牌预制体

    [Header("皮肤数据")]
    public List<int> skinIds; // 皮肤编号列表（按顺序：1、2、3、4...）
    private List<Transform> rightPosList; // 右侧位置列表（按顺序排列：RightPos_0→最左，RightPos_1→靠右）
    private Dictionary<int, GameObject> skinCardDict; // 皮肤ID→卡牌对象

    // 当前状态
    private int currentTopSkinId; // 中间顶层皮肤ID
    private int leftSkinId; // 左侧皮肤ID
    private List<int> rightSkinIds; // 右侧皮肤ID列表（按顺序）

    private void Awake()
    {
        // 初始化右侧位置列表（按子节点名字排序：RightPos_0、RightPos_1...）
        rightPosList = rightPosRoot.GetComponentsInChildren<Transform>()
            .Where(t => t != rightPosRoot)
            .OrderBy(t => int.Parse(t.name.Split('_')[1]))
            .ToList();

        // 初始化皮肤ID列表（如果没手动赋值，自动生成1、2、3...skinCount）
        if (skinIds == null || skinIds.Count == 0)
        {
            skinIds = Enumerable.Range(1, skinCount).ToList();
        }

        skinCardDict = new Dictionary<int, GameObject>();
        // 初始化布局（按你的规则）
        InitLayout();
    }

    /// <summary>
    /// 按你的规则初始化：左=最后1张，中=第1张，右=剩余顺序排
    /// </summary>
    private void InitLayout()
    {
        // 中间顶层：第1张皮肤
        currentTopSkinId = skinIds[0];
        // 左侧：最后1张皮肤
        leftSkinId = skinIds.Last();
        // 右侧：剩余皮肤（从第2张到倒数第2张）按顺序排
        rightSkinIds = skinIds.Skip(1).Take(skinIds.Count - 2).ToList();

        // 生成所有卡牌并放到对应位置
        GenerateAllCards();
        // 更新卡牌显示位置
        UpdateCardPositions();
    }

    /// <summary>
    /// 生成所有皮肤卡牌（动态生成，数量=skinCount）
    /// </summary>
    private void GenerateAllCards()
    {
        // 先清空旧卡牌
        foreach (var card in skinCardDict.Values) Destroy(card);
        skinCardDict.Clear();

        // 生成每个皮肤的卡牌
        foreach (int skinId in skinIds)
        {
            GameObject card = Instantiate(skinCardPrefab, transform);
            card.name = $"SkinCard_{skinId}";
            skinCardDict.Add(skinId, card);

            // 给卡牌绑定点击事件（区分左/中/右位置的卡牌）
            Button btn = card.GetComponent<Button>();
            btn.onClick.AddListener(() => OnCardClick(skinId));

            // 自动加载皮肤图片+显示序号（复用之前的资源加载逻辑）
            /*SkinCard skinCard = card.GetComponent<SkinCard>();
            skinCard.skinId = skinId;
            skinCard.heroId = 101; // 替换为当前选中英雄ID
            skinCard.InitSkinDisplay();*/
        }
    }

    /// <summary>
    /// 更新所有卡牌的位置（左/中/右）
    /// </summary>
    private void UpdateCardPositions()
    {
        // 左侧卡牌：放到leftPos位置
        SetCardPosition(leftSkinId, leftPos);
        // 中间卡牌：放到centerPos位置（顶层，层级最高）
        SetCardPosition(currentTopSkinId, centerPos, sortingOrder: 10);
        // 右侧卡牌：按顺序放到rightPosList的位置（层级依次降低）
        for (int i = 0; i < rightSkinIds.Count; i++)
        {
            if (i < rightPosList.Count)
            {
                SetCardPosition(rightSkinIds[i], rightPosList[i], sortingOrder: 5 - i);
            }
        }
    }

    /// <summary>
    /// 辅助方法：设置卡牌的位置和层级
    /// </summary>
    private void SetCardPosition(int skinId, Transform targetPos, int sortingOrder = 1)
    {
        GameObject card = skinCardDict[skinId];
        RectTransform rt = card.GetComponent<RectTransform>();
        // 位置对齐目标节点
        rt.SetParent(targetPos);
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
        // 层级设置（中间最高，右侧依次降低）
        Canvas canvas = card.GetComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;
    }

    /// <summary>
    /// 卡牌点击事件（核心轮播逻辑）
    /// </summary>
    private void OnCardClick(int clickSkinId)
    {
        // 点击中间顶层：不处理
        if (clickSkinId == currentTopSkinId) return;

        // 点击左侧卡牌：触发逆时针轮播（按你的规则）
        if (clickSkinId == leftSkinId)
        {
            RotateCounterClockwise();
        }
        // 点击右侧卡牌：触发顺时针轮播（按你的规则）
        else if (rightSkinIds.Contains(clickSkinId))
        {
            RotateClockwise(clickSkinId);
        }
    }

    /// <summary>
    /// 逆时针轮播（点击左侧卡牌）
    /// 逻辑：左卡→中间顶层；原中间→右侧最左边；右侧所有左移；左侧补右侧最底层
    /// </summary>
    private void RotateCounterClockwise()
    {
        // 1. 记录旧状态
        int oldTop = currentTopSkinId;
        int oldLeft = leftSkinId;
        List<int> oldRight = new List<int>(rightSkinIds);

        // 2. 状态更新（核心逻辑）
        currentTopSkinId = oldLeft; // 左卡上到中间顶层
        leftSkinId = oldRight.LastOrDefault(); // 左侧空了，补右侧最底层（如果右侧有）
        rightSkinIds.RemoveAt(oldRight.Count - 1); // 右侧最底层移走
        rightSkinIds.Insert(0, oldTop); // 原中间顶层补到右侧最左边

        // 3. 特殊情况：如果右侧没有皮肤（比如3张皮肤：左=3，中=1，右=[2]）
        if (rightSkinIds.Count == 0)
        {
            leftSkinId = skinIds.Last(); // 左侧补所有皮肤的最后一张
        }

        // 4. 播放动画+更新位置
        PlayCardMoveAnim();
        UpdateCardPositions();
    }

    /// <summary>
    /// 顺时针轮播（点击右侧卡牌）
    /// 逻辑：右卡→中间顶层；原中间→左侧；左侧原皮肤→右侧最底层；右侧其他左移
    /// </summary>
    private void RotateClockwise(int clickRightSkinId)
    {
        // 1. 记录旧状态
        int oldTop = currentTopSkinId;
        int oldLeft = leftSkinId;
        List<int> oldRight = new List<int>(rightSkinIds);

        // 2. 找到点击的右侧卡牌在列表中的索引
        int clickIndex = oldRight.IndexOf(clickRightSkinId);

        // 3. 状态更新（核心逻辑）
        currentTopSkinId = clickRightSkinId; // 右卡上到中间顶层
        leftSkinId = oldTop; // 原中间顶层补到左侧
        oldRight.RemoveAt(clickIndex); // 移除点击的右卡
        oldRight.Add(oldLeft); // 原左侧皮肤补到右侧最底层
        rightSkinIds = oldRight; // 更新右侧列表

        // 4. 播放动画+更新位置
        PlayCardMoveAnim();
        UpdateCardPositions();
    }

    /// <summary>
    /// 卡牌移动动画（贴合你的“顶牌运动”直觉）
    /// </summary>
    private void PlayCardMoveAnim()
    {
        // 中间顶层卡牌：先放大再还原（强调顶层）
        GameObject topCard = skinCardDict[currentTopSkinId];
        topCard.transform.DOScale(1.1f, 0.2f).OnComplete(() => topCard.transform.DOScale(1f, 0.1f));

        // 左右移动的卡牌：平滑过渡
        foreach (var card in skinCardDict.Values)
        {
            if (card != topCard)
            {
                card.GetComponent<RectTransform>().DOAnchorPos(Vector2.zero, 0.3f).SetEase(Ease.OutQuad);
            }
        }
    }
}
