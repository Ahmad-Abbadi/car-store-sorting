using System;
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
            Refresh();
        }
        void Move(Action move) { move(); afterMove?.Invoke(); }
        string ThemeName(int i)
        {
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
                var button = new Button(() => Move(() => navigation.ShowTheme(index))) { text = name };
                button.EnableInClassList("selected", i == navigation.CurrentTheme); list.Add(button);
            }
            if (list.childCount == 0) list.Add(new Label("No matching themes"));
        }
        void Refresh()
        {
            root.Q<Label>("currentTheme").text = ThemeName(navigation.CurrentTheme);
            RefreshThemes();
            var grid = root.Q("cars"); grid.Clear();
            var cars = navigation.Cars(navigation.CurrentTheme);
            for (int i = 0; i < cars.Length; i++)
            {
                int index = i;
                var identity = cars[i].GetComponent<ThemedCarIdentity>();
                grid.Add(new Button(() => Move(() => navigation.ShowCar(index)))
                { text = "C" + (i + 1).ToString("D3"), tooltip = identity.collectibleId });
            }
        }
        public void Dispose()
        {
            navigation.NavigationChanged -= Refresh;
            navigation.PointerOverNavigation = false; navigation.EditingSearch = false;
        }
    }
}
