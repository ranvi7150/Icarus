using System;
using UnityEngine;

namespace Icarus.Gameplay.Interaction
{
    public class Switch : MonoBehaviour, IInteractable, IInteractionPromptTarget
    {
        [Serializable]
        private struct ActivationTarget
        {
            public MonoBehaviour targetObject;
        }

        [Header("Switch Targets")]
        [SerializeField] private ActivationTarget[] activationTargets;
        [SerializeField] private GameObject interactionPrompt;

        private IActivatable[] _activatables;

        private void Awake()
        {
            if (interactionPrompt == null)
            {
                Debug.LogError("Switch requires an Interaction Prompt reference.", this);
                enabled = false;
                return;
            }

            if (!TryBuildActivatables())
            {
                enabled = false;
                return;
            }

            SetInteractionPromptVisible(false);
        }

        public void Interact()
        {
            for (int i = 0; i < _activatables.Length; i++)
            {
                _activatables[i].Activate();
            }
        }

        public void SetInteractionPromptVisible(bool isVisible)
        {
            interactionPrompt.SetActive(isVisible);
        }
        

        // Fail fast on invalid targets and cache activatables for runtime interaction.
        private bool TryBuildActivatables()
        {
            if (activationTargets == null || activationTargets.Length == 0)
            {
                Debug.LogError("Switch requires at least one Activation Target.", this);
                return false;
            }

            _activatables = new IActivatable[activationTargets.Length];

            for (int i = 0; i < activationTargets.Length; i++)
            {
                MonoBehaviour target = activationTargets[i].targetObject;
                if (target == null)
                {
                    Debug.LogError($"Switch activation target at index {i} is missing.", this);
                    return false;
                }

                IActivatable activatable = target.GetComponent<IActivatable>();
                if (activatable == null)
                {
                    Debug.LogError($"Switch activation target at index {i} must implement IActivatable.", this);
                    return false;
                }

                _activatables[i] = activatable;
            }

            return true;
        }
    }
}
