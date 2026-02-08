using UnityEngine;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Abstract base class for all Light Sail runtime behaviors.
    /// Subclasses monitor ship state via <see cref="StarChartContext"/> and provide
    /// combat buffs by overriding <see cref="ModifyProjectileParams"/>.
    ///
    /// Lifecycle (managed by <see cref="LightSailRunner"/>):
    /// 1. Initialize(context) — called once after instantiation
    /// 2. Tick(dt, context)  — called every frame
    /// 3. OnDisabled/OnEnabled — called during overheat penalty
    /// 4. Cleanup()          — called before destruction on unequip
    /// </summary>
    public abstract class LightSailBehavior : MonoBehaviour
    {
        /// <summary> Whether this sail is currently providing buffs (false during overheat). </summary>
        public bool IsActive { get; private set; } = true;

        /// <summary> Called once after instantiation. Cache context references here. </summary>
        public virtual void Initialize(StarChartContext context) { }

        /// <summary> Called every frame by LightSailRunner. Monitor state, update buff values. </summary>
        public abstract void Tick(float deltaTime, StarChartContext context);

        /// <summary>
        /// Modify projectile parameters at spawn time. Called for every projectile.
        /// Default implementation is a no-op. Override to apply buffs.
        /// Not called when sail is disabled (runner handles the gate).
        /// </summary>
        public virtual void ModifyProjectileParams(ref ProjectileParams parms) { }

        /// <summary> Called when entering overheat. Override to reset internal buff state. </summary>
        public virtual void OnDisabled()
        {
            IsActive = false;
        }

        /// <summary> Called when exiting overheat. Buffs resume. </summary>
        public virtual void OnEnabled()
        {
            IsActive = true;
        }

        /// <summary> Called before the behavior GameObject is destroyed (unequip). </summary>
        public virtual void Cleanup() { }
    }
}
