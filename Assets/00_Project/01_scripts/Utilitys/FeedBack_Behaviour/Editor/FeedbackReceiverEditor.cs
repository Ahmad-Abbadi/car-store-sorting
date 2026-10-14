using System;
using UnityEditor;
using UnityEngine;

namespace GameFeedback.EditorTools
{
    /// <summary> Custom inspector: add modules and edit their settings inline. </summary>
    [CustomEditor(typeof(FeedbackReceiver))]
    public class FeedbackReceiverEditor : Editor
    {
        SerializedProperty modules;

        /// <summary> Caches the serialized modules list. </summary>
        void OnEnable() => modules = serializedObject.FindProperty("modules");

        /// <summary> Draws the module list, add button and play controls. </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Feedback Modules", EditorStyles.boldLabel);

            if (modules.arraySize == 0)
                EditorGUILayout.HelpBox("No modules yet. Click 'Add Module' below.", MessageType.Info);

            for (int i = 0; i < modules.arraySize; i++)
                if (DrawModule(i)) break;

            EditorGUILayout.Space();
            if (GUILayout.Button("Add Module", GUILayout.Height(24)))
                ShowAddMenu();

            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                if (GUILayout.Button("▶ Play", GUILayout.Height(24)))
                    ((FeedbackReceiver)target).Play();
            }

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary> Draws one module; returns true when the list changed. </summary>
        bool DrawModule(int index)
        {
            bool structureChanged = false;
            SerializedProperty element = modules.GetArrayElementAtIndex(index);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            element.isExpanded = EditorGUILayout.Foldout(element.isExpanded, ModuleName(element), true);
            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(index == 0))
                if (GUILayout.Button("▲", GUILayout.Width(22)))
                {
                    modules.MoveArrayElement(index, index - 1);
                    structureChanged = true;
                }
            using (new EditorGUI.DisabledScope(index == modules.arraySize - 1))
                if (GUILayout.Button("▼", GUILayout.Width(22)))
                {
                    modules.MoveArrayElement(index, index + 1);
                    structureChanged = true;
                }
            if (GUILayout.Button("✕", GUILayout.Width(22)))
            {
                modules.DeleteArrayElementAtIndex(index);
                structureChanged = true;
            }
            EditorGUILayout.EndHorizontal();

            if (!structureChanged && element.isExpanded)
            {
                EditorGUI.indentLevel++;
                DrawChildren(element);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            return structureChanged;
        }

        /// <summary> Draws every serialized field of a managed reference. </summary>
        static void DrawChildren(SerializedProperty element)
        {
            SerializedProperty end = element.GetEndProperty();
            SerializedProperty child = element.Copy();
            bool enter = true;
            while (child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end))
            {
                enter = false;
                EditorGUILayout.PropertyField(child, true);
            }
        }

        /// <summary> Friendly type name for a managed reference element. </summary>
        static string ModuleName(SerializedProperty element)
        {
            string full = element.managedReferenceFullTypename;
            if (string.IsNullOrEmpty(full)) return "Missing Module";
            int start = Mathf.Max(full.LastIndexOf(' '), full.LastIndexOf('.')) + 1;
            return ObjectNames.NicifyVariableName(full.Substring(start));
        }

        /// <summary> Shows a menu of every concrete module type. </summary>
        void ShowAddMenu()
        {
            var menu = new GenericMenu();
            foreach (Type type in TypeCache.GetTypesDerivedFrom<FeedbackModule>())
            {
                if (type.IsAbstract || type.IsGenericType) continue;
                Type captured = type;
                menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(type.Name)), false, () => AddModule(captured));
            }
            menu.ShowAsContext();
        }

        /// <summary> Appends a new module instance of the given type. </summary>
        void AddModule(Type type)
        {
            serializedObject.Update();
            int index = modules.arraySize;
            modules.InsertArrayElementAtIndex(index);

            SerializedProperty element = modules.GetArrayElementAtIndex(index);
            element.managedReferenceValue = Activator.CreateInstance(type);
            element.isExpanded = true;

            SerializedProperty settings = element.FindPropertyRelative("settings");
            if (settings != null) settings.isExpanded = true;

            serializedObject.ApplyModifiedProperties();
        }
    }
}
