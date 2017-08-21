using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HeavyDamageCalculator {
	using dPoint = System.Drawing.Point;
	/// <summary>
	/// MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow : Window {
		// コンストラクタ
		public MainWindow() {
			InitializeComponent();
			this.DataContext = new MainWindowViewModel();
		}
		protected override void OnSourceInitialized(EventArgs e) {
			base.OnSourceInitialized(e);
			this.Draw();
		}
		#region メニュー処理
		private void CopyPicMenu_Click(object sender, RoutedEventArgs e) {
			var stream = new System.IO.MemoryStream();
			ProbChart.SaveImage(stream, System.Drawing.Imaging.ImageFormat.Bmp);
			var bmp = new Bitmap(stream);
			Clipboard.SetDataObject(bmp);
		}
		private void CopyTextMenu_Click(object sender, RoutedEventArgs e) {
			// テキストを作成する
			var output = MakeGnuplotData();
			// コピーする
			try {
				Clipboard.SetText(output);
			} catch(Exception) {
				MessageBox.Show("データのコピーに失敗しました.", "HeavyDamageCalculator", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}
		private void SavePicMenu_Click(object sender, RoutedEventArgs e) {
			var sfd = new SaveFileDialog();
			sfd.FileName = "prob.png";
			sfd.Filter = "PNGファイル(*.png)|*.png|すべてのファイル(*.*)|*.*";
			sfd.AddExtension = true;
			if((bool)sfd.ShowDialog()) {
				try {
					ProbChart.SaveImage(sfd.FileName, System.Drawing.Imaging.ImageFormat.Png);
				} catch(Exception) {
					MessageBox.Show("画像の保存に失敗しました.", "HeavyDamageCalculator", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}
		}
		private void SaveTextMenu_Click(object sender, RoutedEventArgs e) {
			// テキストを作成する
			var output = MakeGnuplotData();
			// 保存する
			var sfd = new SaveFileDialog();
			sfd.FileName = "plot.txt";
			sfd.Filter = "テキストファイル(*.txt)|*.txt|すべてのファイル(*.*)|*.*";
			sfd.AddExtension = true;
			if((bool)sfd.ShowDialog()) {
				try {
					using(var stream = sfd.OpenFile())
					using(var sw = new System.IO.StreamWriter(stream))
						sw.Write(output);
				} catch(Exception) {
					MessageBox.Show("テキストの保存に失敗しました.", "HeavyDamageCalculator", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}
		}
		private void ExitMenu_Click(object sender, RoutedEventArgs e) {
			this.Close();
		}
		private void WindowSizeResetMenu_Click(object sender, RoutedEventArgs e) {
			this.Width = 600;
			this.Height = 400;
		}
		private void ParameterResetMenu_Click(object sender, RoutedEventArgs e) {
			var bindData = this.DataContext as MainWindowViewModel;
			bindData.MaxHpValue = 35;
			bindData.NowHpValue = 35;
			bindData.ArmorValue = 49;
			this.Draw();
		}
		private void IntervalResetMenu_Click(object sender, RoutedEventArgs e) {
			ChartIntervalSlider.Value = 2;
		}
		private void ScaleResetMenu_Click(object sender, RoutedEventArgs e) {
			ChartScaleSlider.Value = 1.0;
		}
		private void AddGraphMenu_Click(object sender, RoutedEventArgs e) {
			if(NowGraphParameter.Name == "") {
				MessageBox.Show("グラフ名を入力してください.", "HeavyDamageCalculator", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}
			graphParameterStock.Add(NowGraphParameter);
		}
		private void ClearGraphMenu_Click(object sender, RoutedEventArgs e) {
			graphParameterStock = new List<GraphParameter>();
			this.Draw();
		}
		private void NaiveCheckMenu_Changed(object sender, RoutedEventArgs e) {
			if(NaiveCheckBox != null && NaiveCheckMenu != null)
				NaiveCheckBox.IsChecked = NaiveCheckMenu.IsChecked;
			this.Draw(false);
		}
		private void PrimaryCheckMenu_Changed(object sender, RoutedEventArgs e) {
			if(PrimaryCheckBox != null && PrimaryCheckMenu != null)
				PrimaryCheckBox.IsChecked = PrimaryCheckMenu.IsChecked;
			this.Draw(false);
		}
		private void AfterLineCheckMenu_Changed(object sender, RoutedEventArgs e) {
			if (AfterLineCheckBox != null && AfterLineCheckMenu != null)
				AfterLineCheckBox.IsChecked = AfterLineCheckMenu.IsChecked;
			this.Draw(false);
		}
		private void AboutMenu_Click(object sender, RoutedEventArgs e) {
			// 自分自身のバージョン情報を取得する
			// (http://dobon.net/vb/dotnet/file/myversioninfo.html)
			// AssemblyTitle
			var asmttl = ((System.Reflection.AssemblyTitleAttribute)
				Attribute.GetCustomAttribute(
				System.Reflection.Assembly.GetExecutingAssembly(),
				typeof(System.Reflection.AssemblyTitleAttribute))).Title;
			// AssemblyCopyright
			var asmcpy = ((System.Reflection.AssemblyCopyrightAttribute)
				Attribute.GetCustomAttribute(
				System.Reflection.Assembly.GetExecutingAssembly(),
				typeof(System.Reflection.AssemblyCopyrightAttribute))).Copyright;
			// AssemblyProduct
			var asmprd = ((System.Reflection.AssemblyProductAttribute)
				Attribute.GetCustomAttribute(
				System.Reflection.Assembly.GetExecutingAssembly(),
				typeof(System.Reflection.AssemblyProductAttribute))).Product;
			// AssemblyVersion
			var asmver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
			MessageBox.Show(asmttl + " Ver." + asmver + "\n" + asmcpy + "\n" + asmprd);
		}
		#endregion
		#region 画面内のオブジェクトに関するイベント処理
		// マウスの移動前座標
		dPoint? dragPoint = null;
		// 表示スケール変化時に、スケール変更と感知するための最小相対移動距離
		const double ChartScaleChangeThreshold = 0.1;
		// 「入力中のグラフを表示」チェックボックスを操作した際の処理
		private void PrimaryCheckBox_Changed(object sender, RoutedEventArgs e) {
			if(PrimaryCheckBox != null && PrimaryCheckMenu != null)
				PrimaryCheckMenu.IsChecked = (bool)PrimaryCheckBox.IsChecked;
			PrimaryCheckMenu_Changed(sender, e);
		}
		// 「ナイーブな実装で計算」チェックボックスを操作した際の処理
		private void NaiveCheckBox_Changed(object sender, RoutedEventArgs e) {
			if(NaiveCheckBox != null && NaiveCheckMenu != null)
				NaiveCheckMenu.IsChecked = (bool)NaiveCheckBox.IsChecked;
			NaiveCheckMenu_Changed(sender, e);
		}
		// 「右端を平行線表示」チェックボックスを操作した際の処理
		private void AfterLineCheckBox_Changed(object sender, RoutedEventArgs e) {
			if (AfterLineCheckBox != null && AfterLineCheckMenu != null)
				AfterLineCheckMenu.IsChecked = (bool)AfterLineCheckBox.IsChecked;
			AfterLineCheckMenu_Changed(sender, e);
		}
		// パラメーター用スライダーを動かした際の処理
		private void ParameterSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
			this.Draw();
		}
		// ProbChart内でドラッグを開始した際の処理
		private void ProbChart_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e) {
			dragPoint = e.Location;
		}
		// ProbChart内でドラッグを終了した際の処理
		private void ProbChart_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e) {
			dragPoint = null;
		}
		// ProbChart内でマウスを動かした際の処理
		private void ProbChart_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e) {
			if(!dragPoint.HasValue)
				return;
			var chartArea = ProbChart.ChartAreas[0];
			// X軸方向の移動距離(グラフ座標換算)
			var pixelOffsetX = dragPoint.Value.X - e.Location.X;
			var pixelSizeX = ProbChart.Width;
			var chartScaleX = chartArea.AxisX.Maximum - chartArea.AxisX.Minimum;
			var chartOffsetX = chartScaleX * pixelOffsetX / pixelSizeX;
			// X軸方向の移動を検知したら、それだけを行うようにする
			if(Math.Abs(chartOffsetX) < chartScaleIntervalX[chartScaleIntervalIndexX])
				return;
			// グラフのマス目の分だけ移動距離を丸める
			chartOffsetX = (int)SpecialRound(chartOffsetX, chartScaleIntervalX[chartScaleIntervalIndexX]);
			// 移動させて「範囲」から外れないかを判定しつつ動かす
			var xmin = chartArea.AxisX.Minimum + chartOffsetX;
			var xmax = chartArea.AxisX.Maximum + chartOffsetX;
			if(xmin < 0) {
				xmax += -xmin;
				xmin = 0;
			}
			chartArea.AxisX.Minimum = xmin;
			chartArea.AxisX.Maximum = xmax;
			// dragPointを変更
			dragPoint = e.Location;
		}
		// 罫線用スライダーを動かした際の処理
		private void ChartIntervalSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
			chartScaleIntervalIndexX = (int)Math.Round(ChartIntervalSlider.Value);
			ProbChart.ChartAreas[0].AxisX.Interval = chartScaleIntervalX[chartScaleIntervalIndexX];
			chartScaleIntervalIndexY = (int)Math.Round(ChartIntervalSlider.Value);
			ProbChart.ChartAreas[0].AxisY.Interval = chartScaleIntervalY[chartScaleIntervalIndexY];
		}
		// 倍率用スライダーを動かした際の処理
		private void ChartScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
			this.Draw();
		}
		// 画面下のスライダーを動かした際の処理
		private void ChartCursorSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
			this.Draw(false);
		}
		// ウィンドウのサイズが変化する
		private void Window_SizeChanged(object sender, SizeChangedEventArgs e) {
			var bindData = this.DataContext as MainWindowViewModel;
			//var scale = (this.Width / this.MinWidth + this.Height / this.MinHeight) / 2;
			var scale = Math.Min(this.Width / this.MinWidth, this.Height / this.MinHeight);
			bindData.ScaleX = scale;
			bindData.ScaleY = scale;
			/*this.Width = this.MinWidth * scale;
			this.Height = this.MinHeight * scale;*/
		}
		#endregion
		#region グラフ描画に関するプロパティ・メソッド
		// 複数グラフを管理するためのList
		List<GraphParameter> graphParameterStock = new List<GraphParameter>();
		// グラフのスケール
		int[] chartScaleIntervalX = { 1, 2, 5, 10 };
		int[] chartScaleIntervalY = { 5, 5, 5, 10 };
		int chartScaleIntervalIndexX = 2;
		int chartScaleIntervalIndexY = 2;
		// 現在のグラフ名を返すプロパティ
		string NowGraphName {
			get {
				var bindData = this.DataContext as MainWindowViewModel;
				return $"{bindData.MaxHpValue},{bindData.ArmorValue},{bindData.NowHpValue}{(NaiveCheckMenu.IsChecked ? "☆" : "")}";
			}
		}
		// 現在のグラフパラメーターを返すプロパティ
		GraphParameter NowGraphParameter {
			get {
				var bindData = this.DataContext as MainWindowViewModel;
				return new GraphParameter(bindData.ParameterName, bindData.MaxHpValue, bindData.ArmorValue, bindData.NowHpValue, NaiveCheckMenu.IsChecked, AfterLineCheckMenu.IsChecked);
			}
		}
		// グラフをプロットする
		public void Draw(bool reScaleFlg = true) {
			// 初期化前は何もしない
			if(ProbChart == null)
				return;
			// グラフエリアを初期化する
			ProbChart.Series.Clear();
			ProbChart.Legends.Clear();
			// グラフエリアの罫線色を設定する
			ProbChart.ChartAreas[0].AxisX.MajorGrid.LineColor = Color.LightGray;
			ProbChart.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.LightGray;
			// グラフエリアにグラフを追加する
			var minAxisX = double.MaxValue;
			var maxAxisX = double.Epsilon;
			var maxAxisY = double.Epsilon;
			if(PrimaryCheckMenu.IsChecked) {
				var series = new Series();
				series.Name = NowGraphName;
				series.ChartType = SeriesChartType.Line;
				series.BorderWidth = 2;
				var plotData = CalculationLogic.CalcPlotData(NowGraphParameter);
				for(int i = 0; i < plotData.Count - (AfterLineCheckMenu.IsChecked ? 0 : 1); ++i) {
					series.Points.AddXY(plotData[i].X, plotData[i].Y * 100);
					if (plotData[i].X > 950.0)
						continue;
					minAxisX = Math.Min(minAxisX, plotData[i].X);
					maxAxisX = Math.Max(maxAxisX, plotData[i].X);
					maxAxisY = Math.Max(maxAxisY, plotData[i].Y * 100);
				}
				ProbChart.Series.Add(series);
				var legend = new Legend();
				legend.DockedToChartArea = "ChartArea";
				legend.Alignment = StringAlignment.Far;
				ProbChart.Legends.Add(legend);
			}
			var bindData = DataContext as MainWindowViewModel;
			// グラフエリアにストックしたグラフを追加する
			foreach(var graphParameter in graphParameterStock) {
				var series = new Series();
				series.Name = graphParameter.Name;
				series.ChartType = SeriesChartType.Line;
				series.BorderWidth = 2;
				var plotData = CalculationLogic.CalcPlotData(graphParameter);
				for (int i = 0; i < plotData.Count - (AfterLineCheckMenu.IsChecked ? 0 : 1); ++i) {
					series.Points.AddXY(plotData[i].X, plotData[i].Y * 100);
					if (plotData[i].X > 950.0)
						continue;
					minAxisX = Math.Min(minAxisX, plotData[i].X);
					maxAxisX = Math.Max(maxAxisX, plotData[i].X);
					maxAxisY = Math.Max(maxAxisY, plotData[i].Y * 100);
				}
				ProbChart.Series.Add(series);
				var legend = new Legend();
				legend.DockedToChartArea = "ChartArea";
				legend.Alignment = StringAlignment.Far;
				ProbChart.Legends.Add(legend);
			}
			// スライドバーに合った位置の縦棒を追加する
			if(ChartCursorSlider != null && (PrimaryCheckMenu.IsChecked || graphParameterStock.Count >= 1)) {
				var series = new Series();
				series.Name = "Cursor";
				series.ChartType = SeriesChartType.Line;
				series.BorderWidth = 2;
				int cursorValue = (int)ChartCursorSlider.Value;
				series.Points.AddXY(cursorValue, 0);
				series.Points.AddXY(cursorValue, 100);
				ProbChart.Series.Add(series);
				// 縦棒の位置における各グラフの数値を読み取り、表示に加える
				string titleText = $"HeavyDamageCalculator(最終攻撃力{cursorValue}";
				if (PrimaryCheckMenu.IsChecked) {
					var plotData = CalculationLogic.CalcPlotData(NowGraphParameter);
					titleText += $",{Math.Round(CalculationLogic.CalcGraphValueLinear(plotData, cursorValue) * 100, 1)}%";
				}
				foreach (var graphParameter in graphParameterStock) {
					var plotData = CalculationLogic.CalcPlotData(graphParameter);
					titleText += $",{Math.Round(CalculationLogic.CalcGraphValueLinear(plotData, cursorValue) * 100, 1)}%";
				}
				titleText += ")";
				this.Title = titleText;
			}
			// スケールを調整する
			{
				var axisX = ProbChart.ChartAreas[0].AxisX;
				axisX.Title = "最終攻撃力";
				var minimum = minAxisX;
				var maximum = maxAxisX;
				var center = (maximum + minimum) / 2;
				var halfRange = maximum - center;
				if (reScaleFlg) {
					axisX.Minimum = Math.Max(SpecialFloor(center - halfRange / ChartScaleSlider.Value, chartScaleIntervalX[chartScaleIntervalIndexX]), 0.0);
					axisX.Maximum = SpecialCeiling(center + halfRange / ChartScaleSlider.Value, chartScaleIntervalX[chartScaleIntervalIndexX]);
					axisX.Interval = chartScaleIntervalX[chartScaleIntervalIndexX];
				}
				if (ChartCursorSlider != null) {
					ChartCursorSlider.Minimum = (int)axisX.Minimum;
					ChartCursorSlider.Maximum = (int)axisX.Maximum;
				}
			}
			{
				var axisY = ProbChart.ChartAreas[0].AxisY;
				axisY.Title = "大破率(％)";
				axisY.Minimum = 0;
				var temp = SpecialCeiling(maxAxisY, chartScaleIntervalY[chartScaleIntervalIndexY]);
				axisY.Maximum = MaxMin(temp, 0.0, 100.0);
				axisY.Interval = chartScaleIntervalY[chartScaleIntervalIndexY];
			}
		}
		// グラフデータをgnuplot形式で書き出す
		string MakeGnuplotData() {
			var output = "";
			bool lineFeedFlg = false;
			// 現在のグラフデータ
			if(PrimaryCheckMenu.IsChecked) {
				output += $"#[{NowGraphName}]\n";
				var plotData = CalculationLogic.CalcPlotData(NowGraphParameter);
				foreach(var point in plotData) {
					output += $"{point.X} {point.Y}\n";
				}
				lineFeedFlg = true;
			}
			// ストックしてあるグラフデータ
			if(graphParameterStock.Count >= 1) {
				foreach(var graphParameter in graphParameterStock) {
					if(lineFeedFlg)
						output += "\n\n";
					output += $"#[{graphParameter.Name}]\n";
					var plotData = CalculationLogic.CalcPlotData(graphParameter);
					foreach(var point in plotData) {
						output += $"{point.X} {point.Y}\n";
					}
					lineFeedFlg = true;
				}
			}
			return output;
		}
		#endregion
		#region ユーティリティ
		// xをstepの定数倍になるように切り下げる
		double SpecialFloor(double x, double step) {
			return Math.Floor(x / step) * step;
		}
		// xをstepの定数倍になるように切り上げる
		double SpecialCeiling(double x, double step) {
			return Math.Ceiling(x / step) * step;
		}
		// xをstepの定数倍になるように丸める
		double SpecialRound(double x, double step) {
			return Math.Round(x / step) * step;
		}
		// xをmin～maxの範囲に丸める
		double MaxMin(double x, double min, double max) {
			return (x < min ? min : x > max ? max : x);
		}
		#endregion
	}
}
