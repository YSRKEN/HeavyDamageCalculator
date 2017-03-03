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
		#region 画面内のオブジェクトに関するイベント処理
		// マウスの移動前座標
		dPoint? dragPoint = null;
		// スライダーを動かした際の処理
		private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
			this.Draw();
		}
		// チェックボックスを切り替えた際の処理
		private void NaiveCheckBox_Changed(object sender, RoutedEventArgs e) {
			this.Draw();
		}
		private void PrimaryCheckBox_Changed(object sender, RoutedEventArgs e) {
			this.Draw();
		}
		// グラフを追加する
		private void AddGraphButton_Click(object sender, RoutedEventArgs e) {
			var bindData = this.DataContext as MainWindowViewModel;
			var graphParameter = new GraphParameter(bindData.ParameterName, bindData.MaxHpValue, bindData.ArmorValue, bindData.NowHpValue, (bool)NaiveCheckBox.IsChecked);
			graphParameterStock.Add(graphParameter);
		}
		// グラフを削除する
		private void ClearGraphButton_Click(object sender, RoutedEventArgs e) {
			graphParameterStock = new List<GraphParameter>();
			this.Draw();
		}
		// ウィンドウサイズをリセットする
		private void WindowSizeResetButton_Click(object sender, RoutedEventArgs e) {
			this.Width = 450;
			this.Height = 350;
		}
		// パラメーターをリセットする
		private void ParameterResetButton_Click(object sender, RoutedEventArgs e) {
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
				} catch(Exception) {
					MessageBox.Show("画像の保存に失敗しました.", "HeavyDamageCalculator", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}
		}
		// gnuplot形式でコピーする
		private void CopyGnuplotButton_Click(object sender, RoutedEventArgs e) {
			// テキストを作成する
			var output = "";
			if((bool)PrimaryCheckBox.IsChecked) {
				output += $"#[{ParameterKey}]\n";
				foreach(var point in ParameterValue) {
					output += $"{point.X} {point.Y}\n";
				}
			}
			if(graphParameterStock.Count >= 1) {
				foreach(var graphParameter in graphParameterStock) {
					output += $"\n\n#[{graphParameter.Name}]\n";
					var plotData = CalculationLogic.CalcPlotData(graphParameter.MaxHp, graphParameter.Armor, graphParameter.NowHp, graphParameter.NaiveFlg);
					foreach(var point in plotData) {
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
		// ProbChart内でマウスを動かした際の処理
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
			//Console.WriteLine($"check {diffScaleX},${diffScaleY}");
			// 移動距離が、グラフのマス目より小さい場合はまだ動かさない
			if(Math.Abs(diffScaleX) < chartScaleIntervalX[chartScaleIntervalIndexX]
			&& Math.Abs(diffScaleY) < chartScaleIntervalY[chartScaleIntervalIndexY])
				return;
			// グラフのマス目の分だけ移動距離を丸める
			diffScaleX = (int)SpecialFloor(diffScaleX, chartScaleIntervalX[chartScaleIntervalIndexX]);
			diffScaleY = (int)SpecialFloor(diffScaleY, chartScaleIntervalY[chartScaleIntervalIndexY]);
			//Console.WriteLine($"move {diffScaleX},${diffScaleY}");
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
		// 罫線を細かくする
		private void FineIntervalButton_Click(object sender, RoutedEventArgs e) {
			chartScaleIntervalIndexX = (int)MaxMin(chartScaleIntervalIndexX - 1, 0, 3);
			ProbChart.ChartAreas[0].AxisX.Interval = chartScaleIntervalX[chartScaleIntervalIndexX];
			chartScaleIntervalIndexY = (int)MaxMin(chartScaleIntervalIndexY - 1, 0, 3);
			ProbChart.ChartAreas[0].AxisY.Interval = chartScaleIntervalY[chartScaleIntervalIndexY];
		}
		// 罫線を荒くする
		private void RoughIntervalButton_Click(object sender, RoutedEventArgs e) {
			chartScaleIntervalIndexX = (int)MaxMin(chartScaleIntervalIndexX + 1, 0, 3);
			ProbChart.ChartAreas[0].AxisX.Interval = chartScaleIntervalX[chartScaleIntervalIndexX];
			chartScaleIntervalIndexY = (int)MaxMin(chartScaleIntervalIndexY + 1, 0, 3);
			ProbChart.ChartAreas[0].AxisY.Interval = chartScaleIntervalY[chartScaleIntervalIndexY];
		}
		// 交戦形態を考慮するかどうかを決める
		private void BattleTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			this.Draw();
		}
		// ウィンドウのサイズが変化する
		private void Window_SizeChanged(object sender, SizeChangedEventArgs e) {
			var bindData = this.DataContext as MainWindowViewModel;
			var scale = (this.Width / this.MinWidth + this.Height / this.MinHeight) / 2;
			bindData.ScaleX = scale;
			bindData.ScaleY = scale;
			this.Width = this.MinWidth * scale;
			this.Height = this.MinHeight * scale;
		}
		#endregion
		#region グラフ描画に関するプロパティ・メソッド
		// 複数グラフを管理するためのList
		List<GraphParameter> graphParameterStock = new List<GraphParameter>();
		// グラフのスケール
		int[] chartScaleIntervalX = { 1, 2, 5, 10 };
		int[] chartScaleIntervalY = { 1, 2, 5, 10 };
		int chartScaleIntervalIndexX = 2;
		int chartScaleIntervalIndexY = 2;
		// 現在のグラフ名を返すプロパティ
		string ParameterKey {
			get {
				var bindData = this.DataContext as MainWindowViewModel;
				return $"{bindData.MaxHpValue},{bindData.ArmorValue},{bindData.NowHpValue}{((bool)NaiveCheckBox.IsChecked ? "☆" : "")}";
			}
		}
		// 現在のグラフデータを返すプロパティ
		List<Point> ParameterValue {
			get {
				var bindData = this.DataContext as MainWindowViewModel;
				return CalculationLogic.CalcPlotData(bindData.MaxHpValue, bindData.ArmorValue, bindData.NowHpValue, (bool)NaiveCheckBox.IsChecked);
			}
		}
		// グラフをプロットする
		public void Draw() {
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
			if((bool)PrimaryCheckBox.IsChecked) {
				var series = new Series();
				series.Name = this.ParameterKey;
				series.ChartType = SeriesChartType.Line;
				series.BorderWidth = 2;
				foreach(var point in this.ParameterValue) {
					series.Points.AddXY(point.X, point.Y * 100);
					minAxisX = Math.Min(minAxisX, point.X);
					maxAxisX = Math.Max(maxAxisX, point.X);
					maxAxisY = Math.Max(maxAxisY, point.Y * 100);
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
				foreach(var point in CalculationLogic.CalcPlotData(graphParameter)) {
					series.Points.AddXY(point.X, point.Y * 100);
					minAxisX = Math.Min(minAxisX, point.X);
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
				axisX.Minimum = SpecialFloor(minAxisX, chartScaleIntervalX[chartScaleIntervalIndexX]);
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
		// xをmin～maxの範囲に丸める
		double MaxMin(double x, double min, double max) {
			return (x < min ? min : x > max ? max : x);
		}
		#endregion
	}
}
