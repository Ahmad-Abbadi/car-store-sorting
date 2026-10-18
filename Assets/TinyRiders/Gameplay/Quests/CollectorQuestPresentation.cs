using System;
using System.Collections.Generic;
using System.Linq;

namespace TinyRiders.Quests
{
    public enum CollectorQuestTab { Journey, Challenges, Completed, AllQuests }

    /// <summary>Shared, testable recommendation and pagination rules. Never changes saved progress.</summary>
    public static class CollectorQuestPresentation
    {
        public const int PageSize=6;
        public static CollectorQuestView[] Select(IEnumerable<CollectorQuestView> source,CollectorQuestTab tab,CollectorQuestCategory? category=null)
        {
            var all=source.Where(q=>q.Available || q.Complete);
            if(tab==CollectorQuestTab.Challenges) all=all.Where(q=>q.Challenge && !q.Claimed);
            else if(tab==CollectorQuestTab.Completed) all=all.Where(q=>q.Claimed);
            else
            {
                all=all.Where(q=>!q.Challenge);
                if(tab==CollectorQuestTab.Journey)
                {
                    var eligible=all.ToArray();
                    all=eligible.Where(q=>q.Complete && !q.Claimed).Concat(eligible.Where(q=>!q.Complete)
                        .GroupBy(q=>q.Category).Select(g=>g.OrderByDescending(q=>q.Target>0 ? (double)q.Progress/q.Target : 0)
                            .ThenBy(q=>q.AuthoredOrder).ThenBy(q=>q.Id,StringComparer.Ordinal).First()));
                }
            }
            if(category.HasValue) all=all.Where(q=>q.Category==category.Value);
            return all.OrderByDescending(q=>q.Complete && !q.Claimed).ThenBy(q=>q.AuthoredOrder).ThenBy(q=>q.Id,StringComparer.Ordinal).ToArray();
        }
        public static int PageCount(int count)=>Math.Max(1,(count+PageSize-1)/PageSize);
        public static CollectorQuestView[] Page(IReadOnlyList<CollectorQuestView> items,int page)=>
            items.Skip(Math.Clamp(page,0,PageCount(items.Count)-1)*PageSize).Take(PageSize).ToArray();
    }
}
