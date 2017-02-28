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
			var bindData = this.DataContext as MainWindowViewModel;
			// プロット用データを用意する
			var plotData = CalculationLogic.CalcPlotData(bindData.MaxHpValue, bindData.ArmorValue, bindData.NowHpValue);
			// グラフエリアを初期化する
			ProbChart.Series.Clear();
			var chartArea = ProbChart.ChartAreas[0];
			chartArea.AxisX.Title = "最終攻撃力";
			chartArea.AxisY.Title = "大破率(％)";
			// グラフエリアにグラフを追加する
			var series = new Series();
			series.ChartType = SeriesChartType.Line;
			series.BorderWidth = 2;
			foreach(var point in plotData) {
				series.Points.AddXY(point.X, point.Y * 100);
			}
			ProbChart.Series.Add(series);
		}
	}
}
