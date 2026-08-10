using System;

namespace MeasurementMix
{
    /// <summary>
    /// Copy-over bridge for the first package version.
    /// New scenes should use BalanceScaleController.
    /// </summary>
    [Obsolete("Use BalanceScaleController.")]
    public class ScaleController : BalanceScaleController
    {
    }
}
