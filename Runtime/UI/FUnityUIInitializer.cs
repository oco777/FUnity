using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FUnity.Runtime.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public class FUnityUIInitializer : MonoBehaviour
    {
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("uiDocument")]
        private UIDocument m_UIDocument;
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("profile")]
        private UILoadProfile m_Profile;

        public UILoadProfile Profile => m_Profile;

        private void Awake()
        {
            if (!TryResolveDocument())
            {
                Debug.LogError("[FUnityUIInitializer] UIDocument component is missing.");
                return;
            }

            if (m_Profile != null)
            {
                ApplyProfile(m_Profile);
            }
            else
            {
                ApplyFallback();
            }
        }

        private bool TryResolveDocument()
        {
            if (m_UIDocument == null)
            {
                m_UIDocument = GetComponent<UIDocument>();
            }

            return m_UIDocument != null;
        }

        private void ApplyProfile(UILoadProfile loadProfile)
        {
            if (loadProfile.panelSettings != null)
            {
                m_UIDocument.panelSettings = loadProfile.panelSettings;
            }

            if (loadProfile.uxml != null)
            {
                m_UIDocument.visualTreeAsset = loadProfile.uxml;
            }

            var root = m_UIDocument.rootVisualElement;
            root.Clear();
            root.styleSheets.Clear();

            if (loadProfile.uxml != null)
            {
                loadProfile.uxml.CloneTree(root);
            }
            else if (m_UIDocument.visualTreeAsset != null)
            {
                m_UIDocument.visualTreeAsset.CloneTree(root);
            }

            AppendStyleSheets(root, loadProfile.uss);

            if (loadProfile.spawnFooni)
            {
                root.Add(new FooniElement());
            }

            if (loadProfile.spawnBlocks)
            {
                Debug.LogWarning("[FUnityUIInitializer] Block UI spawning is not implemented yet.");
            }

            Debug.Log("✅ FUnityUIInitializer: UI loaded via UILoadProfile.");
        }

        private static void AppendStyleSheets(VisualElement root, List<StyleSheet> sheets)
        {
            if (sheets == null)
            {
                return;
            }

            foreach (var sheet in sheets)
            {
                if (sheet != null)
                {
                    root.styleSheets.Add(sheet);
                }
            }
        }

        private void ApplyFallback()
        {
            var root = m_UIDocument.rootVisualElement;
            root.Clear();
            root.styleSheets.Clear();

            root.Add(new FooniElement());
            Debug.Log("🌈 FUnityUIInitializer: FooniElement spawned via fallback.");
        }

        private void OnValidate()
        {
            if (m_UIDocument == null)
            {
                m_UIDocument = GetComponent<UIDocument>();
            }
        }
    }
}
