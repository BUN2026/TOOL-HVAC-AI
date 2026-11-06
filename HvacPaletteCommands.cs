using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;

[assembly: ExtensionApplication(typeof(HvacPalette.HvacPaletteCommands))]
[assembly: CommandClass(typeof(HvacPalette.HvacPaletteCommands))]

namespace HvacPalette
{
    public class HvacPaletteCommands : IExtensionApplication
    {
        private static PaletteSet _paletteSet;
        private static HvacPaletteControl _hvacControl;

        public void Initialize()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc != null)
            {
                Editor ed = doc.Editor;
                if (ed != null)
                {
                    ed.WriteMessage("\n[HVAC Plugin] Đã nạp thành công! Gõ 'HVAC' hoặc 'HVAC_PALETTE' để mở bảng điều khiển.\n");
                }
            }
        }

        public void Terminate()
        {
            if (_paletteSet != null)
            {
                _paletteSet.Dispose();
                _paletteSet = null;
            }
        }

        [CommandMethod("HVAC")]
        [CommandMethod("HVAC_PALETTE")]
        public void ShowHvacPalette()
        {
            if (_paletteSet == null)
            {
                _paletteSet = new PaletteSet("HVAC DESIGN PALETTE (ASHRAE 2001)", new Guid("D3C8A972-2B41-4E6E-99C1-72BF5A31E5F8"))
                {
                    Style = PaletteSetStyles.ShowAutoHideButton |
                            PaletteSetStyles.ShowCloseButton |
                            PaletteSetStyles.ShowPropertiesMenu,
                    MinimumSize = new System.Drawing.Size(320, 500),
                    Size = new System.Drawing.Size(360, 680),
                    Dock = DockSides.Right
                };

                _hvacControl = new HvacPaletteControl();
                _paletteSet.AddVisual("Thiết Kế HVAC", _hvacControl);
            }

            _paletteSet.Visible = true;
        }
    }
}
