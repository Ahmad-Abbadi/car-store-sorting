using UnityEditor;
using UnityEngine;

namespace Irahimz.EditorTools
{
    /// <summary> Shuffles children of selected objects by hierarchy order and/or local position. </summary>
    public class OrderShuffler : EditorWindow
    {
        const string OrderPrefKey = "Irahimz.OrderShuffler.ShuffleOrder";
        const string PositionPrefKey = "Irahimz.OrderShuffler.ShufflePositions";
        const string UndoName = "Shuffle Children";

        static bool ShuffleOrder
        {
            get => EditorPrefs.GetBool(OrderPrefKey, true);
            set => EditorPrefs.SetBool(OrderPrefKey, value);
        }

        static bool ShufflePositions
        {
            get => EditorPrefs.GetBool(PositionPrefKey, false);
            set => EditorPrefs.SetBool(PositionPrefKey, value);
        }

        /// <summary> Opens the Order Shuffler window. </summary>
        [MenuItem("Tools/Irahimz/Order Shuffler")]
        static void Open() => GetWindow<OrderShuffler>("Order Shuffler");

        static bool menuShufflePending;

        /// <summary> Hierarchy right-click entry; uses the window's saved toggles. </summary>
        [MenuItem("GameObject/Irahimz/Shuffle Children", false, 0)]
        static void ShuffleFromMenu()
        {
            // Unity may invoke once per selected object; coalesce into a single shuffle.
            if (menuShufflePending) return;
            menuShufflePending = true;
            EditorApplication.delayCall += () =>
            {
                menuShufflePending = false;
                ShuffleSelection();
            };
        }

        [MenuItem("GameObject/Irahimz/Shuffle Children", true)]
        static bool ValidateShuffleFromMenu() => HasShufflableSelection();

        void OnSelectionChange() => Repaint();

        void OnGUI()
        {
            EditorGUILayout.LabelField("Shuffle Options", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            bool order = EditorGUILayout.Toggle("Shuffle Order", ShuffleOrder);
            bool positions = EditorGUILayout.Toggle("Shuffle Positions", ShufflePositions);
            if (EditorGUI.EndChangeCheck())
            {
                ShuffleOrder = order;
                ShufflePositions = positions;
            }

            EditorGUILayout.Space();

            int selected = GetSelectedParents().Length;
            bool canShuffle = (order || positions) && HasShufflableSelection();
            using (new EditorGUI.DisabledScope(!canShuffle))
            {
                if (GUILayout.Button($"Shuffle Children ({selected} selected)", GUILayout.Height(30)))
                    ShuffleSelection();
            }

            if (!order && !positions)
                EditorGUILayout.HelpBox("Enable at least one shuffle option.", MessageType.Info);
        }

        /// <summary> Top-level selected objects only; selected descendants are ignored. </summary>
        static Transform[] GetSelectedParents() =>
            Selection.GetTransforms(SelectionMode.Editable | SelectionMode.TopLevel);

        /// <summary> True if any selected object has two or more children. </summary>
        static bool HasShufflableSelection()
        {
            foreach (Transform t in GetSelectedParents())
                if (t.childCount > 1) return true;
            return false;
        }

        /// <summary> Shuffles children of every selected object as a single undo step. </summary>
        static void ShuffleSelection()
        {
            bool order = ShuffleOrder;
            bool positions = ShufflePositions;
            if (!order && !positions)
            {
                Debug.LogWarning("[OrderShuffler] Enable Shuffle Order and/or Shuffle Positions first.");
                return;
            }

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);

            foreach (Transform parent in GetSelectedParents())
                ShuffleChildren(parent, order, positions);

            Undo.CollapseUndoOperations(group);
        }

        /// <summary> Shuffles one parent's direct children; both options share one permutation. </summary>
        public static void ShuffleChildren(Transform parent, bool order, bool positions)
        {
            int count = parent.childCount;
            if (count < 2) return;

            var children = new Transform[count];
            var localPositions = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                children[i] = parent.GetChild(i);
                localPositions[i] = children[i].localPosition;
            }

            // Fisher-Yates shuffle.
            var shuffled = (Transform[])children.Clone();
            for (int i = count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            if (positions)
            {
                Undo.RecordObjects(children, UndoName);
                for (int i = 0; i < count; i++)
                    shuffled[i].localPosition = localPositions[i];
            }

            if (order)
            {
                // One undo record for the whole order, not one per child.
                Undo.RegisterChildrenOrderUndo(parent, UndoName);
                for (int i = 0; i < count; i++)
                    shuffled[i].SetSiblingIndex(i);
            }
        }
    }
}
