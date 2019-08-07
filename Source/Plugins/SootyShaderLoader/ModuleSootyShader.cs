
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace SootyShaderLoader
{
    public class ModuleSootyShader : PartModule
    {
        [KSPField]
        public string ShaderName;
        [KSPField]
        public string ObjectNames;
        [KSPField]
        public string TextureID;
        [KSPField]
        public string TextureName;
        [KSPField]
        public bool ShowTransitionEvent = true;
        [KSPField]
        public bool ShowTransitionAction = true;
        [KSPField]
        public float SootySpeed = 2;
        [KSPField]
        public bool OneShot = false;
        [KSPField]
        public bool EVAClean = false;
        [KSPField(isPersistant = true)]
        public bool SootyState = false;

        private float materialState = 0;

        public bool loaded;

        private Coroutine TransitionRoutine;

        private List<Transform> ModelObjects = new List<Transform>();

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            Events["ToggleSooty"].active = ShowTransitionEvent;
            Actions["ToggleSootyAction"].active = ShowTransitionAction;
            Events["CleanSoot"].active = EVAClean;

            if (!loaded)
                return;
            
            if (string.IsNullOrEmpty(ObjectNames))
                return;

            ModelObjects = new List<Transform>();

            var names = ObjectNames.Split(';');

            for (int i = names.Length - 1; i >= 0; i--)
            {
                string name = names[i];

                ModelObjects.AddRange(part.FindModelTransforms(name));
            }

            if (SootyState)
            {
                materialState = 1;

                SetMaterialState();
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            if (loaded)
                return;

            //print("[ShangshengOrbital] - Load shader from OnLoad");
            LoadShader();
        }

        private void LoadShader()
        {
            if (string.IsNullOrEmpty(ObjectNames))
                return;

            if (string.IsNullOrEmpty(ShaderName))
                return;

            Shader shade = GameDatabase.Instance.GetShader(ShaderName);

            if (shade == null)
            {
                print("[ShangshengOrbital] - Error loading shader from GameDatabase: " + ShaderName);
                shade = SootyShaderLoader.SootyShader;
            }

            if (shade == null)
                return;

            Texture tex = GameDatabase.Instance.GetTexture(TextureName, false);

            if (tex == null)
                return;

            var names = ObjectNames.Split(';');

            for (int i = names.Length - 1; i >= 0; i--)
            {
                string name = names[i];

                ModelObjects.AddRange(part.FindModelTransforms(name));
            }

            for (int i = ModelObjects.Count - 1; i >= 0; i--)
            {
                Renderer render = ModelObjects[i].gameObject.GetComponent<Renderer>();

                if (render == null)
                    continue;

                render.material.shader = shade;

                render.material.SetTexture(TextureID, tex);
            }

            loaded = true;

            print("[ShangshengOrbital] - Processed shader switch...");
        }

        [SuppressMessage("Style", "IDE0060", Justification = "KSPAction requires action param even if unused")]
        [KSPAction("Toggle Soot")]
        public void ToggleSootyAction(KSPActionParam action)
        {
            ToggleSooty();
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Toggle Soot", active = true)]
        public void ToggleSooty()
        {
            if (SootyState && OneShot && HighLogic.LoadedSceneIsFlight)
                return;

            SootyState = !SootyState;

            if (TransitionRoutine != null)
                StopCoroutine(TransitionRoutine);

            //print("[ShangshengOrbital] - Toggle soot state");

            TransitionRoutine = StartCoroutine(AnimateMaterial());
        }

        [KSPEvent(guiActive = false, guiActiveEditor = false, guiActiveUnfocused = true, guiName = "Clean Soot", active = false)]
        public void CleanSoot()
        {
            if (!SootyState)
                return;

            if (FlightGlobals.ActiveVessel != null && FlightGlobals.ActiveVessel.isEVA)
            {
                SootyState = false;

                if (TransitionRoutine != null)
                    StopCoroutine(TransitionRoutine);

                //print("[ShangshengOrbital] - Clean soot state");

                TransitionRoutine = StartCoroutine(AnimateMaterial());
            }
        }

        private IEnumerator AnimateMaterial()
        {
            if (SootyState)
            {
                while (materialState < 1)
                {
                    materialState = Mathf.Lerp(materialState, 1, TimeWarp.deltaTime * SootySpeed);
                    //print("[ShangshengOrbital] - Setting soot to: " + materialState.ToString());

                    if (materialState > 0.99f)
                        materialState = 1;

                    SetMaterialState();

                    yield return null;
                }                    
            }
            else
            {
                while (materialState > 0)
                {
                    materialState = Mathf.Lerp(materialState, 0, TimeWarp.deltaTime * SootySpeed);
                    //print("[ShangshengOrbital] - Setting soot to: " + materialState.ToString());

                    if (materialState < 0.001f)
                        materialState = 0;

                    SetMaterialState();

                    yield return null;
                }
            }

            TransitionRoutine = null;
        }

        private void SetMaterialState()
        {
            for (int i = ModelObjects.Count - 1; i >= 0; i--)
            {
                Renderer render = ModelObjects[i].gameObject.GetComponent<Renderer>();

                if (render == null)
                    continue;

                render.material.SetFloat("_State", materialState);
            }
        }
    }

}
