using UnityEngine;
using System;
using UnityEngine.InputSystem;

namespace Icarus.Gameplay.Player
{
    [RequireComponent(typeof(PlayerStats))]
    [RequireComponent(typeof(PlayerInteractor))]
    [RequireComponent(typeof(PlayerMotor))]
    public class PlayerController : MonoBehaviour
    {
        private enum PlayerState
        {
            Alive,
            Dying,
            Respawning,
            SceneTransitioning
        }

        private PlayerInteractor _playerInteractor;
        private PlayerMotor _playerMotor;
        private PlayerStats _playerStats;
        private Wing _wing;

        private PlayerState _state = PlayerState.Alive;

        public event Action Died;
        public event Action Respawned;

        public Vector2 MoveInput => _playerMotor.MoveInput;
        public Vector2 Velocity => _playerMotor.Velocity;
        public int FacingDirection => _playerMotor.FacingDirection;
        public bool IsGroundedForAnimation => _playerMotor.IsGrounded;
        
        public bool IsAlive => _state == PlayerState.Alive;
        public bool IsDying => _state == PlayerState.Dying;

        private void Awake()
        {
            _playerMotor = GetComponent<PlayerMotor>();
            _playerInteractor = GetComponent<PlayerInteractor>();
            _playerStats = GetComponent<PlayerStats>();
            _wing = GetComponentInChildren<Wing>();

            if (_playerMotor == null)
            {
                Debug.LogError("PlayerController requires a PlayerMotor component.", this);
                enabled = false;
                return;
            }

            if (_playerInteractor == null)
            {
                Debug.LogError("PlayerController requires a PlayerInteractor component.", this);
                enabled = false;
                return;
            }

            if (_playerStats == null)
            {
                Debug.LogError("PlayerController requires a PlayerStats component.", this);
                enabled = false;
                return;
            }

            if (_wing == null)
            {
                Debug.LogError("PlayerController requires a Wing component in children.", this);
                enabled = false;
                return;
            }
        }

        private void Update()
        {
            if (!IsAlive)
            {
                return;
            }

            _playerMotor.Tick();
        }

        private void FixedUpdate()
        {
            if (!IsAlive)
            {
                return;
            }

            _playerMotor.FixedTick();
        }

        //All Reset (Move, Dash, Jump, AirFlow, Interaction)
        private void ResetGameplayState()
        {
            _playerMotor.ResetMovementState();
            _playerInteractor.ClearTarget();
        }

        public void RequestDeath()
        {
            if (!IsAlive)
            {
                return;
            }

            _state = PlayerState.Dying;
            ResetGameplayState();
            Died?.Invoke();
        }

        public void BeginSceneTransition()
        {
            if (!IsAlive)
            {
                return;
            }

            _state = PlayerState.SceneTransitioning;
            ResetGameplayState();
        }

        public void RespawnAt(Vector3 spawnPosition)
        {
            _state = PlayerState.Respawning;
            transform.position = spawnPosition;
            ResetGameplayState();

            _state = PlayerState.Alive;
            Respawned?.Invoke();
        }

        public void SetAirFlowVelocity(Vector2 velocity)
        {
            if (!IsAlive)
            {
                return;
            }

            _playerMotor.SetAirFlowVelocity(velocity);
        }

        public void ClearAirFlowVelocity()
        {
            _playerMotor.ClearAirFlowVelocity();
        }

        public bool TryCollectFeather(string featherId)
        {
            if (!IsAlive)
            {
                return false;
            }

            return _playerStats.TryCollectFeather(featherId);
        }

        /* Player Control Event */

        public void OnMove(InputAction.CallbackContext ctx)
        {
            if (!IsAlive)
            {
                return;
            }

            _playerMotor.SetMoveInput(ctx.ReadValue<Vector2>());
        }

        public void OnJump(InputAction.CallbackContext ctx)
        {
            if (!IsAlive)
            {
                return;
            }

            if (ctx.started || ctx.performed)
            {
                _playerMotor.PressJump();
            }

            if (ctx.canceled)
            {
                _playerMotor.ReleaseJump();
            }
        }

        public void OnDash(InputAction.CallbackContext ctx)
        {
            if (!IsAlive)
            {
                return;
            }

            if (ctx.started || ctx.performed)
            {
                _playerMotor.RequestDash();
            }
        }

        public void OnWingToggle(InputAction.CallbackContext ctx)
        {
            if (!IsAlive)
            {
                return;
            }

            if (ctx.performed)
            {
                _wing.ToggleWing();
            }
        }

        public void OnInteract(InputAction.CallbackContext ctx)
        {
            if (!IsAlive)
            {
                return;
            }

            if (!ctx.performed)
            {
                return;
            }

            _playerInteractor.TryInteract();
        }
    }
}
