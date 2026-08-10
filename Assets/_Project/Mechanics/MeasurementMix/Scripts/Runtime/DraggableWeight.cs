using System;

namespace MeasurementMix
{
    /// <summary>
    /// Copy-over bridge for the first package version.
    /// New scenes should use MeasurementWeightItem.
    /// </summary>
    [Obsolete("Use MeasurementWeightItem.")]
    public class DraggableWeight : MeasurementWeightItem
    {
    }
}
