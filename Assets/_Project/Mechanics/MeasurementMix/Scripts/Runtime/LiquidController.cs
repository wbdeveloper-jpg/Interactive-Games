using System;

namespace MeasurementMix
{
    /// <summary>
    /// Copy-over bridge for the first package version.
    /// New scenes should use LiquidMeasurementController.
    /// </summary>
    [Obsolete("Use LiquidMeasurementController.")]
    public class LiquidController : LiquidMeasurementController
    {
    }
}
