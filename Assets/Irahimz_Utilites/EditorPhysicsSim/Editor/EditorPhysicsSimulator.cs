using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Irahimz.EditorTools
{
    /// <summary> Runs 3D physics in edit mode on selected Rigidbodies, then bakes or resets. </summary>
    public class EditorPhysicsSimulator : EditorWindow
    {
        /// <summary> Original Rigidbody pose and kinematic flag, restored when the session ends. </summary>
        struct BodyState
        {
            public Rigidbody Body;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
            public bool IsKinematic;
        }

        enum SimState { Idle, Running, Stopped }

        const string UndoName = "Bake Physics Simulation";
        const int MaxStepsPerTick = 10;

        [SerializeField] bool includeChildren;
        [SerializeField] float timeScale = 1f;

        SimState state = SimState.Idle;
        readonly List<BodyState> targets = new();
        readonly List<BodyState> frozen = new();
        SimulationMode previousMode;
        double lastTickTime;
        float accumulator;
        float elapsed;
        int selectedBodyCount;

        /// <summary> Opens the Editor Physics Simulator window. </summary>
        [MenuItem("Tools/Irahimz/Editor Physics Simulator")]
        static void Open() => GetWindow<EditorPhysicsSimulator>("Physics Simulator");

        void OnEnable()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorSceneManager.sceneSaving += OnSceneSaving;
            RefreshSelectedCount();
        }

        void OnDisable()
        {
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorSceneManager.sceneSaving -= OnSceneSaving;
            if (state != SimState.Idle) ResetSession();
        }

        void OnSelectionChange()
        {
            RefreshSelectedCount();
            Repaint();
        }

        /// <summary> Caches the selection's Rigidbody count so OnGUI doesn't search every repaint. </summary>
        void RefreshSelectedCount() => selectedBodyCount = GetSelectedBodies().Count;

        void OnGUI()
        {
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(state != SimState.Idle))
            {
                EditorGUI.BeginChangeCheck();
                includeChildren = EditorGUILayout.Toggle("Include Children", includeChildren);
                if (EditorGUI.EndChangeCheck()) RefreshSelectedCount();
            }
            timeScale = EditorGUILayout.Slider("Time Scale", timeScale, 0.1f, 5f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            if (state == SimState.Idle)
            {
                EditorGUILayout.LabelField("Rigidbodies in Selection", selectedBodyCount.ToString());
            }
            else
            {
                EditorGUILayout.LabelField("State", state.ToString());
                EditorGUILayout.LabelField("Simulated Bodies", targets.Count.ToString());
                EditorGUILayout.LabelField("Elapsed", $"{elapsed:0.00}s");
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (state == SimState.Running)
                {
                    if (GUILayout.Button("Stop", GUILayout.Height(30))) state = SimState.Stopped;
                }
                else
                {
                    string label = state == SimState.Stopped ? "Resume" : "Simulate";
                    if (GUILayout.Button(label, GUILayout.Height(30))) StartOrResume();
                }

                using (new EditorGUI.DisabledScope(state == SimState.Idle))
                {
                    if (GUILayout.Button("Bake", GUILayout.Height(30))) Bake();
                    if (GUILayout.Button("Reset", GUILayout.Height(30))) ResetSession();
                }
            }

            if (state == SimState.Idle)
                EditorGUILayout.HelpBox("Select objects with a Rigidbody, then press Simulate.", MessageType.Info);
        }

        /// <summary> Starts a new session, or resumes a stopped one. </summary>
        void StartOrResume()
        {
            if (state == SimState.Idle && !BeginSession()) return;
            lastTickTime = EditorApplication.timeSinceStartup;
            state = SimState.Running;
        }

        /// <summary> Captures targets, freezes other Rigidbodies, and switches physics to script mode. </summary>
        bool BeginSession()
        {
            List<Rigidbody> bodies = GetSelectedBodies();
            if (bodies.Count == 0)
            {
                ShowNotification(new GUIContent("No active Rigidbodies selected"));
                return false;
            }

            var targetSet = new HashSet<Rigidbody>(bodies);
            foreach (Rigidbody rb in bodies)
                targets.Add(Capture(rb));

            // Non-selected bodies act as static obstacles during the sim.
            foreach (Rigidbody rb in GetSceneBodies())
            {
                if (targetSet.Contains(rb)) continue;
                frozen.Add(Capture(rb));
                rb.isKinematic = true;
            }

            foreach (BodyState t in targets)
            {
                t.Body.isKinematic = false;
                t.Body.WakeUp();
            }

            previousMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            Physics.SyncTransforms();

            accumulator = 0f;
            elapsed = 0f;
            return true;
        }

        /// <summary> Steps physics at fixed timestep while running. </summary>
        void Tick()
        {
            if (state != SimState.Running) return;

            double now = EditorApplication.timeSinceStartup;
            float step = Time.fixedDeltaTime;
            accumulator += (float)(now - lastTickTime) * timeScale;
            accumulator = Mathf.Min(accumulator, step * MaxStepsPerTick);
            lastTickTime = now;

            while (accumulator >= step)
            {
                Physics.Simulate(step);
                accumulator -= step;
                elapsed += step;
            }

            SceneView.RepaintAll();
            Repaint();
        }

        /// <summary> Keeps simulated poses as one undoable change, then ends the session. </summary>
        void Bake()
        {
            int count = targets.Count;
            var bakedPositions = new Vector3[count];
            var bakedRotations = new Quaternion[count];
            for (int i = 0; i < count; i++)
            {
                if (targets[i].Body == null) continue;
                Transform t = targets[i].Body.transform;
                bakedPositions[i] = t.localPosition;
                bakedRotations[i] = t.localRotation;
            }

            // Revert first so Undo records the original poses.
            RestorePoses();

            // Group bodies by parent: one undo record per parent, not per object.
            var byParent = new Dictionary<Transform, List<Object>>();
            var roots = new List<Object>();
            foreach (BodyState s in targets)
            {
                if (s.Body == null) continue;
                Transform t = s.Body.transform;
                if (t.parent == null)
                {
                    roots.Add(t);
                    continue;
                }
                if (!byParent.TryGetValue(t.parent, out List<Object> group))
                    byParent[t.parent] = group = new List<Object>();
                group.Add(t);
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);

            foreach (List<Object> group in byParent.Values)
                Undo.RecordObjects(group.ToArray(), UndoName);
            if (roots.Count > 0)
                Undo.RecordObjects(roots.ToArray(), UndoName);

            for (int i = 0; i < count; i++)
            {
                if (targets[i].Body == null) continue;
                targets[i].Body.transform.SetLocalPositionAndRotation(bakedPositions[i], bakedRotations[i]);
            }

            Undo.CollapseUndoOperations(undoGroup);
            EndSession();
        }

        /// <summary> Reverts all simulated bodies to their original poses. </summary>
        void ResetSession()
        {
            RestorePoses();
            EndSession();
        }

        /// <summary> Restores original local poses of all simulated bodies. </summary>
        void RestorePoses()
        {
            foreach (BodyState s in targets)
            {
                if (s.Body == null) continue;
                s.Body.transform.SetLocalPositionAndRotation(s.LocalPosition, s.LocalRotation);
            }
        }

        /// <summary> Clears velocities, restores kinematic flags and simulation mode. </summary>
        void EndSession()
        {
            foreach (BodyState s in targets)
            {
                if (s.Body == null) continue;
                s.Body.linearVelocity = Vector3.zero;
                s.Body.angularVelocity = Vector3.zero;
                s.Body.isKinematic = s.IsKinematic;
            }

            foreach (BodyState s in frozen)
                if (s.Body != null) s.Body.isKinematic = s.IsKinematic;

            Physics.simulationMode = previousMode;
            Physics.SyncTransforms();

            targets.Clear();
            frozen.Clear();
            state = SimState.Idle;
            SceneView.RepaintAll();
            Repaint();
        }

        /// <summary> Resets before entering play mode so temp changes don't leak. </summary>
        void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode && state != SimState.Idle)
                ResetSession();
        }

        /// <summary> Resets before saving so unbaked poses and flags aren't saved. </summary>
        void OnSceneSaving(Scene scene, string path)
        {
            if (state == SimState.Idle) return;
            Debug.LogWarning("[EditorPhysicsSimulator] Scene saved mid-simulation; simulation was reset. Bake first to keep poses.");
            ResetSession();
        }

        static BodyState Capture(Rigidbody rb)
        {
            Transform t = rb.transform;
            return new BodyState
            {
                Body = rb,
                LocalPosition = t.localPosition,
                LocalRotation = t.localRotation,
                IsKinematic = rb.isKinematic
            };
        }

        /// <summary> Active Rigidbodies on the selection, optionally including children. </summary>
        List<Rigidbody> GetSelectedBodies()
        {
            var result = new List<Rigidbody>();
            var seen = new HashSet<Rigidbody>();
            foreach (Transform t in Selection.GetTransforms(SelectionMode.Editable))
            {
                if (includeChildren)
                {
                    foreach (Rigidbody rb in t.GetComponentsInChildren<Rigidbody>())
                        if (seen.Add(rb)) result.Add(rb);
                }
                else if (t.TryGetComponent(out Rigidbody rb) && rb.gameObject.activeInHierarchy && seen.Add(rb))
                {
                    result.Add(rb);
                }
            }
            return result;
        }

        /// <summary> All active Rigidbodies in loaded scenes. </summary>
        static List<Rigidbody> GetSceneBodies()
        {
            var result = new List<Rigidbody>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (GameObject root in scene.GetRootGameObjects())
                    result.AddRange(root.GetComponentsInChildren<Rigidbody>());
            }
            return result;
        }
    }
}
