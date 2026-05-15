using UnityEngine;

namespace Icarus.Gameplay.Player
{
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private Animator bodyAnimator;
        [SerializeField] private string idleStateName = "Idle";
        [SerializeField] private string runStateName = "Run";
        [SerializeField] private string jumpStateName = "Jump";
        [SerializeField] private string interactionStateName = "Interaction";
        [SerializeField] private float runInputThreshold = 0.01f;
        [SerializeField] private float runVelocityThreshold = 0.05f;

        private int _idleStateHash;
        private int _runStateHash;
        private int _jumpStateHash;
        private int _interactionStateHash;
        
        private int _currentStateHash;
        private int _interactionStartFrame;
        private bool _isInteractionPlaying;

        private void Awake()
        {
            if (playerController == null)
            {
                Debug.LogError("PlayerAnimator requires a PlayerController component in Parent.", this);
                enabled = false;
                return;
            }

            if (bodyAnimator == null)
            {
                Debug.LogError("PlayerAnimator requires a Body Animator reference.", this);
                enabled = false;
                return;
            }

            _idleStateHash = Animator.StringToHash(idleStateName);
            _runStateHash = Animator.StringToHash(runStateName);
            _jumpStateHash = Animator.StringToHash(jumpStateName);
            _interactionStateHash = Animator.StringToHash(interactionStateName);
            _currentStateHash = _idleStateHash;
        }

        private void OnEnable()
        {
            playerController.Interacted += PlayInteraction;
        }

        private void OnDisable()
        {
            playerController.Interacted -= PlayInteraction;
        }

        private void Update()
        {
            if (IsInteractionStillPlaying())
            {
                return;
            }

            _isInteractionPlaying = false;
            PlayState(GetMovementStateHash());
        }

        private int GetMovementStateHash()
        {
            if (!playerController.IsGroundedForAnimation)
            {
                return _jumpStateHash;
            }

            bool hasMoveInput = Mathf.Abs(playerController.MoveInput.x) > runInputThreshold;
            bool isMoving = Mathf.Abs(playerController.Velocity.x) > runVelocityThreshold;
            return hasMoveInput || isMoving ? _runStateHash : _idleStateHash;
        }

        private void PlayInteraction()
        {
            _isInteractionPlaying = true;
            _interactionStartFrame = Time.frameCount;
            PlayState(_interactionStateHash, restart: true);
        }

        private bool IsInteractionStillPlaying()
        {
            if (!_isInteractionPlaying)
            {
                return false;
            }

            if (Time.frameCount == _interactionStartFrame)
            {
                return true;
            }

            AnimatorStateInfo stateInfo = bodyAnimator.GetCurrentAnimatorStateInfo(0);
            return stateInfo.shortNameHash == _interactionStateHash && stateInfo.normalizedTime < 1f;
        }

        private void PlayState(int stateHash, bool restart = false)
        {
            if (!restart && _currentStateHash == stateHash)
            {
                return;
            }

            _currentStateHash = stateHash;
            bodyAnimator.Play(stateHash, 0, 0f);
        }
    }
}
