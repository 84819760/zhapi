namespace ZhApi;
public class ChildNodeHelper
{
    public static IReadOnlyDictionary<string, ElementNode> GetMap(SummaryNode node)
    {
        var map = new Dictionary<string, ElementNode>();
        foreach (var item in node.GetTextNodeParentDistinct())
        {
            var path = GetPaths(node, item).Reverse().JoinString(".");
            if (path is { Length: > 0 })
                map[path] = item;
        }
        return map;
    }

    private static IEnumerable<string> GetPaths(SummaryNode root, ElementNode item)
    {
        ElementNode? node = item;
        while (true)
        {
            if (node is null || node.Parent is not ElementNode parent) yield break;
            if (node == root)
            {
                yield return node.Name;
                yield break;
            }
            else
            {
                var index = GetIndex(node);
                yield return $"{node.Name}{index}";
            }
            node = parent;
        }
    }

    private static int GetIndex(ElementNode node)
    {
        if (node.Parent is not ElementNode parent)
            return -1;

        var nodes = parent.Nodes;
        for (int i = 0; i < nodes.Count; i++)
            if (node == nodes[i])
                return i;

        return -1;
    }
}
