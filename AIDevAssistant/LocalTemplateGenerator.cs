using UnityEngine;

public static class LocalTemplateGenerator
{
    public static string Generate(string userRequirement)
    {
        string templateType = DetectTemplateType(userRequirement);

        switch (templateType)
        {
            case "Bag":
                return BuildBagPanelTemplate(userRequirement);
            case "Shop":
                return BuildShopPanelTemplate(userRequirement);
            case "Task":
                return BuildTaskPanelTemplate(userRequirement);
            default:
                return BuildDefaultPanelTemplate(userRequirement);
        }
    }

    private static string DetectTemplateType(string userRequirement)
    {
        if (string.IsNullOrEmpty(userRequirement))
        {
            return "Default";
        }

        if (userRequirement.Contains("背包"))
        {
            return "Bag";
        }

        if (userRequirement.Contains("商店"))
        {
            return "Shop";
        }

        if (userRequirement.Contains("任务"))
        {
            return "Task";
        }

        return "Default";
    }

    private static string BuildBagPanelTemplate(string userRequirement)
    {
        return
$@"// 本地模板生成结果：背包面板
// 原始需求：{userRequirement}

using UnityEngine;
using UnityEngine.UI;

public class BagPanel : MonoBehaviour
{{
    [Header(""UI References"")]
    public Button closeButton;
    public Transform itemListRoot;

    private void Start()
    {{
        BindEvents();
        Init();
    }}

    private void BindEvents()
    {{
        if (closeButton != null)
        {{
            closeButton.onClick.AddListener(OnClickClose);
        }}
    }}

    public void Init()
    {{
        Debug.Log(""BagPanel Init"");
        RefreshView();
    }}

    public void RefreshView()
    {{
        Debug.Log(""Refresh bag item list"");
        // TODO: 根据背包数据刷新物品列表
    }}

    public void OnClickClose()
    {{
        gameObject.SetActive(false);
    }}
}}";
    }

    private static string BuildShopPanelTemplate(string userRequirement)
    {
        return
$@"// 本地模板生成结果：商店面板
// 原始需求：{userRequirement}

using UnityEngine;
using UnityEngine.UI;

public class ShopPanel : MonoBehaviour
{{
    [Header(""UI References"")]
    public Button closeButton;
    public Transform goodsListRoot;
    public Button refreshButton;

    private void Start()
    {{
        BindEvents();
        Init();
    }}

    private void BindEvents()
    {{
        if (closeButton != null)
        {{
            closeButton.onClick.AddListener(OnClickClose);
        }}

        if (refreshButton != null)
        {{
            refreshButton.onClick.AddListener(RefreshView);
        }}
    }}

    public void Init()
    {{
        Debug.Log(""ShopPanel Init"");
        RefreshView();
    }}

    public void RefreshView()
    {{
        Debug.Log(""Refresh goods list"");
        // TODO: 刷新商品列表
    }}

    public void OnClickClose()
    {{
        gameObject.SetActive(false);
    }}
}}";
    }

    private static string BuildTaskPanelTemplate(string userRequirement)
    {
        return
$@"// 本地模板生成结果：任务面板
// 原始需求：{userRequirement}

using UnityEngine;
using UnityEngine.UI;

public class TaskPanel : MonoBehaviour
{{
    [Header(""UI References"")]
    public Button closeButton;
    public Transform taskListRoot;

    private void Start()
    {{
        BindEvents();
        Init();
    }}

    private void BindEvents()
    {{
        if (closeButton != null)
        {{
            closeButton.onClick.AddListener(OnClickClose);
        }}
    }}

    public void Init()
    {{
        Debug.Log(""TaskPanel Init"");
        RefreshView();
    }}

    public void RefreshView()
    {{
        Debug.Log(""Refresh task list"");
        // TODO: 刷新任务列表
    }}

    public void OnClickClose()
    {{
        gameObject.SetActive(false);
    }}
}}";
    }

    private static string BuildDefaultPanelTemplate(string userRequirement)
    {
        return
$@"// 本地模板生成结果：通用面板
// 原始需求：{userRequirement}

using UnityEngine;

public class DemoPanel : MonoBehaviour
{{
    public void Init()
    {{
        Debug.Log(""DemoPanel Init"");
    }}

    public void RefreshView()
    {{
        Debug.Log(""Refresh View"");
    }}

    public void OnClickClose()
    {{
        gameObject.SetActive(false);
    }}
}}";
    }
}