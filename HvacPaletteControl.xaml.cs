using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HvacPalette
{
    public partial class HvacPaletteControl : UserControl
    {
        private Dictionary<string, List<HvacLayerItem>> _categoryLayers;
        private string _currentCategory = "duct";
        private HvacLayerItem _currentLayer;
        private string _flowUnit = "m3h"; // "m3h" hoặc "Ls"
        private string _termFlowUnit = "m3h";
        private bool _isFrictionMethod = true;
        private double _currentPv = 11.88;

        public HvacPaletteControl()
        {
            InitializeComponent();
            InitData();
        }

        private void InitData()
        {
            _categoryLayers = HvacConfigRepository.GetCategoryLayers();

            // Populate Sizes
            int[] widths = new int[] { 150, 200, 250, 300, 350, 400, 450, 500, 600, 700, 800, 1000, 1200 };
            int[] heights = new int[] { 150, 200, 250, 300, 350, 400, 500, 600, 800 };

            foreach (int w in widths) cboWidth.Items.Add(w);
            foreach (int h in heights) cboHeight.Items.Add(h);
            cboWidth.SelectedItem = 400;
            cboHeight.SelectedItem = 250;

            // Populate Fitting Types with Co (ASHRAE pp. 34.29 - 34.68)
            cboFittingType.Items.Add(new ComboBoxItem { Content = "Co cong 90° (R/W=1.5, Co = 0.22)", Tag = 0.22 });
            cboFittingType.Items.Add(new ComboBoxItem { Content = "Co cong 90° (R/W=1.0, Co = 0.38)", Tag = 0.38 });
            cboFittingType.Items.Add(new ComboBoxItem { Content = "Co vuông 90° có Vanes (Co = 0.18)", Tag = 0.18 });
            cboFittingType.Items.Add(new ComboBoxItem { Content = "Co vuông 90° KHÔNG cánh (Co = 1.25)", Tag = 1.25 });
            cboFittingType.Items.Add(new ComboBoxItem { Content = "Co chếch 45° (Co = 0.12)", Tag = 0.12 });
            cboFittingType.Items.Add(new ComboBoxItem { Content = "Tê rẽ nhánh 90° Branch (Co = 0.95)", Tag = 0.95 });
            cboFittingType.Items.Add(new ComboBoxItem { Content = "Tê dòng chạy thẳng Main (Co = 0.25)", Tag = 0.25 });
            cboFittingType.Items.Add(new ComboBoxItem { Content = "Côn thu góc ≤ 30° (Co = 0.08)", Tag = 0.08 });
            cboFittingType.Items.Add(new ComboBoxItem { Content = "Van chặn lửa FD (Co = 0.30)", Tag = 0.30 });
            cboFittingType.Items.Add(new ComboBoxItem { Content = "Van VCD mở 100% (Co = 0.25)", Tag = 0.25 });
            cboFittingType.SelectedIndex = 0;

            // Populate Air Terminals (5 Block DXF)
            cboTerminalBlock.Items.Add(new ComboBoxItem { Content = "Khuếch tán 4 hướng (BUN_Mieng gio khuyech tan 4 huong.dxf)", Tag = "BUN_Mieng gio khuyech tan 4 huong.dxf" });
            cboTerminalBlock.Items.Add(new ComboBoxItem { Content = "Khuếch tán tròn (BUN_Mieng gio khuyech tan tron.dxf)", Tag = "BUN_Mieng gio khuyech tan tron.dxf" });
            cboTerminalBlock.Items.Add(new ComboBoxItem { Content = "Miệng gió nan đơn (BUN_Mieng gio nan don.dxf)", Tag = "BUN_Mieng gio nan don.dxf" });
            cboTerminalBlock.Items.Add(new ComboBoxItem { Content = "Miệng gió hồi sọt trứng (BUN_Mieng gio sot trung.dxf)", Tag = "BUN_Mieng gio sot trung.dxf" });
            cboTerminalBlock.Items.Add(new ComboBoxItem { Content = "Miệng gió Louver ngoài trời (BUN_Mieng gio louver.dxf)", Tag = "BUN_Mieng gio louver.dxf" });
            cboTerminalBlock.SelectedIndex = 0;

            // Populate Target Vneck
            cboTargetVneck.Items.Add(new ComboBoxItem { Content = "2.0 m/s (Rất êm / NC 20)", Tag = 2.0 });
            cboTargetVneck.Items.Add(new ComboBoxItem { Content = "2.5 m/s (Chuẩn VP / NC 25)", Tag = 2.5 });
            cboTargetVneck.Items.Add(new ComboBoxItem { Content = "3.0 m/s (Thương mại / NC 30)", Tag = 3.0 });
            cboTargetVneck.SelectedIndex = 1;

            // Populate Terminal Face & Neck
            string[] faces = new string[] { "600x600", "450x450", "300x300", "1200x150", "1000x200" };
            foreach (string f in faces) cboTermFace.Items.Add(f);
            cboTermFace.SelectedIndex = 0;

            int[] necks = new int[] { 150, 200, 250, 300, 350 };
            foreach (int n in necks) cboTermNeck.Items.Add(n);
            cboTermNeck.SelectedItem = 200;

            // Populate Gas Sizes
            cboGasHp.Items.Add(new ComboBoxItem { Content = "1.5 - 2.0 HP (FCU Nhỏ)", Tag = 1.5 });
            cboGasHp.Items.Add(new ComboBoxItem { Content = "2.5 - 3.2 HP (FCU Vừa)", Tag = 3.0 });
            cboGasHp.Items.Add(new ComboBoxItem { Content = "4.0 - 5.0 HP (FCU Lớn)", Tag = 5.0 });
            cboGasHp.Items.Add(new ComboBoxItem { Content = "8.0 - 10.0 HP (Dàn nóng VRV)", Tag = 10.0 });
            cboGasHp.Items.Add(new ComboBoxItem { Content = "12.0 - 16.0 HP (Module VRV)", Tag = 16.0 });
            cboGasHp.SelectedIndex = 1;

            // Populate Drain Sizes
            cboDrainSize.Items.Add("D21 (1 FCU nhỏ)");
            cboDrainSize.Items.Add("D27 (1-2 FCU)");
            cboDrainSize.Items.Add("D34 (Gom 3-4 FCU)");
            cboDrainSize.Items.Add("D42 (Ống trục gom)");
            cboDrainSize.SelectedIndex = 1;

            PopulateCriteriaValues();
            LoadLayersForCategory("duct");
            RunDuctCalculation();
            RunTerminalCalculation();
        }

        private void PopulateCriteriaValues()
        {
            if (cboCriteriaValue == null) return;
            cboCriteriaValue.Items.Clear();

            if (_isFrictionMethod)
            {
                cboCriteriaValue.Items.Add(new ComboBoxItem { Content = "0.65 Pa/m (Êm dịu / Khách sạn)", Tag = 0.65 });
                cboCriteriaValue.Items.Add(new ComboBoxItem { Content = "0.80 Pa/m (Chuẩn ASHRAE / TCVN)", Tag = 0.80 });
                cboCriteriaValue.Items.Add(new ComboBoxItem { Content = "1.00 Pa/m (Kinh tế / Tối ưu diện tích)", Tag = 1.00 });
                cboCriteriaValue.Items.Add(new ComboBoxItem { Content = "1.20 Pa/m (Tốc độ cao / Trục chính)", Tag = 1.20 });
                cboCriteriaValue.SelectedIndex = 1;
            }
            else
            {
                cboCriteriaValue.Items.Add(new ComboBoxItem { Content = "2.5 m/s (Cổ miệng gió)", Tag = 2.5 });
                cboCriteriaValue.Items.Add(new ComboBoxItem { Content = "4.0 m/s (Nhánh miệng gió)", Tag = 4.0 });
                cboCriteriaValue.Items.Add(new ComboBoxItem { Content = "5.5 m/s (Nhánh chính)", Tag = 5.5 });
                cboCriteriaValue.Items.Add(new ComboBoxItem { Content = "7.0 m/s (Trục chính)", Tag = 7.0 });
                cboCriteriaValue.Items.Add(new ComboBoxItem { Content = "8.5 m/s (Trục đứng)", Tag = 8.5 });
                cboCriteriaValue.SelectedIndex = 3;
            }
        }

        private void LoadLayersForCategory(string categoryKey)
        {
            _currentCategory = categoryKey;
            cboLayerSelect.Items.Clear();

            if (_categoryLayers.ContainsKey(categoryKey))
            {
                foreach (HvacLayerItem item in _categoryLayers[categoryKey])
                {
                    cboLayerSelect.Items.Add(item);
                }
                if (cboLayerSelect.Items.Count > 0)
                {
                    cboLayerSelect.SelectedIndex = 0;
                }
            }
        }

        private void CboLayerSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _currentLayer = cboLayerSelect.SelectedItem as HvacLayerItem;
            if (_currentLayer != null)
            {
                txtStatusMessage.Text = string.Format("Layer hiện hành: {0} (Color {1})", _currentLayer.Name, _currentLayer.ColorIndex);
                CadExecutor.SetCurrentLayer(_currentLayer.Name, _currentLayer.ColorIndex);
            }
        }

        private void BtnCat_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null || btn.Tag == null) return;
            string cat = btn.Tag.ToString();

            btnCatDuct.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
            btnCatGas.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
            btnCatChiller.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
            btnCatWater.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
            btnCatSignal.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
            btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0284C7"));

            panelDuct.Visibility = cat == "duct" ? Visibility.Visible : Visibility.Collapsed;
            panelGas.Visibility = cat == "gas" ? Visibility.Visible : Visibility.Collapsed;
            panelChiller.Visibility = cat == "chiller" ? Visibility.Visible : Visibility.Collapsed;
            panelWater.Visibility = cat == "water" ? Visibility.Visible : Visibility.Collapsed;
            panelSignal.Visibility = cat == "signal" ? Visibility.Visible : Visibility.Collapsed;

            LoadLayersForCategory(cat);
        }

        private void BtnFlowUnit_Click(object sender, RoutedEventArgs e)
        {
            double val;
            if (!double.TryParse(txtFlowValue.Text, out val)) val = 1200;

            if (_flowUnit == "m3h")
            {
                _flowUnit = "Ls";
                btnFlowUnit.Content = "L/s";
                btnFlowUnit.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309"));
                txtFlowValue.Text = Math.Round(val / 3.6).ToString();
            }
            else
            {
                _flowUnit = "m3h";
                btnFlowUnit.Content = "m³/h";
                btnFlowUnit.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0369A1"));
                txtFlowValue.Text = Math.Round(val * 3.6).ToString();
            }
            RunDuctCalculation();
        }

        private void RbMethod_Checked(object sender, RoutedEventArgs e)
        {
            _isFrictionMethod = rbMethodFriction.IsChecked == true;
            PopulateCriteriaValues();
            RunDuctCalculation();
        }

        private void TxtFlowValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            RunDuctCalculation();
        }

        private void CboCriteriaValue_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RunDuctCalculation();
        }

        private void CboSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboWidth == null || cboHeight == null || cboWidth.SelectedItem == null || cboHeight.SelectedItem == null) return;
            double w = Convert.ToDouble(cboWidth.SelectedItem);
            double h = Convert.ToDouble(cboHeight.SelectedItem);

            double flowM3s = GetFlowM3s();
            UpdateHydraulicsUI(w, h, flowM3s);
        }

        private double GetFlowM3s()
        {
            double rawFlow;
            if (!double.TryParse(txtFlowValue.Text, out rawFlow)) return 0;
            return _flowUnit == "Ls" ? (rawFlow / 1000.0) : (rawFlow / 3600.0);
        }

        private void RunDuctCalculation()
        {
            if (cboWidth == null || cboHeight == null || cboCriteriaValue == null || cboCriteriaValue.SelectedItem == null) return;

            double flowM3s = GetFlowM3s();
            if (flowM3s <= 0) return;

            int[] widths = new int[] { 150, 200, 250, 300, 350, 400, 450, 500, 600, 700, 800, 1000, 1200 };
            int[] heights = new int[] { 150, 200, 250, 300, 350, 400, 500, 600, 800 };

            double targetValue = 0.8;
            ComboBoxItem item = cboCriteriaValue.SelectedItem as ComboBoxItem;
            if (item != null && item.Tag is double)
            {
                targetValue = (double)item.Tag;
            }

            int bestW = 400, bestH = 250;
            double minDiff = double.MaxValue;

            if (!_isFrictionMethod)
            {
                double targetArea = flowM3s / targetValue;
                foreach (int w in widths)
                {
                    foreach (int h in heights)
                    {
                        double area = (w / 1000.0) * (h / 1000.0);
                        double ratio = Math.Max(w, h) / (double)Math.Min(w, h);
                        if (ratio <= 3.5)
                        {
                            double diff = Math.Abs(area - targetArea);
                            if (diff < minDiff)
                            {
                                minDiff = diff;
                                bestW = w;
                                bestH = h;
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (int w in widths)
                {
                    foreach (int h in heights)
                    {
                        double ratio = Math.Max(w, h) / (double)Math.Min(w, h);
                        if (ratio <= 3.5)
                        {
                            DuctFlowResult res = AshraeCalculator.CalculateDuctHydraulics(w, h, flowM3s);
                            double diff = Math.Abs(res.FrictionDropPerMeter - targetValue);
                            if (diff < minDiff)
                            {
                                minDiff = diff;
                                bestW = w;
                                bestH = h;
                            }
                        }
                    }
                }
            }

            cboWidth.SelectedItem = bestW;
            cboHeight.SelectedItem = bestH;
            UpdateHydraulicsUI(bestW, bestH, flowM3s);
        }

        private void UpdateHydraulicsUI(double w, double h, double flowM3s)
        {
            DuctFlowResult res = AshraeCalculator.CalculateDuctHydraulics(w, h, flowM3s);
            _currentPv = res.DynamicPressurePv;

            lblResultV.Text = string.Format("V = {0:F2} m/s", res.Velocity);
            lblResultPv.Text = string.Format("Pv = {0:F2} Pa", res.DynamicPressurePv);
            lblResultDh.Text = string.Format("Dh = Ø{0:F0} mm", res.Dh);
            lblResultDpf.Text = string.Format("ΔPf = {0:F2} Pa/m", res.FrictionDropPerMeter);

            bool isWarning = false;

            if (res.Velocity > 8.0)
            {
                lblResultV.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                isWarning = true;
            }
            else if (res.Velocity > 6.5)
            {
                lblResultV.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FACC15"));
            }
            else
            {
                lblResultV.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80"));
            }

            if (res.FrictionDropPerMeter > 1.30)
            {
                lblResultDpf.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                isWarning = true;
            }
            else if (res.FrictionDropPerMeter > 1.05)
            {
                lblResultDpf.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FACC15"));
            }
            else
            {
                lblResultDpf.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80"));
            }

            if (res.AspectRatio <= 3.0)
            {
                lblAspectRatio.Text = string.Format("W/H: {0:F1}:1 (Chuẩn)", res.AspectRatio);
                lblAspectRatio.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80"));
            }
            else if (res.AspectRatio <= 4.0)
            {
                lblAspectRatio.Text = string.Format("W/H: {0:F1}:1 (Cao)", res.AspectRatio);
                lblAspectRatio.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FACC15"));
            }
            else
            {
                lblAspectRatio.Text = string.Format("W/H: {0:F1}:1 (>4:1 Cảnh báo)", res.AspectRatio);
                lblAspectRatio.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                isWarning = true;
            }

            if (isWarning)
            {
                borderResultBox.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                txtWarningNotice.Visibility = Visibility.Visible;
                txtWarningNotice.Text = string.Format("⚠️ CẢNH BÁO: V={0:F1}m/s, ΔPf={1:F2}Pa/m vượt ngưỡng chuẩn", res.Velocity, res.FrictionDropPerMeter);
            }
            else
            {
                borderResultBox.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"));
                txtWarningNotice.Visibility = Visibility.Collapsed;
            }

            CalculateFittingLoss();
        }

        private void CboFittingType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CalculateFittingLoss();
        }

        private void CalculateFittingLoss()
        {
            if (cboFittingType == null || cboFittingType.SelectedItem == null || lblFittingLoss == null) return;
            ComboBoxItem item = cboFittingType.SelectedItem as ComboBoxItem;
            if (item != null && item.Tag is double)
            {
                double co = (double)item.Tag;
                double dpj = co * _currentPv;
                lblFittingLoss.Text = string.Format("ΔPj = {0:F2} Pa", dpj);
            }
        }

        // ==========================================
        // AIR TERMINAL CALCULATION & INSERTION
        // ==========================================
        private void BtnTermFlowUnit_Click(object sender, RoutedEventArgs e)
        {
            double val;
            if (!double.TryParse(txtTermFlow.Text, out val)) val = 300;

            if (_termFlowUnit == "m3h")
            {
                _termFlowUnit = "Ls";
                btnTermFlowUnit.Content = "L/s";
                btnTermFlowUnit.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309"));
                txtTermFlow.Text = Math.Round(val / 3.6).ToString();
            }
            else
            {
                _termFlowUnit = "m3h";
                btnTermFlowUnit.Content = "m³/h";
                btnTermFlowUnit.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#312E81"));
                txtTermFlow.Text = Math.Round(val * 3.6).ToString();
            }
            RunTerminalCalculation();
        }

        private void BtnCopyFlow_Click(object sender, RoutedEventArgs e)
        {
            double rawDuct;
            if (!double.TryParse(txtFlowValue.Text, out rawDuct)) rawDuct = 1200;
            double termQ = rawDuct / 4.0;
            if (_flowUnit == "m3h" && _termFlowUnit == "Ls") termQ /= 3.6;
            if (_flowUnit == "Ls" && _termFlowUnit == "m3h") termQ *= 3.6;

            txtTermFlow.Text = Math.Round(termQ).ToString();
            RunTerminalCalculation();
        }

        private void TxtTermFlow_TextChanged(object sender, TextChangedEventArgs e)
        {
            RunTerminalCalculation();
        }

        private void CboTargetVneck_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RunTerminalCalculation();
        }

        private void CboTerminalBlock_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RunTerminalCalculation();
        }

        private void CboTermManual_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboTermNeck == null || cboTermNeck.SelectedItem == null) return;
            double neck = Convert.ToDouble(cboTermNeck.SelectedItem);
            UpdateTerminalHydraulics(neck);
        }

        private void RunTerminalCalculation()
        {
            if (cboTermNeck == null || cboTermFace == null || cboTargetVneck == null || cboTargetVneck.SelectedItem == null) return;

            double rawQ;
            if (!double.TryParse(txtTermFlow.Text, out rawQ)) return;
            double qM3s = _termFlowUnit == "Ls" ? (rawQ / 1000.0) : (rawQ / 3600.0);
            if (qM3s <= 0) return;

            double targetV = 2.5;
            ComboBoxItem vItem = cboTargetVneck.SelectedItem as ComboBoxItem;
            if (vItem != null && vItem.Tag is double) targetV = (double)vItem.Tag;

            double reqArea = qM3s / targetV;
            int[] standardNecks = new int[] { 150, 200, 250, 300, 350 };
            int bestNeck = 200;
            double minDiff = double.MaxValue;

            foreach (int d in standardNecks)
            {
                double area = Math.PI * Math.Pow(d / 2000.0, 2);
                double diff = Math.Abs(area - reqArea);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    bestNeck = d;
                }
            }

            string bestFace = "600x600";
            if (bestNeck <= 150) bestFace = "300x300";
            else if (bestNeck <= 200) bestFace = "450x450";

            cboTermNeck.SelectedItem = bestNeck;
            cboTermFace.SelectedItem = bestFace;

            UpdateTerminalHydraulics(bestNeck);
        }

        private void UpdateTerminalHydraulics(double neckDiaMm)
        {
            double rawQ;
            if (!double.TryParse(txtTermFlow.Text, out rawQ)) return;
            double qM3s = _termFlowUnit == "Ls" ? (rawQ / 1000.0) : (rawQ / 3600.0);
            if (qM3s <= 0) return;

            double area = Math.PI * Math.Pow(neckDiaMm / 2000.0, 2);
            double vNeck = qM3s / area;
            double pvNeck = 1.204 * Math.Pow(vNeck, 2) / 2.0;
            double dp = 2.0 * pvNeck;
            int nc = (int)Math.Round(18 + (vNeck * 3.8));

            lblTermVneck.Text = string.Format("V={0:F2} m/s", vNeck);
            lblTermDp.Text = string.Format("ΔP={0:F1} Pa", dp);
            lblTermNC.Text = string.Format("NC {0}", nc);

            if (vNeck > 3.5)
            {
                lblTermVneck.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                borderTermResult.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                lblTermWarning.Visibility = Visibility.Visible;
            }
            else if (vNeck > 2.8)
            {
                lblTermVneck.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FACC15"));
                borderTermResult.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"));
                lblTermWarning.Visibility = Visibility.Collapsed;
            }
            else
            {
                lblTermVneck.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80"));
                borderTermResult.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"));
                lblTermWarning.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnInsertTerminal_Click(object sender, RoutedEventArgs e)
        {
            if (_currentLayer == null || cboTerminalBlock.SelectedItem == null || cboTermNeck.SelectedItem == null) return;
            ComboBoxItem bItem = cboTerminalBlock.SelectedItem as ComboBoxItem;
            if (bItem == null || bItem.Tag == null) return;

            string dxfName = bItem.Tag.ToString();
            double neck = Convert.ToDouble(cboTermNeck.SelectedItem);
            string face = cboTermFace.SelectedItem != null ? cboTermFace.SelectedItem.ToString() : "600x600";

            double rawQ;
            if (!double.TryParse(txtTermFlow.Text, out rawQ)) rawQ = 300;
            double flowM3h = _termFlowUnit == "Ls" ? (rawQ * 3.6) : rawQ;
            double flowLs = _termFlowUnit == "Ls" ? rawQ : (rawQ / 3.6);

            bool attachText = chkAttachText.IsChecked == true;

            CadExecutor.InsertAirTerminalWithFlow(dxfName, flowM3h, flowLs, neck, face, _currentLayer.Name, _currentLayer.ColorIndex, attachText);
        }

        private void BtnAutoConnect_Click(object sender, RoutedEventArgs e)
        {
            if (_currentLayer == null) return;
            CadExecutor.AutoConnectTerminalToMainDuct(_currentLayer.Name, "M-AC air duct flex", _currentLayer.ColorIndex);
        }

        // ==========================================
        // CAD DRAWING ACTIONS
        // ==========================================
        private void BtnDrawDuct_Click(object sender, RoutedEventArgs e)
        {
            if (_currentLayer == null || cboWidth.SelectedItem == null || cboHeight.SelectedItem == null) return;
            double w = Convert.ToDouble(cboWidth.SelectedItem);
            double h = Convert.ToDouble(cboHeight.SelectedItem);
            string layerText = _currentLayer.Name + "-TEXT";
            string prefix = _currentLayer.Label != null && _currentLayer.Label.Contains("(") ? 
                _currentLayer.Label.Substring(_currentLayer.Label.IndexOf("(") + 1).Replace(")", "") : "SA";
            bool isMain = chkIsMainDuct.IsChecked == true;

            CadExecutor.DrawContinuousDuct(w, h, _currentLayer.Name, layerText, _currentLayer.ColorIndex, prefix, isMain);
        }

        private void BtnDrawFlex_Click(object sender, RoutedEventArgs e)
        {
            if (_currentLayer == null) return;
            CadExecutor.InsertDxfBlock("BUN_Khop noi mem tron.dxf", Path.Combine(HvacConfigRepository.FittingDir, "BUN_Khop noi mem tron.dxf"), _currentLayer.Name);
        }

        private void BtnInsertBlock_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null || btn.Tag == null || _currentLayer == null) return;
            string fileName = btn.Tag.ToString();
            string fullPath = Path.Combine(HvacConfigRepository.FittingDir, fileName);
            if (!File.Exists(fullPath)) fullPath = Path.Combine(HvacConfigRepository.EquipmentDir, fileName);

            CadExecutor.InsertDxfBlock(fileName, fullPath, _currentLayer.Name);
        }

        private void CboGasHp_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboGasHp.SelectedItem == null) return;
            ComboBoxItem item = cboGasHp.SelectedItem as ComboBoxItem;
            if (item != null && item.Tag is double)
            {
                double hp = (double)item.Tag;
                if (hp <= 2.0) lblGasPairResult.Text = "Cặp ống: Ø6.35 / Ø12.7 mm";
                else if (hp <= 3.2) lblGasPairResult.Text = "Cặp ống: Ø9.52 / Ø15.88 mm";
                else if (hp <= 5.0) lblGasPairResult.Text = "Cặp ống: Ø9.52 / Ø19.05 mm";
                else if (hp <= 10.0) lblGasPairResult.Text = "Cặp ống: Ø12.7 / Ø28.58 mm";
                else lblGasPairResult.Text = "Cặp ống: Ø15.88 / Ø34.90 mm";
            }
        }

        private void BtnDrawGas_Click(object sender, RoutedEventArgs e)
        {
            if (_currentLayer == null) return;
            double liq = 9.52, suc = 15.88;
            ComboBoxItem item = cboGasHp.SelectedItem as ComboBoxItem;
            if (item != null && item.Tag is double)
            {
                double hp = (double)item.Tag;
                if (hp <= 2.0) { liq = 6.35; suc = 12.7; }
                else if (hp <= 3.2) { liq = 9.52; suc = 15.88; }
                else if (hp <= 5.0) { liq = 9.52; suc = 19.05; }
                else if (hp <= 10.0) { liq = 12.7; suc = 28.58; }
                else { liq = 15.88; suc = 34.90; }
            }
            CadExecutor.DrawGasPipePair(liq, suc, 60.0, _currentLayer.Name, _currentLayer.ColorIndex);
        }

        private void TxtChillerRt_TextChanged(object sender, TextChangedEventArgs e)
        {
            double rt;
            if (!double.TryParse(txtChillerRt.Text, out rt)) return;
            double g = rt * 0.605;
            string dn = rt <= 20 ? "DN50" : rt <= 40 ? "DN65" : rt <= 75 ? "DN80" : rt <= 130 ? "DN100" : "DN150";
            lblChillerDnResult.Text = string.Format("{0} (G = {1:F1} m³/h)", dn, g);
        }

        private void BtnDrawChiller_Click(object sender, RoutedEventArgs e)
        {
            if (_currentLayer == null) return;
            txtStatusMessage.Text = "Chọn tuyến cấp/hồi Chiller trên màn hình CAD.";
        }

        private void BtnDrawDrain_Click(object sender, RoutedEventArgs e)
        {
            if (_currentLayer == null) return;
            txtStatusMessage.Text = "Vẽ tuyến nước ngưng dốc i=1%.";
        }

        private void BtnDrawSignal_Click(object sender, RoutedEventArgs e)
        {
            if (_currentLayer == null) return;
            txtStatusMessage.Text = "Vẽ dây tín hiệu điều khiển FCU/Thermostat.";
        }

        private void BtnDrawTray_Click(object sender, RoutedEventArgs e)
        {
            if (_currentLayer == null) return;
            txtStatusMessage.Text = "Vẽ máng cáp trunking.";
        }
    }
}
