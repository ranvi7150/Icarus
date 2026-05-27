using UnityEngine;
using UnityEngine.UI;

namespace Icarus.Gameplay.Player
{
    public class GlideBar : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private Image foregroundFillImage;

        private void Awake()
        {
            if (canvas == null)
            {
                Debug.LogError("GlideBar requires a Canvas reference.", this);
                enabled = false;
                return;
            }

            if (foregroundFillImage == null)
            {
                Debug.LogError("GlideBar requires a ForeGround Image reference.", this);
                enabled = false;
                return;
            }

            foregroundFillImage.type = Image.Type.Filled;
            foregroundFillImage.fillMethod = Image.FillMethod.Horizontal;
            foregroundFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;

            ApplyVisualState(isVisible: false, normalizedFillAmount: 1f);
        }

        public void ApplyVisualState(bool isVisible, float normalizedFillAmount)
        {
            canvas.gameObject.SetActive(isVisible);
            foregroundFillImage.fillAmount = Mathf.Clamp01(normalizedFillAmount);
        }
    }
}
