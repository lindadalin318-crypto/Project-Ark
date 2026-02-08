using UnityEngine;
using ProjectArk.Heat;
using ProjectArk.Ship;

namespace ProjectArk.Combat
{
    /// <summary>
    /// Readonly dependency bundle providing access to all ship systems.
    /// Created once by StarChartController in Awake and passed to
    /// Light Sail / Satellite runners for dependency injection.
    /// </summary>
    public readonly struct StarChartContext
    {
        public readonly ShipMotor Motor;
        public readonly ShipAiming Aiming;
        public readonly InputHandler Input;
        public readonly HeatSystem Heat; // nullable
        public readonly StarChartController Controller;
        public readonly Transform ShipTransform;

        public StarChartContext(ShipMotor motor, ShipAiming aiming,
                                InputHandler input, HeatSystem heat,
                                StarChartController controller, Transform shipTransform)
        {
            Motor = motor;
            Aiming = aiming;
            Input = input;
            Heat = heat;
            Controller = controller;
            ShipTransform = shipTransform;
        }
    }
}
