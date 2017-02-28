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
			// プロットするデータを用意する(スタブ)
			var plotData = new List<Point> {
				new Point { X = 34.3, Y = 0         },
				new Point { X = 60.3, Y = 0         },
				new Point { X = 63.7, Y = 0.115646  },
				new Point { X = 68.3, Y = 0.272109  },
				new Point { X = 89.7, Y = 0.3345    },
				new Point { X = 97.7, Y = 0.0857143 },
			};
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
