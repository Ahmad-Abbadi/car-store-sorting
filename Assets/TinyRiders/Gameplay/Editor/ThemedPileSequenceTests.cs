using System;
using System.Linq;
using NUnit.Framework;
using TinyRiders;

public class ThemedPileSequenceTests
{
    static ThemedCarCatalog.Entry[] Collection() => Enumerable.Range(0,40).SelectMany(theme =>
        Enumerable.Range(1,15).Select(car=>new ThemedCarCatalog.Entry
        {themeId="TH_"+theme.ToString("D2"),baseCarId="C"+car.ToString("D3"),collectibleId="TR-C"+car.ToString("D3")+"-TH_"+theme.ToString("D2")+"-V01"})).ToArray();

    [Test] public void AllDesignsAppearBeforeAnySecondCopy()
    {
        var entries=Collection();var ids=ThemedPileSequence.InstanceIds(entries,2,73481).ToArray();
        Assert.AreEqual(1200,ids.Length);Assert.AreEqual(1200,ids.Distinct().Count());
        Assert.True(ids.Take(600).All(id=>id.EndsWith(":0001",StringComparison.Ordinal)));
        CollectionAssert.AreEquivalent(entries.Select(e=>e.collectibleId),ids.Take(600).Select(id=>id.Split(':')[1]));
    }
    [Test] public void InitialPileContainsEveryThemeAndNoRepeatedDesigns()
    {
        var designs=ThemedPileSequence.Designs(Collection(),73481).Take(108).ToArray();
        Assert.AreEqual(108,designs.Select(e=>e.collectibleId).Distinct().Count());
        Assert.AreEqual(40,designs.Select(e=>e.themeId).Distinct().Count());
        Assert.True(designs.GroupBy(e=>e.themeId).All(g=>g.Count()==2||g.Count()==3));
        Assert.AreEqual(15,designs.Select(e=>e.baseCarId).Distinct().Count());
    }
    [Test] public void CatalogOrderDoesNotAffectSequenceAndSeedChangesIt()
    {
        var entries=Collection();
        CollectionAssert.AreEqual(ThemedPileSequence.InstanceIds(entries,2,73481),ThemedPileSequence.InstanceIds(entries.Reverse(),2,73481));
        Assert.False(ThemedPileSequence.InstanceIds(entries,2,73481).SequenceEqual(ThemedPileSequence.InstanceIds(entries,2,73482)));
        Assert.IsEmpty(ThemedPileSequence.InstanceIds(Array.Empty<ThemedCarCatalog.Entry>(),2,1));
    }
}
