using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HeavyDamageCalculator {
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
		// グラフをプロットする
		public void Draw() {
			if(ProbChart == null)
				return;
			var bindData = this.DataContext as MainWindowViewModel;
			// プロット用データを用意する
			var plotData = CalculationLogic.CalcPlotData(bindData.MaxHpValue, bindData.ArmorValue, bindData.NowHpValue, (bool)NaiveCheckBox.IsChecked);
			// グラフエリアを初期化する
			ProbChart.Series.Clear();
			if(plotData.Count <= 0)
				return;
			{
				var axisX = ProbChart.ChartAreas[0].AxisX;
				axisX.Title = "最終攻撃力";
				axisX.Minimum = 0;
				axisX.Maximum = Math.Ceiling(plotData.Max(p => p.X) / 10) * 10;
				axisX.Interval = 10;
			}
			{
				var axisY = ProbChart.ChartAreas[0].AxisY;
				axisY.Title = "大破率(％)";
				axisY.Minimum = 0;
				axisY.Maximum = Math.Ceiling(plotData.Max(p => p.Y) * 100 / 10) * 10;
				axisY.Interval = 10;
			}
			// グラフエリアにグラフを追加する
			var series = new Series();
			series.ChartType = SeriesChartType.Line;
			series.BorderWidth = 2;
			foreach(var point in plotData) {
				series.Points.AddXY(point.X, point.Y * 100);
			}
			ProbChart.Series.Add(series);
		}
		// スライダーを動かした際の処理
		private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
			this.Draw();
		}
		// チェックボックスを切り替えた際の処理
		private void NaiveCheckBox_Changed(object sender, RoutedEventArgs e) {
			this.Draw();
		}
		// 表示をリセットする
		private void ResetButton_Click(object sender, RoutedEventArgs e) {
			this.Width = 450;
			this.Height = 350;
			var bindData = this.DataContext as MainWindowViewModel;
			bindData.MaxHpValue = 35;
			bindData.NowHpValue = 35;
			bindData.ArmorValue = 49;
		}
		// 画像を保存する
		private void PicSaveButton_Click(object sender, RoutedEventArgs e) {
			var sfd = new SaveFileDialog();
			sfd.FileName = "prob.png";
			sfd.Filter = "PNGファイル(*.png)|*.png|すべてのファイル(*.*)|*.*";
			sfd.ShowDialog();
			if(sfd.FileName != "") {
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
			var outout = "";
			foreach(var point in plotData) {
				outout += $"{point.X} {point.Y}\n";
			}
			// コピーする
			try {
				Clipboard.SetText(outout);
			} catch(Exception) {
				MessageBox.Show("データのコピーに失敗しました.", "HeavyDamageCalculator", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}
	}
}
