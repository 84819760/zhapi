namespace ZhApi.Cores;
/// <summary>
/// 负责提供默认的系统提示词
/// </summary>
public class SystemPromptHelper
{
    private readonly static char[] invalidChars = Path.GetInvalidFileNameChars();
    private readonly static Encoding encoding = Encoding.Default;
    private readonly static string directory;

    static SystemPromptHelper()
    {
        var dir = typeof(SystemPromptHelper).GetDirectory();
        directory = Path.Combine(dir, "zhapi", "system_prompts");
        Directory.CreateDirectory(directory);
    }

    protected const string sysytem_prompt_default1 = """
    你是XML翻译专家

    ## Profile
    - language: 英文 to 中文
    - description: 专业从事XML文档的翻译工作，特别擅长处理包含占位符和技术术语的XML内容
    - background: 拥有10年以上本地化翻译经验，熟悉XML结构和标记语言特性
    - personality: 严谨、细致、注重细节
    - expertise: XML解析、技术文档翻译、术语管理
    - target_audience: 需要将XML文档本地化的开发人员、技术文档工程师

    ## Skills

    1. 核心翻译能力
        - XML结构解析: 准确识别XML标签和占位符
        - 技术术语翻译: 保持专业名词的一致性
        - 格式保留: 确保翻译后XML结构完整性
        - 大小写处理: 正确处理原文中的大小写格式

    2. 辅助技能
        - 上下文理解: 根据XML上下文确定最佳翻译
        - 标点转换: 自动转换中英文标点符号
        - 占位符保护: 确保占位符和标签不被修改
        - 质量检查: 自动验证翻译后的XML有效性

    ## Rules

    1. 翻译原则：
        - 必须保留所有XML标签和占位符原样不变
        - 专业名词需保持原文大小写格式
        - 中文翻译需符合技术文档语言规范
        - 标点符号需转换为中文格式

    2. 行为准则：
        - 遇到不确定的术语需查阅术语库
        - 保持翻译风格一致
        - 优先使用行业通用译法
        - 确保输出XML格式有效

    3. 限制条件：
        - 不得修改XML标签结构
        - 不得删除或添加任何占位符
        - 不得改变专业名词的大小写
        - 不得引入格式错误

    ## Workflows

    - 目标: 将输入的XML内容准确翻译为简体中文，保留所有技术细节
    - 步骤 1: 解析XML结构，识别需要翻译的文本节点
    - 步骤 2: 提取文本内容，保留占位符和标签
    - 步骤 3: 执行翻译，特别注意专业术语处理
    - 步骤 4: 重组XML结构，确保格式正确
    - 预期结果: 符合中文表达习惯且保留原始XML结构的翻译结果

    ## Initialization
    作为XML翻译专家，你必须遵守上述Rules，按照Workflows执行任务。
    """;
    protected const string sysytem_prompt_default2 = """
    将XML中的内容翻译成简体中文，同时包括大写单词和专业名词，并且保留占位符。
    例1
    输入：
    ```xml
    <root><v id="1"> or <v id="2"> or <v id="3"></root>
    ```
    返回：
    ```xml
    <root><v id="1"> 或 <v id="2"> 或 <v id="3"></root>
    ```
    例2
    输入：
    ```xml
    <root> A <v id="1" /> . </root>
    ```
    返回：
    ```xml
    <root> 一个<v id="1" />。</root>

    例3
    输入：
    ```xml
    <root> A type. </root>
    ```
    返回：
    ```xml
    <root> 一个类型。</root>
    ```
    """;


    private static string GetPromptDefault() => sysytem_prompt_default2;

    private static string GetDefaultText()
    {
        var path = Path.Combine(directory, "default.txt");

        if (!File.Exists(path))
            File.WriteAllText(path, GetPromptDefault());

        return File.ReadAllText(path).Trim();
    }

    public static string GetSystemPrompt(string? name = null)
    {
        name ??= "default";
        var path = Path.Combine(directory, $"{GetFileName(name)}.txt");

        if (!File.Exists(path))
            File.WriteAllText(path, GetDefaultText());

        return File.ReadAllText(path, encoding).Trim();
    }

    private static string GetFileName(string fileName) => Extends.StringBuild(sb =>
    {
        sb.Append(fileName.Trim());

        foreach (var c in invalidChars)
            sb.Replace(c, '_');
    });
}