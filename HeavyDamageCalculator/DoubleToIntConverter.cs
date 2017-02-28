using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace HeavyDamageCalculator {
	/** スライドバーにBindingしたTextBlockの表示が小数にならないようにするための細工
	 * 参考ページ：
	 * 「[WPF] WPF でスライダーにバインドしたテキストボックスの値を常に整数に保つ方法 - ぐーたら書房」
	 * (http://gootara.org/library/2016/06/wpf-sdc2.html)
	 */
	public class DoubleToIntConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			return System.Convert.ToInt32(value);
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}
}
