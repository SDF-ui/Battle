using System.Collections.Generic;
using System.Linq;

// 可扩展的搜索器模式
public class ItemSearcher
{
    private Dictionary<string, System.Func<Item, object, bool>> searchConditions 
        = new Dictionary<string, System.Func<Item, object, bool>>();
    
    public void AddCondition(string key, System.Func<Item, object, bool> condition)
    {
        searchConditions[key] = condition;
    }
    
    public List<Item> Search(Dictionary<string, object> conditions, IEnumerable<Item> items)
    {
        return items.Where(item =>
        {
            foreach (var cond in conditions)
            {
                if (searchConditions.ContainsKey(cond.Key) &&
                    !searchConditions[cond.Key](item, cond.Value))
                {
                    return false;
                }
            }
            return true;
        }).ToList();
    }
}