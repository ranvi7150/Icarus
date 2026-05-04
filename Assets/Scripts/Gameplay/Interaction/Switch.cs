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

        private void Awake()
        {
            SetInteractionPromptVisible(false);
        }

        private void OnDisable()
        {
            SetInteractionPromptVisible(false);
        }

        public void Interact()
        {
            if (activationTargets == null || activationTargets.Length == 0)
            {
                return;
            }

            for (int i = 0; i < activationTargets.Length; i++)
            {
                MonoBehaviour target = activationTargets[i].targetObject;
                if (target == null)
                {
                    continue;
                }

                IActivatable activatable = target.GetComponent<IActivatable>();
                activatable?.Activate();
            }
        }

        public void SetInteractionPromptVisible(bool isVisible)
        {
            if (interactionPrompt == null)
            {
                return;
            }

            interactionPrompt.SetActive(isVisible);
        }
    }
}
