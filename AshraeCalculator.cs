using System;

namespace HvacPalette
{
    /// <summary>
    /// Tính toán tổn thất áp suất và thủy lực theo chuẩn ASHRAE Fundamentals Handbook 2001 (Chương 34 - Duct Design)
    /// </summary>
    public static class AshraeCalculator
    {
        public const double AirDensity = 1.204; // kg/m3 (Điều kiện tiêu chuẩn)
        public const double SheetMetalRoughness = 0.09; // mm (Độ nhám tôn tráng kẽm galvanized duct - Bảng 1)

        /// <summary>
        /// Công thức 8 (trang 34.2): Áp suất động Pv = ρ * V^2 / 2
        /// </summary>
        public static double CalculatePv(double velocity)
        {
            return AirDensity * Math.Pow(velocity, 2) / 2.0;
        }

        /// <summary>
        /// Công thức 24 (trang 34.8): Đường kính thủy lực Dh = 4A / P = 2*W*H / (W + H) (mm)
        /// </summary>
        public static double CalculateDh(double widthMm, double heightMm)
        {
            if (widthMm + heightMm <= 0) return 0;
            return (2.0 * widthMm * heightMm) / (widthMm + heightMm);
        }

        /// <summary>
        /// Công thức 23 (trang 34.7): Số Reynolds Re = 66.4 * Dh * V
        /// </summary>
        public static double CalculateRe(double dhMm, double velocity)
        {
            return 66.4 * dhMm * velocity;
        }

        /// <summary>
        /// Công thức 21 (trang 34.7): Hệ số ma sát f
        /// </summary>
        public static double CalculateFrictionFactor(double dhMm, double reynolds)
        {
            if (dhMm <= 0 || reynolds <= 0) return 0.02;

            double fPrime = 0.11 * Math.Pow((SheetMetalRoughness / dhMm) + (68.0 / reynolds), 0.25);
            if (fPrime >= 0.018)
            {
                return fPrime;
            }
            return (0.85 * fPrime) + 0.0028;
        }

        /// <summary>
        /// Công thức 19 (trang 34.7): Tổn thất ma sát trên 1m chiều dài dPf = 1000 * (f / Dh) * Pv (Pa/m)
        /// </summary>
        public static DuctFlowResult CalculateDuctHydraulics(double widthMm, double heightMm, double flowM3s)
        {
            double areaM2 = (widthMm / 1000.0) * (heightMm / 1000.0);
            if (areaM2 <= 0) return new DuctFlowResult();

            double velocity = flowM3s / areaM2;
            double dh = CalculateDh(widthMm, heightMm);
            double re = CalculateRe(dh, velocity);
            double f = CalculateFrictionFactor(dh, re);
            double pv = CalculatePv(velocity);
            double dPfPerM = 1000.0 * (f / dh) * pv;
            double aspectRatio = Math.Max(widthMm, heightMm) / Math.Min(widthMm, heightMm);

            return new DuctFlowResult
            {
                Width = widthMm,
                Height = heightMm,
                Velocity = velocity,
                Dh = dh,
                Reynolds = re,
                FrictionFactor = f,
                DynamicPressurePv = pv,
                FrictionDropPerMeter = dPfPerM,
                AspectRatio = aspectRatio
            };
        }

        /// <summary>
        /// Công thức 30 (trang 34.11): Tổn thất qua phụ kiện fittings dPj = Co * Pv
        /// </summary>
        public static double CalculateFittingLoss(double co, double dynamicPressurePv)
        {
            return co * dynamicPressurePv;
        }
    }

    public class DuctFlowResult
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public double Velocity { get; set; }
        public double Dh { get; set; }
        public double Reynolds { get; set; }
        public double FrictionFactor { get; set; }
        public double DynamicPressurePv { get; set; }
        public double FrictionDropPerMeter { get; set; }
        public double AspectRatio { get; set; }
    }
}
