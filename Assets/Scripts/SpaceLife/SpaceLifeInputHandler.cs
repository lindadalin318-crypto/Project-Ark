
using UnityEngine;

namespace ProjectArk.SpaceLife
{
    public class SpaceLifeInputHandler : MonoBehaviour
    {
        [Header("Key Bindings")]
        [SerializeField] private KeyCode _toggleSpaceLifeKey = KeyCode.Tab;

        private void Update()
        {
            HandleToggleSpaceLife();
        }

        private void HandleToggleSpaceLife()
        {
            if (Input.GetKeyDown(_toggleSpaceLifeKey))
            {
                if (SpaceLifeManager.Instance != null)
                {
                    SpaceLifeManager.Instance.ToggleSpaceLife();
                }
                else
                {
                    Debug.LogWarning("[SpaceLifeInputHandler] SpaceLifeManager not found!");
                }
            }
        }
    }
}

