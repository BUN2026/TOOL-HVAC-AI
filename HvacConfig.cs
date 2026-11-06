using System;
using System.Collections.Generic;
using System.IO;

namespace HvacPalette
{
    public class HvacLayerItem
    {
        public string Name { get; set; }
        public short ColorIndex { get; set; }
        public string HexColor { get; set; }
        public string Label { get; set; }

        public HvacLayerItem() { }

        public HvacLayerItem(string name, short colorIndex, string hexColor, string label)
        {
            Name = name;
            ColorIndex = colorIndex;
            HexColor = hexColor;
            Label = label;
        }

        public override string ToString()
        {
            return string.Format("[{0}] {1} ({2})", ColorIndex, Name, Label);
        }
    }

    public static class HvacConfigRepository
    {
        public const string LibraryDir = @"D:\CAO VAN SIEU\27. CHUONG TRINH AI - MEP\THU VIEN";
        public const string ConfigPath = @"D:\CAO VAN SIEU\27. CHUONG TRINH AI - MEP\THU VIEN\hvac_config.json";

        public static string FittingDir
        {
            get
            {
                string hvacPath = Path.Combine(LibraryDir, "HVAC");
                if (Directory.Exists(hvacPath))
                {
                    string[] subDirs = Directory.GetDirectories(hvacPath);
                    foreach (string d in subDirs)
                    {
                        string name = Path.GetFileName(d);
                        if (name.IndexOf("Ph", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("kien", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return d;
                        }
                    }
                }
                return Path.Combine(LibraryDir, "HVAC", "Phụ kiện");
            }
        }

        public static string EquipmentDir
        {
            get
            {
                string hvacPath = Path.Combine(LibraryDir, "HVAC");
                if (Directory.Exists(hvacPath))
                {
                    string[] subDirs = Directory.GetDirectories(hvacPath);
                    foreach (string d in subDirs)
                    {
                        string name = Path.GetFileName(d);
                        if (name.IndexOf("Thi", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("bi", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return d;
                        }
                    }
                }
                return Path.Combine(LibraryDir, "HVAC", "Thiết bị");
            }
        }

        public static Dictionary<string, List<HvacLayerItem>> GetCategoryLayers()
        {
            Dictionary<string, List<HvacLayerItem>> dict = new Dictionary<string, List<HvacLayerItem>>();

            // 1. Ống Gió
            dict["duct"] = new List<HvacLayerItem>
            {
                new HvacLayerItem("M-AC air duct supply", 50, "#38bdf8", "Cấp lạnh (SA)"),
                new HvacLayerItem("M-AC air duct return", 30, "#f87171", "Hồi lạnh (RA)"),
                new HvacLayerItem("M-AC air duct fresh", 100, "#34d399", "Gió tươi AC (OA)"),
                new HvacLayerItem("M-AC air duct exhaust", 10, "#fbbf24", "Gió thải AC (EA)"),
                new HvacLayerItem("M-MV air duct fresh", 190, "#a78bfa", "TG Cấp tươi (MV-FA)"),
                new HvacLayerItem("M-MV air duct exhaust", 150, "#f472b6", "TG Hút thải (MV-EA)"),
                new HvacLayerItem("M-F air duct fresh", 170, "#fb923c", "Tăng áp buồng thang (F-PA)"),
                new HvacLayerItem("M-F air duct exhaust", 160, "#e11d48", "Hút khói sự cố (F-SE)")
            };

            // 2. Ống Gas
            dict["gas"] = new List<HvacLayerItem>
            {
                new HvacLayerItem("M-AC ref pipe", 220, "#c084fc", "Ống đồng Gas (VRV/Split)")
            };

            // 3. Chiller
            dict["chiller"] = new List<HvacLayerItem>
            {
                new HvacLayerItem("M-CW sup pipe", 140, "#60a5fa", "Nước lạnh cấp (CHWS)"),
                new HvacLayerItem("M-CW ret pipe", 150, "#3b82f6", "Nước lạnh hồi (CHWR)")
            };

            // 4. Nước Ngưng
            dict["water"] = new List<HvacLayerItem>
            {
                new HvacLayerItem("M-AC cond drain pipe", 130, "#2dd4bf", "Ống thoát nước ngưng")
            };

            // 5. Tín Hiệu
            dict["signal"] = new List<HvacLayerItem>
            {
                new HvacLayerItem("M-AC control wiring", 40, "#facc15", "Dây tín hiệu điều khiển")
            };

            return dict;
        }
    }
}
