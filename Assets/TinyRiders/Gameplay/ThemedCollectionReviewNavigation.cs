using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyRiders
{
    public sealed class ThemedCollectionReviewNavigation : MonoBehaviour
    {
        public ThemedCollectionReviewCamera navigation;
        CollectionReviewPanel panel;
        void OnEnable()
        {
            if (navigation == null) return;
            panel = new CollectionReviewPanel(GetComponent<UIDocument>().rootVisualElement, navigation, null);
        }
        void OnDisable() { panel?.Dispose(); panel = null; }
    }

    public sealed class CollectionReviewPanel : IDisposable
    {
        readonly VisualElement root;
        readonly ThemedCollectionReviewCamera navigation;
        readonly Action afterMove;
        readonly TextField search;
        public CollectionReviewPanel(VisualElement root, ThemedCollectionReviewCamera navigation, Action afterMove)
        {
            this.root = root; this.navigation = navigation; this.afterMove = afterMove;
            search = root.Q<TextField>("themeSearch");
            search.RegisterValueChangedCallback(_ => RefreshThemes());
            search.RegisterCallback<FocusInEvent>(_ => navigation.EditingSearch = true);
            search.RegisterCallback<FocusOutEvent>(_ => navigation.EditingSearch = false);
            var panel = root.Q("navigationPanel");
            panel.RegisterCallback<PointerEnterEvent>(_ => navigation.PointerOverNavigation = true);
            panel.RegisterCallback<PointerLeaveEvent>(_ => navigation.PointerOverNavigation = false);
            root.Q<Button>("previous").clicked += () => Move(() => navigation.ShowTheme(navigation.CurrentTheme - 1));
            root.Q<Button>("next").clicked += () => Move(() => navigation.ShowTheme(navigation.CurrentTheme + 1));
            root.Q<Button>("frameTheme").clicked += () => Move(() => navigation.ShowTheme(navigation.CurrentTheme));
            root.Q<Button>("overview").clicked += () => Move(navigation.ShowOverview);
            navigation.NavigationChanged += Refresh;
            Localizer.LanguageChanged += Refresh;
            Refresh();
        }
        void Move(Action move) { move(); afterMove?.Invoke(); }
        string ThemeName(int i)
        {
            var identity = navigation.themeCenters[i].parent.GetComponentInChildren<ThemedCarIdentity>(true);
            if (identity != null) return Localizer.Get("theme." + identity.themeId + ".name");
            string name = navigation.themeCenters[i].parent.name;
            int start = name.IndexOf(" - ", StringComparison.Ordinal);
            int end = name.IndexOf(" [", StringComparison.Ordinal);
            return start >= 0 && end > start ? name.Substring(start + 3, end - start - 3) : name;
        }
        void RefreshThemes()
        {
            var list = root.Q<ScrollView>("themes"); list.Clear();
            for (int i = 0; i < navigation.themeCenters.Length; i++)
            {
                int index = i; string name = ThemeName(i);
                if (name.IndexOf(search.value ?? "", StringComparison.OrdinalIgnoreCase) < 0) continue;
                var button = new Button(() => Move(() => navigation.ShowTheme(index))) { text = Display(name) };
                button.EnableInClassList("selected", i == navigation.CurrentTheme); list.Add(button);
            }
            if (list.childCount == 0) list.Add(new Label(Display(Localizer.Get("review.empty"))));
        }
        void Refresh()
        {
            var data = Localizer.Data;
            root.style.unityFont = Localizer.IsRtl ? data.arabicSource : data.latinSource;
            root.style.unityFontDefinition = new StyleFontDefinition(StyleKeyword.Null);
            root.style.unityTextAlign = Localizer.IsRtl ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            int total = Enumerable.Range(0, navigation.themeCenters.Length).Sum(i => navigation.Cars(i).Length);
            root.Q<Label>("reviewTitle").text = Display(Localizer.Get("review.title"));
            root.Q<Label>("reviewCaption").text = Display(Localizer.Format("review.caption", navigation.themeCenters.Length, total));
            root.Q<Label>("reviewControls").text = Display(Localizer.Get("review.controls"));
            search.label = Display(Localizer.Get("review.search"));
            root.Q<Button>("previous").text = Display(Localizer.Get("review.previous"));
            root.Q<Button>("next").text = Display(Localizer.Get("review.next"));
            root.Q<Button>("frameTheme").text = Display(Localizer.Get("review.frame"));
            root.Q<Button>("overview").text = Display(Localizer.Format("review.overview", total));
            if (navigation.themeCenters.Length == 0) return;
            root.Q<Label>("currentTheme").text = Display(ThemeName(navigation.CurrentTheme));
            RefreshThemes();
            var grid = root.Q("cars"); grid.Clear();
            var cars = navigation.Cars(navigation.CurrentTheme);
            for (int i = 0; i < cars.Length; i++)
            {
                int index = i;
                var identity = cars[i].GetComponent<ThemedCarIdentity>();
                grid.Add(new Button(() => Move(() => navigation.ShowCar(index)))
                { text = identity.baseCarId, tooltip = Display(Localizer.Get("item." + identity.collectibleId + ".name")) });
            }
        }
        public void Dispose()
        {
            navigation.NavigationChanged -= Refresh;
            Localizer.LanguageChanged -= Refresh;
            navigation.PointerOverNavigation = false; navigation.EditingSearch = false;
        }
        static string Display(string text) => Localizer.IsRtl && ArabicText.NeedsShaping(text)
            ? string.Join("\n", text.Split('\n').Select(ArabicText.ToVisual)) : text;
    }
}
