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
		// 複数グラフを管理するためのDictionary
		Dictionary<string, List<Point>> plotDataStock = new Dictionary<string, List<Point>>();
		// グラフのスケール
		int[] chartScaleIntervalX = { 1, 2, 5, 10 };
		int[] chartScaleIntervalY = { 1, 2, 5, 10 };
		int chartScaleIntervalIndexX = 3;
		int chartScaleIntervalIndexY = 3;
		// マウスにおけるドラッグ判定
		dPoint? dragPoint = null; //マウスの移動前座標
		// コンストラクタ
		public MainWindow() {
			InitializeComponent();
			this.DataContext = new MainWindowViewModel();
		}
		protected override void OnSourceInitialized(EventArgs e) {
			base.OnSourceInitialized(e);
			this.Draw();
		}
		// グラフをプロットする
		public void Draw() {
			// 初期化前は何もしない
			if(ProbChart == null)
				return;
			// プロット用データを用意する
			var bindData = this.DataContext as MainWindowViewModel;
			var plotData = CalculationLogic.CalcPlotData(bindData.MaxHpValue, bindData.ArmorValue, bindData.NowHpValue, (bool)NaiveCheckBox.IsChecked);
			// グラフエリアを初期化する
			ProbChart.Series.Clear();
			ProbChart.Legends.Clear();
			if(plotData.Count <= 0)
				return;
			// グラフエリアの罫線色を設定する
			ProbChart.ChartAreas[0].AxisX.MajorGrid.LineColor = Color.LightGray;
			ProbChart.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.LightGray;
			// グラフエリアにグラフを追加する
			var maxAxisX = double.Epsilon;
			var maxAxisY = double.Epsilon;
			{
				var series = new Series();
				series.Name = "入力データ";
				series.ChartType = SeriesChartType.Line;
				series.BorderWidth = 2;
				foreach(var point in plotData) {
					series.Points.AddXY(point.X, point.Y * 100);
					maxAxisX = Math.Max(maxAxisX, point.X);
					maxAxisY = Math.Max(maxAxisY, point.Y * 100);
				}
				ProbChart.Series.Add(series);
				var legend = new Legend();
				legend.DockedToChartArea = "ChartArea";
				legend.Alignment = StringAlignment.Far;
				ProbChart.Legends.Add(legend);
			}
			// グラフエリアにストックしたグラフを追加する
			foreach(var pair in plotDataStock) {
				var series = new Series();
				series.Name = pair.Key;
				series.ChartType = SeriesChartType.Line;
				series.BorderWidth = 2;
				foreach(var point in pair.Value) {
					series.Points.AddXY(point.X, point.Y * 100);
					maxAxisX = Math.Max(maxAxisX, point.X);
					maxAxisY = Math.Max(maxAxisY, point.Y * 100);
				}
				ProbChart.Series.Add(series);
				var legend = new Legend();
				legend.DockedToChartArea = "ChartArea";
				legend.Alignment = StringAlignment.Far;
				ProbChart.Legends.Add(legend);
			}
			// スケールを調整する
			{
				var axisX = ProbChart.ChartAreas[0].AxisX;
				axisX.Title = "最終攻撃力";
				axisX.Minimum = 0;
				axisX.Maximum = SpecialCeiling(maxAxisX, chartScaleIntervalX[chartScaleIntervalIndexX]);
				axisX.Interval = chartScaleIntervalX[chartScaleIntervalIndexX];
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
		// スライダーを動かした際の処理
		private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
			this.Draw();
		}
		// チェックボックスを切り替えた際の処理
		private void NaiveCheckBox_Changed(object sender, RoutedEventArgs e) {
			this.Draw();
		}
		// グラフを追加する
		private void AddGraphButton_Click(object sender, RoutedEventArgs e) {
			// Keyを生成する
			var bindData = this.DataContext as MainWindowViewModel;
			var key = $"{bindData.MaxHpValue},{bindData.ArmorValue},{bindData.NowHpValue}{((bool)NaiveCheckBox.IsChecked ? "☆" : "")}";
			// Valueを生成する
			var plotData = CalculationLogic.CalcPlotData(bindData.MaxHpValue, bindData.ArmorValue, bindData.NowHpValue, (bool)NaiveCheckBox.IsChecked);
			if(plotData.Count <= 0)
				return;
			// plotDataStockに追加する
			plotDataStock[key] = plotData;
		}
		// グラフを削除する
		private void ClearGraphButton_Click(object sender, RoutedEventArgs e) {
			plotDataStock = new Dictionary<string, List<Point>>();
			this.Draw();
		}
		// ウィンドウサイズをリセットする
		private void ResetButton_Click(object sender, RoutedEventArgs e) {
			this.Width = 450;
			this.Height = 350;
			var bindData = this.DataContext as MainWindowViewModel;
			bindData.MaxHpValue = 35;
			bindData.NowHpValue = 35;
			bindData.ArmorValue = 49;
			this.Draw();
		}
		// グラフの画像を保存する
		private void PicSaveButton_Click(object sender, RoutedEventArgs e) {
			var sfd = new SaveFileDialog();
			sfd.FileName = "prob.png";
			sfd.Filter = "PNGファイル(*.png)|*.png|すべてのファイル(*.*)|*.*";
			sfd.AddExtension = true;
			if((bool)sfd.ShowDialog()) {
				try {
					ProbChart.SaveImage(sfd.FileName, System.Drawing.Imaging.ImageFormat.Png);
				}catch(Exception) {
					MessageBox.Show("画像の保存に失敗しました.", "HeavyDamageCalculator", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}
		}
		// gnuplot形式でコピーする
		private void CopyGnuplotButton_Click(object sender, RoutedEventArgs e) {
			// プロット用データを用意する
			var bindData = this.DataContext as MainWindowViewModel;
			var plotData = CalculationLogic.CalcPlotData(bindData.MaxHpValue, bindData.ArmorValue, bindData.NowHpValue, (bool)NaiveCheckBox.IsChecked);
			// コピペできるように加工する
			var output = "[入力データ]\n";
			foreach(var point in plotData) {
				output += $"{point.X} {point.Y}\n";
			}
			if(plotDataStock.Count >= 1) {
				foreach(var pair in plotDataStock) {
					output += $"[{pair.Key}]\n";
					foreach(var point in pair.Value) {
						output += $"{point.X} {point.Y}\n";
					}
				}
			}
			// コピーする
			try {
				Clipboard.SetText(output);
			} catch(Exception) {
				MessageBox.Show("データのコピーに失敗しました.", "HeavyDamageCalculator", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}
		// ProbChart内でドラッグを開始した際の処理
		private void ProbChart_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e) {
			dragPoint = e.Location;
		}
		// ProbChart内でドラッグを終了した際の処理
		private void ProbChart_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e) {
			dragPoint = null;
		}
		// ProbChart内でドラッグし続けた際の処理
		private void ProbChart_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e) {
			if(!dragPoint.HasValue)
				return;
			// diff_Xはドラッグ時の移動距離
			var diffWigth = dragPoint.Value.X - e.Location.X;
			var diffHeight = e.Location.Y - dragPoint.Value.Y;
			// 移動距離をグラフ座標に変換する
			var chartWidth = ProbChart.Width;
			var chartHeight = ProbChart.Height;
			var chartArea = ProbChart.ChartAreas[0];
			var chartScaleX = chartArea.AxisX.Maximum - chartArea.AxisX.Minimum;
			var chartScaleY = chartArea.AxisY.Maximum - chartArea.AxisY.Minimum;
			var diffScaleX = chartScaleX * diffWigth / chartWidth;
			var diffScaleY = chartScaleY * diffHeight / chartHeight;
			Console.WriteLine($"check {diffScaleX},${diffScaleY}");
			// 移動距離が、グラフのマス目より小さい場合はまだ動かさない
			if(Math.Abs(diffScaleX) < chartScaleIntervalX[chartScaleIntervalIndexX]
			&& Math.Abs(diffScaleY) < chartScaleIntervalY[chartScaleIntervalIndexY])
				return;
			// グラフのマス目の分だけ移動距離を丸める
			diffScaleX = (int)SpecialFloor(diffScaleX, chartScaleIntervalX[chartScaleIntervalIndexX]);
			diffScaleY = (int)SpecialFloor(diffScaleY, chartScaleIntervalY[chartScaleIntervalIndexY]);
			Console.WriteLine($"move {diffScaleX},${diffScaleY}");
			// 移動させて「範囲」から外れないかを判定しつつ動かす
			var xmin = chartArea.AxisX.Minimum + diffScaleX;
			var xmax = chartArea.AxisX.Maximum + diffScaleX;
			if(xmin < 0) {
				xmax += -xmin;
				xmin = 0;
			}
			chartArea.AxisX.Minimum = xmin;
			chartArea.AxisX.Maximum = xmax;
			var ymin = chartArea.AxisY.Minimum + diffScaleY;
			var ymax = chartArea.AxisY.Maximum + diffScaleY;
			if(ymin < 0) {
				ymax += -ymin;
				ymin = 0;
			}
			if(ymax > 100) {
				ymin -= ymax - 100;
				ymax = 100;
			}
			chartArea.AxisY.Minimum = ymin;
			chartArea.AxisY.Maximum = ymax;
			// dragPointを変更
			dragPoint = e.Location;
		}
		// ProbChartの罫線を細かく/荒くする
		private void ProbChart_KeyDown(object sender, KeyEventArgs e) {
			if(e.Key == Key.Add) {
				chartScaleIntervalIndexX = (int)MaxMin(chartScaleIntervalIndexX - 1, 0, 3);
				ProbChart.ChartAreas[0].AxisX.Interval = chartScaleIntervalX[chartScaleIntervalIndexX];
				chartScaleIntervalIndexY = (int)MaxMin(chartScaleIntervalIndexY - 1, 0, 3);
				ProbChart.ChartAreas[0].AxisY.Interval = chartScaleIntervalY[chartScaleIntervalIndexY];
			}
			if(e.Key == Key.Subtract) {
				chartScaleIntervalIndexX = (int)MaxMin(chartScaleIntervalIndexX + 1, 0, 3);
				ProbChart.ChartAreas[0].AxisX.Interval = chartScaleIntervalX[chartScaleIntervalIndexX];
				chartScaleIntervalIndexY = (int)MaxMin(chartScaleIntervalIndexY + 1, 0, 3);
				ProbChart.ChartAreas[0].AxisY.Interval = chartScaleIntervalY[chartScaleIntervalIndexY];
			}
		}
		// xをstepの定数倍になるように切り下げる
		double SpecialFloor(double x, double step) {
			return Math.Floor(x / step) * step;
		}
		// xをstepの定数倍になるように切り上げる
		double SpecialCeiling(double x, double step) {
			return Math.Ceiling(x / step) * step;
		}
		// xをmin～maxの範囲に丸める
		double MaxMin(double x, double min, double max) {
			return (x < min ? min : x > max ? max : x);
		}
	}
}
